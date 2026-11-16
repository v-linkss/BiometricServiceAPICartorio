using Cartorio360.Agente.Servicos;

namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Ponte entre o handler HTTP (thread do Kestrel) e a janela do scanner (thread
/// de UI, STA).
///
/// Por que isto e necessario: WIA e COM e a janela e WinForms — as duas coisas
/// exigem a thread STA que roda o message pump. Um handler do ASP.NET Core roda
/// numa thread do pool, sem pump. Chamar WIA de la falha de formas obscuras, ou
/// trava. Entao o handler marshala para a thread principal via Invoke num
/// formulario oculto criado no arranque.
///
/// Este era o unico ponto realmente delicado de absorver o 360scan.exe: como
/// processo separado ele tinha o proprio [STAThread] Main. In-process, e preciso
/// reproduzir isso de proposito.
/// </summary>
public sealed class ScannerHost
{
    private readonly Form _bomba;
    private readonly Configuracao _config;
    private readonly UltimaDigitalizacao _ultima;

    // Uma digitalizacao por vez. Sem isto, dois cliques rapidos no front abririam
    // duas janelas disputando o mesmo scanner e a mesma pasta.
    private readonly SemaphoreSlim _exclusao = new(1, 1);

    public ScannerHost(Form bomba, Configuracao config, UltimaDigitalizacao ultima)
    {
        _bomba = bomba;
        _config = config;
        _ultima = ultima;
    }

    public bool Ocupado => _exclusao.CurrentCount == 0;

    /// <summary>
    /// Abre a janela de digitalizacao e so retorna quando ela fecha — mesmo
    /// contrato do Process.Start(...).WaitForExit() que o BiometricService fazia.
    /// O front depende disso: ele chama /run-scanner, espera, e so entao chama
    /// /upload.
    /// </summary>
    public async Task<(ResultadoDigitalizacao? Resultado, string? Erro)> DigitalizarAsync(FormatoDigitalizacao formato)
    {
        if (!await _exclusao.WaitAsync(TimeSpan.FromSeconds(1)))
            throw new InvalidOperationException("Já existe uma digitalização em andamento.");

        try
        {
            var pasta = Path.Combine(_config.PastaScansResolvida, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            Log.Info($"iniciando digitalizacao formato={formato} pasta={pasta}");

            var tcs = new TaskCompletionSource<(ResultadoDigitalizacao?, string?)>();

            // BeginInvoke e nao Invoke: Invoke bloquearia a thread do Kestrel
            // enquanto a janela estivesse aberta, e um segundo request (o /status
            // que o front dispara para decidir o formato da etiqueta) ficaria
            // preso atras dela.
            _bomba.BeginInvoke(new Action(() =>
            {
                try
                {
                    Directory.CreateDirectory(pasta);
                    using var form = new ScannerForm(formato, pasta);
                    form.ShowDialog();
                    tcs.TrySetResult((form.Resultado, form.UltimoErro));
                }
                catch (Exception ex)
                {
                    Log.Erro("falha ao abrir a janela do scanner", ex);
                    tcs.TrySetException(ex);
                }
            }));

            var (resultado, erro) = await tcs.Task;

            if (resultado != null)
            {
                _ultima.Registrar(resultado);
                Log.Info($"digitalizacao concluida: {resultado.CaminhoArquivo} ({resultado.Paginas} pagina(s))");
            }
            else
            {
                Log.Info($"digitalizacao encerrada sem arquivo: {erro ?? "cancelada pelo operador"}");
                try { if (Directory.Exists(pasta) && !Directory.EnumerateFileSystemEntries(pasta).Any()) Directory.Delete(pasta); }
                catch { }
            }

            return (resultado, erro);
        }
        finally
        {
            _exclusao.Release();
        }
    }
}
