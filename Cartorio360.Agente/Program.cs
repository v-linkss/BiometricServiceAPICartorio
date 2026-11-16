using System.Net;
using System.Net.Sockets;
using Cartorio360.Agente.Api;
using Cartorio360.Agente.Bandeja;
using Cartorio360.Agente.Scanner;
using Cartorio360.Agente.Servicos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cartorio360.Agente;

internal static class Program
{
    // Instancia unica. Com o autostart por tarefa agendada, clicar no atalho
    // abriria um segundo processo disputando a porta — e ele morreria com
    // "address already in use", mensagem que nao diz nada ao atendente.
    private const string NOME_MUTEX = @"Global\Cartorio360Agente";

    /// <summary>
    /// STAThread e obrigatorio: WIA e COM STA e a janela do scanner e WinForms.
    /// Este atributo e a razao de o agente ser um app de bandeja e nao um
    /// Windows Service — sessao 0 nao tem desktop nem message pump.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, NOME_MUTEX, out bool primeiro);
        if (!primeiro)
        {
            MessageBox.Show(
                "O Agente 360 já está em execução.\nProcure o ícone na área de notificação, ao lado do relógio.",
                "Agente 360", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var config = Configuracao.Carregar();
        Log.Configurar(Path.Combine(AppContext.BaseDirectory, "logs"));
        Log.Info($"--- Agente 360 iniciando (porta {config.Porta}, backend {config.MsManagementUrl}) ---");

        if (PortaOcupada(config.Porta))
        {
            var quem = DescreverOcupante(config.Porta);
            Log.Erro($"porta {config.Porta} ocupada. {quem}");
            MessageBox.Show(
                $"A porta {config.Porta} já está em uso.\n\n{quem}\n\n" +
                "Feche o programa que está usando a porta, ou altere 'porta' no config.json.",
                "Agente 360", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Formulario oculto que existe so para dar uma thread de UI onde marshalar
        // a digitalizacao. Sem ele nao ha para onde fazer Invoke.
        using var bomba = new Form
        {
            ShowInTaskbar = false,
            WindowState = FormWindowState.Minimized,
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            Opacity = 0
        };
        bomba.Load += (_, _) => bomba.Hide();
        _ = bomba.Handle;   // forca a criacao do handle antes do primeiro Invoke

        var ultima = new UltimaDigitalizacao();
        var zebra = new ZebraPrinter(config);
        var scanner = new ScannerHost(bomba, config, ultima);
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var upload = new UploadService(config, ultima, http);

        using var bandeja = new AgenteTray(config, zebra);

        WebApplication app;
        try
        {
            app = MontarWebApp(config, scanner, upload, zebra, ultima);
            app.Start();
        }
        catch (Exception ex)
        {
            Log.Erro("falha ao subir o servidor HTTP", ex);
            MessageBox.Show($"Não foi possível iniciar o Agente 360:\n\n{ex.Message}",
                "Agente 360", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Log.Info($"escutando em http://127.0.0.1:{config.Porta}/apiservice");
        bandeja.Notificar("Agente 360", $"No ar na porta {config.Porta}.");

        Application.ApplicationExit += (_, _) =>
        {
            Log.Info("--- Agente 360 encerrando ---");
            try { app.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(); } catch { }
            http.Dispose();
        };

        // Bloqueia aqui rodando o message pump. O Kestrel segue nas threads dele.
        Application.Run();
    }

    private static WebApplication MontarWebApp(
        Configuracao config,
        ScannerHost scanner,
        UploadService upload,
        ZebraPrinter zebra,
        UltimaDigitalizacao ultima)
    {
        var builder = WebApplication.CreateBuilder();

        // Loopback e nao 0.0.0.0 (como estava no appsettings do BiometricService):
        // o front roda no navegador da propria estacao, entao localhost sempre e
        // esta maquina. Sem escuta externa, o Windows Firewall nao pergunta nada
        // e o instalador nao precisa criar excecao.
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, config.Porta));

        builder.Logging.ClearProviders();

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(scanner);
        builder.Services.AddSingleton(upload);
        builder.Services.AddSingleton(zebra);
        builder.Services.AddSingleton(ultima);

        builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();

        // Private Network Access / Local Network Access.
        //
        // O navegador trata "pagina publica chamando localhost" como travessia
        // para um espaco de enderecos mais privado, e manda um preflight com
        // Access-Control-Request-Private-Network: true. Sem a resposta abaixo, a
        // chamada e barrada mesmo com o CORS liberado.
        //
        // Isto NAO resolve sozinho: desde o Chrome 142 a origem tambem precisa
        // ser contexto seguro (HTTPS) e o usuario precisa conceder a permissao.
        // Um front servido em http:// e barrado antes de chegar aqui. Ver o
        // README para as opcoes de contorno.
        app.Use(async (contexto, seguir) =>
        {
            if (HttpMethods.IsOptions(contexto.Request.Method) &&
                contexto.Request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
            {
                contexto.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
            }
            await seguir();
        });

        app.UseCors();
        app.MapRotas();
        return app;
    }

    private static bool PortaOcupada(int porta)
    {
        try
        {
            using var teste = new TcpListener(IPAddress.Loopback, porta);
            teste.Start();
            teste.Stop();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }

    /// <summary>
    /// Diz QUEM esta na porta. Sem isso, "porta ocupada" manda o suporte
    /// adivinhar — e o suspeito provavel e justamente uma sobra da instalacao
    /// antiga.
    /// </summary>
    private static string DescreverOcupante(int porta)
    {
        try
        {
            var conexoes = System.Net.NetworkInformation.IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners();

            if (conexoes.Any(c => c.Port == porta))
                return "Outro programa já está escutando nessa porta (possivelmente uma versão anterior do agente).";
        }
        catch { }
        return "Não foi possível identificar o programa que está usando a porta.";
    }
}
