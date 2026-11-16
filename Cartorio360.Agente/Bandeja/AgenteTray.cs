using System.Diagnostics;
using Cartorio360.Agente.Servicos;

namespace Cartorio360.Agente.Bandeja;

/// <summary>
/// Icone na bandeja. E a unica presenca visivel do agente entre uma
/// digitalizacao e outra, e o caminho do suporte para os logs.
/// </summary>
public sealed class AgenteTray : IDisposable
{
    private readonly NotifyIcon _icone;
    private readonly Icon _iconeGrafico;
    private readonly Configuracao _config;
    private readonly ZebraPrinter _zebra;

    public AgenteTray(Configuracao config, ZebraPrinter zebra)
    {
        _config = config;
        _zebra = zebra;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Diagnóstico...", null, (_, _) => MostrarDiagnostico());
        menu.Items.Add("Abrir pasta de logs", null, (_, _) => Abrir(Log.PastaAtual));
        menu.Items.Add("Abrir pasta de digitalizações", null, (_, _) => Abrir(_config.PastaScansResolvida));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => Application.Exit());

        _iconeGrafico = Recursos.CarregarPequeno();
        _icone = new NotifyIcon
        {
            Icon = _iconeGrafico,
            Text = $"Agente 360 — porta {config.Porta}",
            Visible = true,
            ContextMenuStrip = menu
        };
        _icone.DoubleClick += (_, _) => MostrarDiagnostico();
    }

    public void Notificar(string titulo, string mensagem, ToolTipIcon tipo = ToolTipIcon.Info)
    {
        try
        {
            _icone.BalloonTipTitle = titulo;
            _icone.BalloonTipText = mensagem;
            _icone.BalloonTipIcon = tipo;
            _icone.ShowBalloonTip(5000);
        }
        catch { /* balao e conveniencia, nunca motivo de falha */ }
    }

    private void MostrarDiagnostico()
    {
        var fila = _zebra.Localizar();
        var texto =
            $"Agente 360\n" +
            $"Porta: {_config.Porta}\n" +
            $"Backend: {_config.MsManagementUrl}\n" +
            $"Digitalizações: {_config.PastaScansResolvida}\n" +
            $"Logs: {Log.PastaAtual}\n\n" +
            (fila == null
                ? "Zebra: não encontrada"
                : $"Zebra: {fila.Nome}\nPorta: {fila.Porta}\nDriver: {fila.Driver}") +
            (fila is { PortaSuspeita: true }
                ? $"\n\nATENÇÃO: a fila está em {fila.Porta}. O Windows aceita o job\ne pode não sair papel. Verifique a porta da impressora."
                : "");

        MessageBox.Show(texto, "Agente 360", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void Abrir(string pasta)
    {
        try
        {
            Directory.CreateDirectory(pasta);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{pasta}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível abrir a pasta:\n{ex.Message}");
        }
    }

    public void Dispose()
    {
        // Esconder antes de descartar: sem isto o icone fica "fantasma" na
        // bandeja ate alguem passar o mouse por cima.
        _icone.Visible = false;
        _icone.Dispose();
        _iconeGrafico.Dispose();
    }
}
