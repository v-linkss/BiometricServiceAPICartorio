using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Cartorio360.Agente.Servicos;

public sealed record FilaImpressao(string Nome, string Driver, string Porta)
{
    /// <summary>
    /// Uma fila em LPTx quase sempre e residuo de instalacao feita sem a
    /// impressora conectada, ou consequencia de o device ter virado WinUSB pelo
    /// Zadig (sem porta USB para a fila apontar). Nos dois casos o spooler ACEITA
    /// o job e nao sai papel — falha silenciosa. Vale avisar em vez de deixar o
    /// suporte adivinhando.
    /// </summary>
    public bool PortaSuspeita => Porta.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)
                              || Porta.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                              // WSD e descoberta por rede: sobra de aparelho que
                              // ja foi removido, e o job morre com
                              // ERROR_PRINTER_DELETED.
                              || Porta.StartsWith("WSD", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Envia ZPL para a Zebra como job RAW pelo spooler do Windows.
///
/// Por que nao libusb, como o ObserverUpload faz: falar libusb com o device
/// obriga a trocar o driver da impressora por WinUSB, com o Zadig, em cada
/// estacao. Isso quebra a impressao pelo spooler e e o passo mais arriscado de
/// automatizar num instalador. Pelo spooler a Zebra fica com o driver normal e o
/// instalador nao toca em driver nenhum.
/// </summary>
public sealed class ZebraPrinter
{
    private static readonly Regex PadraoZebra =
        new("Zebra|ZDesigner|ZPL", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Separa as etiquetas do bloco ZPL. Nao-guloso de proposito: o front manda
    // varias etiquetas num corpo so, e cada uma precisa virar um job — mesmo
    // comportamento do laco do ObserverUpload.
    private static readonly Regex PadraoEtiqueta =
        new(@"\^XA[\s\S]*?\^XZ", RegexOptions.Compiled);

    // "(Copiar N)" em portugues, "(Copy N)" em ingles: sufixo que o Windows poe
    // ao adicionar a mesma impressora de novo. Fila assim e quase sempre residuo.
    private static readonly Regex PadraoCopia =
        new(@"\((Copiar|Copy)\s*\d*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Configuracao _config;

    public ZebraPrinter(Configuracao config) => _config = config;

    public static bool ZplValido(string? zpl)
        => !string.IsNullOrWhiteSpace(zpl)
           && (zpl.Contains("^XA") || zpl.Contains("^XZ") || zpl.TrimStart().StartsWith("^"));

    /// <summary>
    /// Separa o bloco ZPL em etiquetas individuais.
    ///
    /// Publico para poder ser testado sem impressora: o front manda um LOTE de
    /// selos num corpo so, e cada etiqueta precisa virar um job separado. Se este
    /// fatiamento quebrar, o lote sai como uma etiqueta gigante ou uma so — falha
    /// que so apareceria com papel na mao.
    /// </summary>
    public static IReadOnlyList<string> FatiarEtiquetas(string zpl)
        => PadraoEtiqueta.Matches(zpl).Select(m => m.Value).ToList();

    /// <summary>
    /// Fila a usar. Null = nao ha Zebra alcancavel.
    /// </summary>
    public FilaImpressao? Localizar() => LocalizarCandidatas().FirstOrDefault();

    /// <summary>
    /// Todas as filas Zebra, da mais provavel para a menos.
    ///
    /// Ordenar importa porque estacao de cartorio acumula fila duplicada: o
    /// Windows cria "(Copiar N)" a cada reinstalacao, e sobram filas apontando
    /// para portas que nao existem mais. Um caso real: uma fila em porta WSD
    /// (descoberta por rede, aparelho ja removido) vinha antes da boa em USB001,
    /// e a impressao morria com ERROR_PRINTER_DELETED.
    ///
    /// Criterios, do mais forte para o mais fraco:
    ///   1. porta USB    - e como a etiquetadora fica ligada de fato
    ///   2. nao ser copia - "(Copiar N)" e residuo de reinstalacao
    ///   3. nao ser WSD nem porta serial/paralela
    /// </summary>
    public IReadOnlyList<FilaImpressao> LocalizarCandidatas()
    {
        var filas = ListarFilas();

        if (!string.IsNullOrWhiteSpace(_config.ImpressoraZebra))
        {
            var escolhida = filas.FirstOrDefault(f =>
                string.Equals(f.Nome, _config.ImpressoraZebra, StringComparison.OrdinalIgnoreCase));
            if (escolhida != null) return new[] { escolhida };
            Log.Aviso($"impressoraZebra='{_config.ImpressoraZebra}' configurada mas nao encontrada; caindo para deteccao");
        }

        var candidatas = filas
            .Where(f => PadraoZebra.IsMatch(f.Nome) || PadraoZebra.IsMatch(f.Driver))
            .OrderByDescending(f => f.Porta.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(f => !PadraoCopia.IsMatch(f.Nome))
            .ThenByDescending(f => !f.PortaSuspeita)
            .ToList();

        if (candidatas.Count > 1)
            Log.Info($"{candidatas.Count} filas Zebra; ordem de tentativa: " +
                     string.Join(" > ", candidatas.Select(f => $"{f.Nome} ({f.Porta})")));

        return candidatas;
    }

    public IReadOnlyList<FilaImpressao> ListarFilas()
    {
        var resultado = new List<FilaImpressao>();
        try
        {
            // EnumPrinters nivel 2 em vez de PrinterSettings.InstalledPrinters:
            // precisamos tambem do driver e da PORTA, e e a porta que denuncia o
            // caso do LPT1.
            const int PRINTER_ENUM_LOCAL = 0x00000002;
            const int PRINTER_ENUM_CONNECTIONS = 0x00000004;
            int flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;

            EnumPrinters(flags, null, 2, IntPtr.Zero, 0, out int necessario, out _);
            if (necessario <= 0) return resultado;

            IntPtr buffer = Marshal.AllocHGlobal(necessario);
            try
            {
                if (!EnumPrinters(flags, null, 2, buffer, necessario, out _, out int quantidade))
                {
                    Log.Aviso($"EnumPrinters falhou (Win32 {Marshal.GetLastWin32Error()})");
                    return resultado;
                }

                int tamanho = Marshal.SizeOf<PRINTER_INFO_2>();
                for (int i = 0; i < quantidade; i++)
                {
                    var info = Marshal.PtrToStructure<PRINTER_INFO_2>(buffer + i * tamanho);
                    resultado.Add(new FilaImpressao(
                        info.pPrinterName ?? "",
                        info.pDriverName ?? "",
                        info.pPortName ?? ""));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            // Falha ao enumerar e indistinguivel de "nao tem Zebra" para quem
            // chama: nos dois casos nao ha para onde mandar ZPL. Nunca propagar,
            // porque o /status precisa continuar respondendo 200.
            Log.Erro("falha ao enumerar impressoras", ex);
        }
        return resultado;
    }

    /// <summary>
    /// Imprime tentando as filas em ordem de probabilidade, ate uma aceitar.
    ///
    /// A retentativa existe porque uma fila pode estar viva no registro e morta
    /// na pratica: o caso real foi uma "(Copiar 4)" em porta WSD, que so revelou
    /// o problema no StartDocPrinter, com ERROR_PRINTER_DELETED (Win32 1905).
    /// Desistir na primeira transformava isso em chamado de suporte.
    /// </summary>
    public (int Quantidade, FilaImpressao Fila) Imprimir(string zpl)
    {
        var candidatas = LocalizarCandidatas();
        if (candidatas.Count == 0)
            throw new InvalidOperationException("Nenhuma impressora Zebra encontrada conectada via USB");

        Exception? ultimaFalha = null;
        foreach (var fila in candidatas)
        {
            try
            {
                return (Imprimir(fila, zpl), fila);
            }
            catch (Exception ex)
            {
                ultimaFalha = ex;
                Log.Aviso($"falha em '{fila.Nome}' ({fila.Porta}): {ex.Message}");
            }
        }

        throw ultimaFalha ?? new InvalidOperationException("Não foi possível imprimir.");
    }

    /// <summary>
    /// Fatia o ZPL em etiquetas e manda uma por job numa fila especifica.
    /// Devolve quantas foram.
    /// </summary>
    public int Imprimir(FilaImpressao fila, string zpl)
    {
        var etiquetas = FatiarEtiquetas(zpl);
        if (etiquetas.Count == 0)
            throw new InvalidOperationException("Nenhuma etiqueta ZPL válida encontrada.");

        if (fila.PortaSuspeita)
            Log.Aviso($"fila '{fila.Nome}' esta na porta {fila.Porta}: o job pode ser aceito sem sair papel");

        foreach (var etiqueta in etiquetas)
        {
            var erro = EnviarRaw(fila.Nome, Encoding.ASCII.GetBytes(etiqueta));
            if (erro != null)
                throw new InvalidOperationException($"Falha ao imprimir na '{fila.Nome}': {erro}");
        }

        Log.Info($"{etiquetas.Count} etiqueta(s) enviada(s) para '{fila.Nome}' ({fila.Porta})");
        return etiquetas.Count;
    }

    // ---- winspool.drv -------------------------------------------------------

    private static string? EnviarRaw(string impressora, byte[] dados)
    {
        if (!OpenPrinter(impressora, out IntPtr hPrinter, IntPtr.Zero))
            return $"OpenPrinter falhou (Win32 {Marshal.GetLastWin32Error()})";

        try
        {
            var di = new DOCINFOA
            {
                pDocName = "Cartorio360 - etiqueta de selo",
                pDataType = "RAW"   // sem isto o driver renderiza como grafico
            };

            if (!StartDocPrinter(hPrinter, 1, di))
                return $"StartDocPrinter falhou (Win32 {Marshal.GetLastWin32Error()})";

            try
            {
                if (!StartPagePrinter(hPrinter))
                    return $"StartPagePrinter falhou (Win32 {Marshal.GetLastWin32Error()})";

                IntPtr buffer = Marshal.AllocCoTaskMem(dados.Length);
                try
                {
                    Marshal.Copy(dados, 0, buffer, dados.Length);
                    if (!WritePrinter(hPrinter, buffer, dados.Length, out int escritos))
                        return $"WritePrinter falhou (Win32 {Marshal.GetLastWin32Error()})";
                    if (escritos != dados.Length)
                        return $"WritePrinter gravou {escritos} de {dados.Length} bytes";
                }
                finally { Marshal.FreeCoTaskMem(buffer); }

                EndPagePrinter(hPrinter);
            }
            finally { EndDocPrinter(hPrinter); }
        }
        finally { ClosePrinter(hPrinter); }

        return null;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName = "";
        [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string pDataType = "RAW";
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PRINTER_INFO_2
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string? pServerName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pPrinterName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pShareName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pPortName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pDriverName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pComment;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pLocation;
        public IntPtr pDevMode;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pSepFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pPrintProcessor;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pDatatype;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level,
        [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum,
        int cbBuf, out int pcbNeeded, out int pcReturned);
}
