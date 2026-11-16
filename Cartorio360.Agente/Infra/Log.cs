using System.Text;

namespace Cartorio360.Agente.Infra;

/// <summary>
/// Log em arquivo, com rotacao diaria.
///
/// Proposital nao usar Serilog: a unica coisa que o agente precisa e uma linha
/// por evento num arquivo que o suporte consiga abrir pelo menu da bandeja. Uma
/// dependencia a menos e um problema a menos no restore offline da estacao.
///
/// O WiaMultiPageScan original despejava o diagnostico de duplex em
/// Console.WriteLine — que num WinExe nao vai a lugar nenhum. Aqui esse mesmo
/// diagnostico passa a ficar gravado, que e justamente o que se quer quando um
/// scanner especifico nao duplexa no cartorio.
/// </summary>
public static class Log
{
    private static readonly object _trava = new();
    private static string _pasta = Path.Combine(AppContext.BaseDirectory, "logs");

    public static void Configurar(string pastaLogs)
    {
        _pasta = pastaLogs;
        try { Directory.CreateDirectory(_pasta); } catch { /* ver Escrever */ }
    }

    public static string PastaAtual => _pasta;

    public static void Info(string mensagem) => Escrever("INFO ", mensagem);
    public static void Aviso(string mensagem) => Escrever("AVISO", mensagem);
    public static void Erro(string mensagem) => Escrever("ERRO ", mensagem);

    public static void Erro(string mensagem, Exception ex)
        => Escrever("ERRO ", $"{mensagem} :: {ex.GetType().Name}: {ex.Message}");

    private static void Escrever(string nivel, string mensagem)
    {
        var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{nivel}] {mensagem}";
        try
        {
            lock (_trava)
            {
                Directory.CreateDirectory(_pasta);
                var arquivo = Path.Combine(_pasta, $"agente-{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(arquivo, linha + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Falhar ao gravar log nunca pode derrubar uma digitalizacao ou uma
            // impressao. Se a pasta estiver sem permissao (o caso do icacls
            // faltando), o agente segue funcionando, apenas cego.
        }
    }
}
