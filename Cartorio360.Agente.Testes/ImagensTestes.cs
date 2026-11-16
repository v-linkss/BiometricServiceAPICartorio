using System.Drawing;
using System.Drawing.Imaging;
using Cartorio360.Agente.Scanner;
using Xunit;

namespace Cartorio360.Agente.Testes;

public class ImagensTestes : IDisposable
{
    private readonly string _pasta;

    public ImagensTestes()
    {
        _pasta = Path.Combine(Path.GetTempPath(), "agente360-testes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
    }

    public void Dispose()
    {
        try { Directory.Delete(_pasta, recursive: true); } catch { }
    }

    private string GerarPng(int largura, int altura, Action<Graphics>? desenhar = null)
    {
        var caminho = Path.Combine(_pasta, $"{Guid.NewGuid():N}.png");
        using (var bmp = new Bitmap(largura, altura))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                desenhar?.Invoke(g);
            }
            bmp.Save(caminho, ImageFormat.Png);
        }
        return caminho;
    }

    // ---------------------------------------------------------------- em branco

    [Fact]
    public void Folha_totalmente_branca_e_descartada()
    {
        var caminho = GerarPng(600, 800);
        Assert.True(Imagens.EstaEmBranco(caminho));
    }

    [Fact]
    public void Folha_com_texto_e_mantida()
    {
        // Varias linhas pretas espalhadas: e o que uma ficha preenchida produz.
        var caminho = GerarPng(600, 800, g =>
        {
            using var caneta = new Pen(Color.Black, 3);
            for (int y = 50; y < 750; y += 40)
                g.DrawLine(caneta, 50, y, 550, y);
        });

        Assert.False(Imagens.EstaEmBranco(caminho));
    }

    [Fact]
    public void Folha_toda_escura_nao_e_branca()
    {
        var caminho = GerarPng(400, 400, g => g.Clear(Color.DimGray));
        Assert.False(Imagens.EstaEmBranco(caminho));
    }

    // ===================================================================
    // DEFEITO CONHECIDO, ainda nao corrigido — aguarda calibracao com
    // documento de cartorio real.
    //
    // Uma assinatura esparsa no rodape de uma folha em branco E DESCARTADA
    // como pagina vazia. A amostragem pula de 10 em 10 pixels: a 150 dpi sao
    // ~1,7 mm entre amostras, e um traco de caneta tem ~0,3 mm. Poucos tracos
    // curtos nao movem nem a proporcao de branco (fica >= 0,97) nem a de
    // bordas (fica < 0,01), e a folha e julgada vazia nos dois criterios.
    //
    // No simplex isso quase nao aparecia, porque a frente sempre tem conteudo.
    // No duplex e recorrente: os versos medidos no DR-C230 deram 0,9864 e
    // 0,9879 de branco — ACIMA do limiar — e so sobreviveram pelas bordas.
    //
    // Este teste fixa o comportamento ATUAL, para a refatoracao ser
    // verificavel. Ao corrigir, inverta a assercao.
    // ===================================================================
    [Fact]
    public void DEFEITO_Assinatura_esparsa_e_descartada_como_pagina_em_branco()
    {
        var caminho = GerarPng(600, 800, g =>
        {
            using var caneta = new Pen(Color.Black, 4);
            g.DrawLine(caneta, 80, 700, 300, 690);
            g.DrawLine(caneta, 90, 710, 280, 700);
            g.DrawLine(caneta, 100, 720, 260, 715);
        });

        Assert.True(Imagens.EstaEmBranco(caminho));
    }

    /// <summary>
    /// Onde fica a fronteira hoje: conteudo espalhado pela folha sobrevive, mesmo
    /// sendo pouco. O problema nao e a QUANTIDADE de tinta, e a CONCENTRACAO dela
    /// num canto — a amostragem esparsa nao ve o que esta agrupado.
    /// </summary>
    [Fact]
    public void Conteudo_pouco_mas_espalhado_e_mantido()
    {
        var caminho = GerarPng(600, 800, g =>
        {
            using var caneta = new Pen(Color.Black, 4);
            for (int y = 60; y < 780; y += 60)
                g.DrawLine(caneta, 60, y, 240, y);
        });

        Assert.False(Imagens.EstaEmBranco(caminho));
    }

    /// <summary>
    /// Prova de que da para corrigir por parametro, sem mexer no algoritmo: com o
    /// criterio de bordas mais sensivel, a mesma assinatura esparsa sobrevive.
    /// E o caminho provavel da calibracao.
    /// </summary>
    [Fact]
    public void Assinatura_esparsa_sobrevive_com_limiar_de_bordas_menor()
    {
        var caminho = GerarPng(600, 800, g =>
        {
            using var caneta = new Pen(Color.Black, 4);
            g.DrawLine(caneta, 80, 700, 300, 690);
            g.DrawLine(caneta, 90, 710, 280, 700);
            g.DrawLine(caneta, 100, 720, 260, 715);
        });

        Assert.False(Imagens.EstaEmBranco(caminho, limiarBordas: 0.001));
    }

    [Fact]
    public void Limiares_podem_ser_afrouxados_por_parametro()
    {
        var caminho = GerarPng(600, 800);
        // Limiar impossivel de atingir: nem folha vazia e considerada branca.
        Assert.False(Imagens.EstaEmBranco(caminho, limiarBranco: 1.01));
    }

    // ------------------------------------------------------------ redimensionar

    [Fact]
    public void Imagem_grande_e_reduzida_preservando_proporcao()
    {
        var caminho = GerarPng(2560, 1440);
        Imagens.Redimensionar(caminho, 1280, 720);

        using var img = Image.FromFile(caminho);
        Assert.Equal(1280, img.Width);
        Assert.Equal(720, img.Height);
    }

    [Fact]
    public void Proporcao_nao_e_distorcida_em_imagem_alta()
    {
        // Folha A4 retrato: cabe pela altura, e a largura acompanha.
        var caminho = GerarPng(1240, 1754);
        Imagens.Redimensionar(caminho, 1280, 720);

        using var img = Image.FromFile(caminho);
        Assert.Equal(720, img.Height);
        Assert.InRange(img.Width, 505, 512);   // 1240 * (720/1754) ~= 509
    }

    [Fact]
    public void Imagem_menor_que_o_limite_nao_e_ampliada()
    {
        var caminho = GerarPng(400, 300);
        Imagens.Redimensionar(caminho, 1280, 720);

        using var img = Image.FromFile(caminho);
        Assert.Equal(400, img.Width);
        Assert.Equal(300, img.Height);
    }

    [Fact]
    public void Arquivo_nao_fica_travado_apos_redimensionar()
    {
        var caminho = GerarPng(2000, 1000);
        Imagens.Redimensionar(caminho);

        // Se o Bitmap tivesse ficado aberto, isto lancaria IOException — e o
        // /upload nao conseguiria ler o arquivo depois da digitalizacao.
        File.Delete(caminho);
        Assert.False(File.Exists(caminho));
    }

    // -------------------------------------------------------------------- PDF

    [Fact]
    public void Pdf_tem_uma_pagina_por_imagem()
    {
        var imagens = Enumerable.Range(0, 4).Select(_ => GerarPng(600, 800)).ToList();
        var pdf = Path.Combine(_pasta, "doc_ficha.pdf");

        Imagens.MontarPdf(imagens, pdf);

        Assert.True(File.Exists(pdf));
        var bytes = File.ReadAllBytes(pdf);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

        var texto = System.Text.Encoding.Latin1.GetString(bytes);
        var paginas = System.Text.RegularExpressions.Regex.Matches(texto, @"/Type\s*/Page[^s]").Count;
        Assert.Equal(4, paginas);
    }

    [Fact]
    public void Pdf_sem_imagem_nenhuma_falha_em_vez_de_gerar_arquivo_vazio()
    {
        var pdf = Path.Combine(_pasta, "vazio.pdf");
        Assert.Throws<InvalidOperationException>(() => Imagens.MontarPdf(Array.Empty<string>(), pdf));
    }

    // ----------------------------------------------------------------- apagar

    [Fact]
    public void Apagar_remove_os_arquivos()
    {
        var arquivos = Enumerable.Range(0, 3).Select(_ => GerarPng(100, 100)).ToList();
        Imagens.Apagar(arquivos);
        Assert.All(arquivos, a => Assert.False(File.Exists(a)));
    }

    [Fact]
    public void Apagar_ignora_arquivo_que_nao_existe()
    {
        var inexistente = Path.Combine(_pasta, "nao-existe.png");
        Imagens.Apagar(new[] { inexistente });   // nao pode lancar
    }

    [Fact]
    public void CarregarSemTravar_permite_apagar_o_arquivo_depois()
    {
        var caminho = GerarPng(300, 200);
        using (var img = Imagens.CarregarSemTravar(caminho))
        {
            Assert.Equal(300, img.Width);
            File.Delete(caminho);   // travaria com Image.FromFile
        }
        Assert.False(File.Exists(caminho));
    }
}
