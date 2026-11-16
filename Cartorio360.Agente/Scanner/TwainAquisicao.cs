using System.Collections.Concurrent;
using NTwain;
using NTwain.Data;

namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Aquisicao via TWAIN. E o caminho usado quando o atendente pede frente e verso.
///
/// Por que existe, ao lado do WIA que ja funcionava:
/// no Canon DR-C230 o WIA declara duplex em 3086 e 3088 e recusa qualquer escrita
/// na propriedade (E_POINTER) — anuncia e nao entrega. Pelo TWAIN, o mesmo
/// aparelho responde CAP_DUPLEXENABLED com CanSet=True e liga na primeira
/// tentativa. Medido, nao suposto: ver exeScanner/tools/SondaTwain.
///
/// O WIA continua no codigo como reserva: cobre scanner sem data source TWAIN, e
/// e o que ja estava validado para simplex e multipagina.
/// </summary>
public sealed class TwainAquisicao : IDisposable
{
    private readonly TwainSession _sessao;
    private bool _dsmAberto;

    public TwainAquisicao()
    {
        // Identidade montada a mao, e NAO por TwainSession(DataGroups) nem por
        // TWIdentity.CreateFromAssembly.
        //
        // Aqueles caminhos leem Assembly.Location para descobrir nome e versao do
        // aplicativo — e num publish single-file, como e o do agente,
        // Assembly.Location vem VAZIO. O resultado e
        // ArgumentException("The path is empty") no construtor, antes de qualquer
        // conversa com o scanner. Falha que so aparece no build publicado: em
        // `dotnet run` funciona, porque ali existe um .dll em disco.
        var identidade = TWIdentity.Create(
            DataGroups.Image,
            new Version(2, 0, 0),
            "Cartorio 360",
            "Agente 360",
            "Agente 360",
            "Scanner, upload e impressao de etiquetas de selo");

        _sessao = new TwainSession(identidade);
    }

    /// <summary>
    /// Data sources visiveis. Lista vazia = nao ha TWAIN utilizavel nesta
    /// estacao, e quem chama deve cair para o WIA.
    /// </summary>
    public IReadOnlyList<string> ListarFontes(IntPtr janela)
    {
        try
        {
            if (!Abrir(janela)) return Array.Empty<string>();
            return _sessao.GetSources().Select(f => f.Name).ToList();
        }
        catch (Exception ex)
        {
            Log.Aviso($"TWAIN: falha ao listar data sources: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    private bool Abrir(IntPtr janela)
    {
        if (_dsmAberto) return true;

        // O hook entrega ao NTwain as mensagens que o data source manda para a
        // janela. Sem ele o TWAIN nao conversa: o protocolo e baseado em mensagens
        // do Windows, e por isso a digitalizacao exige uma janela com message pump
        // — mesma razao de o agente ser app de bandeja e nao servico.
        var rc = _sessao.Open(new WindowsFormsMessageLoopHook(janela));
        if (rc != ReturnCode.Success)
        {
            Log.Aviso($"TWAIN: DSM nao abriu ({rc})");
            return false;
        }
        _dsmAberto = true;
        Log.Info($"TWAIN: DSM aberto (estado {_sessao.State})");
        return true;
    }

    /// <summary>
    /// Digitaliza e devolve os arquivos PNG gerados, em ordem de chegada.
    ///
    /// ASSINCRONO DE PROPOSITO, e chamado NA THREAD DE UI. O TWAIN e um protocolo
    /// de mensagens do Windows: o data source responde postando mensagens para a
    /// janela que registramos no hook. Isso cria duas amarras que se contradizem
    /// se ignoradas:
    ///   - o hook so pode ser criado na thread de UI (do contrario:
    ///     "This can only be created on the Windows Forms UI thread");
    ///   - a thread de UI nao pode ficar BLOQUEADA esperando, senao o message pump
    ///     para e as mensagens do scanner nunca sao processadas.
    /// A saida e nao bloquear: dispara na UI e aguarda por evento, com await. Uma
    /// espera bloqueante aqui (Task.Run + ManualResetEvent) trava de um jeito ou
    /// de outro.
    ///
    /// No duplex de passagem unica o aparelho entrega frente e verso como paginas
    /// consecutivas — nao ha segunda transferencia a fazer, ao contrario do que o
    /// caminho WIA precisava tentar.
    /// </summary>
    public async Task<(IReadOnlyList<string> Arquivos, string? Erro)> DigitalizarAsync(
        IntPtr janela,
        string pastaSaida,
        bool duplex,
        bool multiplasPaginas,
        string? fontePreferida)
    {
        string? erro = null;
        var arquivos = new ConcurrentQueue<string>();

        if (!Abrir(janela)) return (Array.Empty<string>(), "Não foi possível iniciar o TWAIN nesta estação.");

        var fontes = _sessao.GetSources().ToList();
        if (fontes.Count == 0) return (Array.Empty<string>(), "Nenhum scanner TWAIN encontrado.");

        var fonte = (!string.IsNullOrWhiteSpace(fontePreferida)
                        ? fontes.FirstOrDefault(f => f.Name.Equals(fontePreferida, StringComparison.OrdinalIgnoreCase))
                        : null)
                    ?? _sessao.DefaultSource
                    ?? fontes[0];

        Log.Info($"TWAIN: usando '{fonte.Name}' [{fonte.Manufacturer}] protocolo {fonte.ProtocolVersion}");

        var rc = fonte.Open();
        if (rc != ReturnCode.Success) return (Array.Empty<string>(), $"Não foi possível abrir o scanner ({rc}).");

        var terminou = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        string? erroTransferencia = null;
        int pagina = 0;

        void AoTransferir(object? s, DataTransferredEventArgs e)
        {
            try
            {
                using var fluxo = e.GetNativeImageStream();
                if (fluxo == null) { Log.Aviso("TWAIN: transferencia sem imagem"); return; }

                Directory.CreateDirectory(pastaSaida);
                var caminho = Path.Combine(pastaSaida, $"Page{++pagina}.png");

                using (var img = Image.FromStream(fluxo))
                    img.Save(caminho, System.Drawing.Imaging.ImageFormat.Png);

                arquivos.Enqueue(caminho);
                Log.Info($"TWAIN: pagina {pagina} recebida");
            }
            catch (Exception ex)
            {
                Log.Erro("TWAIN: falha ao gravar pagina", ex);
            }
        }

        void AoErrar(object? s, TransferErrorEventArgs e)
        {
            erroTransferencia = e.Exception?.Message ?? e.ReturnCode.ToString();
            Log.Aviso($"TWAIN: erro de transferencia: {erroTransferencia}");
        }

        void AoDesabilitar(object? s, EventArgs e) => terminou.TrySetResult(true);

        _sessao.DataTransferred += AoTransferir;
        _sessao.TransferError += AoErrar;
        _sessao.SourceDisabled += AoDesabilitar;

        try
        {
            ConfigurarCapacidades(fonte, duplex, multiplasPaginas);

            // NoUI: quem desenha a tela e o agente, nao o driver. Isso mantem o
            // atendente na mesma interface de sempre.
            rc = fonte.Enable(SourceEnableMode.NoUI, false, janela);
            if (rc != ReturnCode.Success)
                return (Array.Empty<string>(), $"O scanner não aceitou iniciar a digitalização ({rc}).");

            // await, e nao Wait(): a thread de UI precisa continuar bombeando
            // mensagens enquanto o scanner trabalha. O limite existe para a janela
            // nao ficar presa para sempre se o driver nunca responder.
            var concluiu = await Task.WhenAny(terminou.Task, Task.Delay(TimeSpan.FromMinutes(10)));
            if (concluiu != terminou.Task)
            {
                erro = "O scanner não respondeu no tempo esperado.";
                try { _sessao.ForceStepDown(4); } catch { }
            }
        }
        catch (Exception ex)
        {
            Log.Erro("TWAIN: falha na digitalizacao", ex);
            erro = $"Falha na digitalização: {ex.Message}";
        }
        finally
        {
            _sessao.DataTransferred -= AoTransferir;
            _sessao.TransferError -= AoErrar;
            _sessao.SourceDisabled -= AoDesabilitar;
            try { if (fonte.IsOpen) fonte.Close(); } catch { }
        }

        if (erro == null && erroTransferencia != null && arquivos.IsEmpty)
            erro = $"Falha na digitalização: {erroTransferencia}";

        return (arquivos.ToList(), erro);
    }

    private static void ConfigurarCapacidades(DataSource fonte, bool duplex, bool multiplasPaginas)
    {
        var caps = fonte.Capabilities;
        Log.Info($"TWAIN: configurando duplex={duplex} multipagina={multiplasPaginas}");

        Definir("alimentador", () => caps.CapFeederEnabled.CanSet
            ? caps.CapFeederEnabled.SetValue(BoolType.True) : ReturnCode.Failure);

        if (duplex)
        {
            // O interruptor que o WIA nao deixava mexer.
            if (caps.CapDuplexEnabled.CanSet)
            {
                var r = caps.CapDuplexEnabled.SetValue(BoolType.True);
                var lido = caps.CapDuplexEnabled.CanGet ? caps.CapDuplexEnabled.GetCurrent().ToString() : "?";
                Log.Info($"TWAIN: duplex -> {r} (releitura: {lido})");
            }
            else
            {
                Log.Aviso("TWAIN: CAP_DUPLEXENABLED nao e gravavel neste scanner");
            }
        }
        else
        {
            if (caps.CapDuplexEnabled.CanSet)
                Definir("duplex desligado", () => caps.CapDuplexEnabled.SetValue(BoolType.False));
        }

        // CAP_XFERCOUNT: -1 significa "transfira tudo que houver no alimentador".
        // Sem multipagina, limita ao necessario para uma folha — 2 no duplex
        // (frente e verso), 1 no simplex.
        short quantidade = (short)(multiplasPaginas ? -1 : (duplex ? 2 : 1));
        if (caps.CapXferCount.CanSet)
        {
            var r = caps.CapXferCount.SetValue(quantidade);
            var lido = caps.CapXferCount.CanGet ? caps.CapXferCount.GetCurrent().ToString() : "?";
            Log.Info($"TWAIN: XferCount = {quantidade} -> {r} (releitura: {lido})");
        }
        else
        {
            Log.Aviso("TWAIN: CAP_XFERCOUNT nao e gravavel; o scanner decide quantas paginas transferir");
        }

        Definir("modo de cor", () => caps.ICapPixelType.CanSet
            ? caps.ICapPixelType.SetValue(PixelType.RGB) : ReturnCode.Failure);

        Definir("unidade", () => caps.ICapUnits.CanSet
            ? caps.ICapUnits.SetValue(Unit.Inches) : ReturnCode.Failure);

        Definir("resolucao horizontal", () => caps.ICapXResolution.CanSet
            ? caps.ICapXResolution.SetValue(150) : ReturnCode.Failure);

        Definir("resolucao vertical", () => caps.ICapYResolution.CanSet
            ? caps.ICapYResolution.SetValue(150) : ReturnCode.Failure);
    }

    private static void Definir(string oQue, Func<ReturnCode> acao)
    {
        try
        {
            var rc = acao();
            if (rc != ReturnCode.Success) Log.Aviso($"TWAIN: {oQue} -> {rc}");
        }
        catch (Exception ex)
        {
            Log.Aviso($"TWAIN: {oQue} falhou: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_dsmAberto)
            {
                if (_sessao.IsSourceOpen) _sessao.CurrentSource?.Close();
                _sessao.Close();
                _dsmAberto = false;
            }
        }
        catch (Exception ex)
        {
            Log.Aviso($"TWAIN: falha ao encerrar a sessao: {ex.Message}");
        }
    }
}
