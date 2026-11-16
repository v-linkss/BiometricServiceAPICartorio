using System.Runtime.InteropServices;
using WIA;

namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Janela de digitalizacao. Descende do WiaMultiPageScan (Program.cs, 834 linhas),
/// mas aqui cuida so da interface e de decidir QUEM digitaliza:
///
///   duplex   -> TwainAquisicao   (o WIA nao consegue; ver README)
///   simplex  -> WiaAquisicao     (validado, e nao exige data source instalado)
///
/// Se o TWAIN nao produzir paginas — sem data source, driver ausente, falha — cai
/// para o WIA sozinho. O atendente nunca fica sem digitalizar por causa da
/// escolha de tecnologia.
///
/// Tratamento de pagina, PDF e limpeza vivem em <see cref="Imagens"/>; as
/// mensagens de erro do WIA, em <see cref="ErrosWia"/>. Os dois sao testaveis sem
/// scanner, e e onde estao as regras que mais afetam o resultado no cartorio.
/// </summary>
public class ScannerForm : Form
{
    private readonly CheckBox _multiPagina;
    private readonly CheckBox _duplex;
    private readonly Button _rotacionar;
    private readonly Button _escanear;
    private readonly Button _selecionarScanner;
    private readonly Label _carregando;
    private readonly PictureBox _previa;

    private readonly FormatoDigitalizacao _formato;
    private readonly string _pastaSaida;

    private readonly List<string> _paginas = new();
    private Image? _imagemAtual;
    private Device? _scanner;
    private bool _duplexSolicitado;
    private string? _avisoDuplex;

    /// <summary>Preenchido ao concluir. Null = cancelado ou nada escaneado.</summary>
    public ResultadoDigitalizacao? Resultado { get; private set; }

    /// <summary>
    /// Motivo da ultima falha, em portugues, para subir ate o front em vez de um
    /// generico "nenhuma pagina escaneada".
    /// </summary>
    public string? UltimoErro { get; private set; }

    public ScannerForm(FormatoDigitalizacao formato, string pastaSaida)
    {
        _formato = formato;
        _pastaSaida = pastaSaida;

        Text = $"Scanner 360 — {(formato == FormatoDigitalizacao.Pdf ? "PDF" : "Imagem")}";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = Recursos.CarregarGrande();

        ConectarScannerPadrao();

        // Multipagina e duplex so fazem sentido em PDF — mesma regra do original.
        bool ehPdf = formato == FormatoDigitalizacao.Pdf;

        var painel = new Panel { Dock = DockStyle.Left, Width = 250, Padding = new Padding(10) };

        _carregando = new Label
        {
            Text = "Escaneando...",
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.Red,
            Visible = false,
            TextAlign = ContentAlignment.MiddleCenter
        };
        painel.Controls.Add(_carregando);

        _escanear = new Button { Text = "Escanear", Dock = DockStyle.Top, Height = 40 };
        _escanear.Click += async (_, _) => await EscanearAsync();
        painel.Controls.Add(_escanear);

        _multiPagina = new CheckBox { Text = "Escanear múltiplas páginas", Dock = DockStyle.Top, Height = 30, Enabled = ehPdf };
        painel.Controls.Add(_multiPagina);

        _duplex = new CheckBox { Text = "Scan frente e verso (Duplex)", Dock = DockStyle.Top, Height = 30, Enabled = ehPdf };
        painel.Controls.Add(_duplex);

        _rotacionar = new Button { Text = "Rotacionar imagem", Dock = DockStyle.Top, Height = 40, Enabled = false };
        _rotacionar.Click += Rotacionar_Click;
        painel.Controls.Add(_rotacionar);

        _selecionarScanner = new Button { Text = "Selecionar Scanner", Dock = DockStyle.Top, Height = 40 };
        _selecionarScanner.Click += SelecionarScanner_Click;
        painel.Controls.Add(_selecionarScanner);

        Controls.Add(painel);

        _previa = new PictureBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };
        Controls.Add(_previa);
    }

    // ------------------------------------------------------------------ scanner

    private void ConectarScannerPadrao()
    {
        try
        {
            var gerenciador = new DeviceManager();
            for (int i = 1; i <= gerenciador.DeviceInfos.Count; i++)
            {
                var info = gerenciador.DeviceInfos[i];
                if (info.Type != WiaDeviceType.ScannerDeviceType) continue;
                _scanner = info.Connect();
                break;
            }

            if (_scanner != null) Log.Info($"scanner padrao: {NomeDoScanner(_scanner)}");
            else Log.Aviso("nenhum scanner WIA encontrado");
        }
        catch (Exception ex)
        {
            Log.Erro("falha ao conectar no scanner padrao", ex);
        }
    }

    private static string NomeDoScanner(Device scanner)
    {
        try { return scanner.Properties["Name"].get_Value()?.ToString() ?? ""; }
        catch { return ""; }
    }

    private void SelecionarScanner_Click(object? sender, EventArgs e)
    {
        using var selecao = new ScannerSelectionForm();
        if (selecao.ShowDialog(this) != DialogResult.OK || selecao.SelectedScanner == null) return;

        _scanner = selecao.SelectedScanner;
        MessageBox.Show(this, $"Scanner selecionado: {NomeDoScanner(_scanner)}");
    }

    // ---------------------------------------------------------------- digitalizar

    private async Task EscanearAsync()
    {
        if (_scanner == null)
        {
            MessageBox.Show(this, "Por favor, selecione um scanner antes de escanear.");
            return;
        }

        _escanear.Enabled = false;
        _rotacionar.Enabled = false;
        _carregando.Visible = true;

        // Nova tentativa nao herda o estado da anterior.
        _paginas.Clear();
        UltimoErro = null;
        _avisoDuplex = null;
        _imagemAtual?.Dispose();
        _imagemAtual = null;
        _previa.Image = null;

        bool duplex = _duplex.Checked;
        bool multiPagina = _multiPagina.Checked;
        _duplexSolicitado = duplex;

        // Handle lido na thread de UI: e a janela que recebe as mensagens do data
        // source TWAIN.
        IntPtr janela = Handle;

        // SEM Task.Run no TWAIN: ele precisa ser aberto na thread de UI e, ao mesmo
        // tempo, nao pode bloquea-la. Ver o comentario em TwainAquisicao.
        bool resolvido = duplex && await DigitalizarPorTwainAsync(janela, multiPagina);

        if (!resolvido)
            await Task.Run(() => DigitalizarPorWia(duplex, multiPagina));

        if (_paginas.Count > 0) Finalizar();

        _carregando.Visible = false;
        _escanear.Enabled = true;
    }

    /// <summary>
    /// Devolve true se produziu paginas. Falhar aqui nao e fatal: quem chama cai
    /// para o WIA. Por isso a mensagem ao atendente sai do caminho que realmente
    /// terminar, e nao daqui — avisar duas vezes seria pior do que nao avisar.
    /// </summary>
    private async Task<bool> DigitalizarPorTwainAsync(IntPtr janela, bool multiplasPaginas)
    {
        try
        {
            using var twain = new TwainAquisicao();

            var fontes = twain.ListarFontes(janela);
            if (fontes.Count == 0)
            {
                Log.Aviso("TWAIN: nenhum data source; usando WIA");
                return false;
            }
            Log.Info($"TWAIN: data sources disponiveis: {string.Join(", ", fontes)}");

            var (brutos, erro) = await twain.DigitalizarAsync(janela, _pastaSaida, true, multiplasPaginas, null);
            if (brutos.Count == 0)
            {
                Log.Aviso($"TWAIN nao produziu paginas ({erro ?? "sem erro reportado"}); tentando WIA");
                return false;
            }

            AceitarPaginas(brutos);
            Log.Info($"TWAIN: {brutos.Count} pagina(s) recebida(s), {_paginas.Count} apos descarte de brancos");
            return _paginas.Count > 0;
        }
        catch (Exception ex)
        {
            Log.Erro("TWAIN falhou; tentando WIA", ex);
            return false;
        }
    }

    private void DigitalizarPorWia(bool duplex, bool multiplasPaginas)
    {
        if (_scanner == null) return;

        var resultado = new WiaAquisicao(_scanner).Digitalizar(_pastaSaida, duplex, multiplasPaginas);

        _paginas.AddRange(resultado.Arquivos);
        _avisoDuplex ??= resultado.Aviso;

        if (resultado.Erro != null && _paginas.Count == 0)
        {
            UltimoErro = resultado.Erro;
            Avisar(resultado.Erro);
        }
    }

    /// <summary>Descarta versos em branco e reduz o que sobrou.</summary>
    private void AceitarPaginas(IEnumerable<string> brutos)
    {
        foreach (var arquivo in brutos)
        {
            if (Imagens.EstaEmBranco(arquivo))
            {
                Imagens.Apagar(new[] { arquivo });
                continue;
            }
            Imagens.Redimensionar(arquivo);
            _paginas.Add(arquivo);
        }
    }

    // ------------------------------------------------------------------ concluir

    private void Finalizar()
    {
        Directory.CreateDirectory(_pastaSaida);
        var destino = Path.Combine(_pastaSaida, _formato.NomeArquivo());

        try
        {
            if (_formato == FormatoDigitalizacao.Pdf)
            {
                Imagens.MontarPdf(_paginas, destino);
                Imagens.Apagar(_paginas);
                Resultado = new ResultadoDigitalizacao(destino, _formato, _paginas.Count, DateTime.Now);
                Log.Info($"PDF gerado com {_paginas.Count} pagina(s): {destino}");
            }
            else
            {
                if (File.Exists(destino)) File.Delete(destino);
                File.Move(_paginas[0], destino);

                // As demais paginas nao entram numa saida de imagem unica: sao lixo.
                Imagens.Apagar(_paginas.Skip(1));

                Resultado = new ResultadoDigitalizacao(destino, _formato, 1, DateTime.Now);
                MostrarPrevia(destino);
                _rotacionar.Enabled = true;
                Log.Info($"imagem gerada: {destino}");
            }

            // Avisar quando o duplex foi pedido e nao pode ser usado: sem isto o
            // atendente acha que digitalizou os dois lados e so descobre o
            // contrario quando alguem procura o verso no documento.
            MessageBox.Show(this,
                $"Documento escaneado com sucesso!\nPáginas: {Resultado.Paginas}" +
                (_avisoDuplex != null ? $"\n\n{_avisoDuplex}" : ""));
            Close();
        }
        catch (Exception ex)
        {
            Log.Erro("falha ao montar o arquivo final", ex);
            MessageBox.Show(this, $"Erro ao finalizar a digitalização: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------- previa

    private void MostrarPrevia(string caminho)
    {
        _imagemAtual?.Dispose();
        _imagemAtual = Imagens.CarregarSemTravar(caminho);
        _previa.Image = _imagemAtual;
    }

    private void Rotacionar_Click(object? sender, EventArgs e)
    {
        if (_imagemAtual == null) return;

        _imagemAtual.RotateFlip(RotateFlipType.Rotate90FlipNone);
        _previa.Image = _imagemAtual;

        // O original rotacionava so na tela, e o arquivo subia sem a rotacao — o
        // atendente endireitava a imagem e mandava torta assim mesmo.
        try
        {
            if (Resultado is { Formato: FormatoDigitalizacao.Imagem } r && File.Exists(r.CaminhoArquivo))
            {
                _imagemAtual.Save(r.CaminhoArquivo, System.Drawing.Imaging.ImageFormat.Png);
                Log.Info("rotacao gravada no arquivo");
            }
        }
        catch (Exception ex)
        {
            Log.Erro("falha ao gravar rotacao", ex);
        }
    }

    /// <summary>MessageBox a partir de thread que nao e a de UI.</summary>
    private void Avisar(string mensagem)
    {
        try
        {
            if (IsHandleCreated && !IsDisposed)
                Invoke(new Action(() => MessageBox.Show(this, mensagem, "Scanner 360",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)));
        }
        catch { /* janela ja fechada: a mensagem ainda vai no log e na resposta HTTP */ }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _imagemAtual?.Dispose();
            if (_scanner != null)
            {
                try { Marshal.ReleaseComObject(_scanner); } catch { }
                _scanner = null;
            }
        }
        base.Dispose(disposing);
    }
}
