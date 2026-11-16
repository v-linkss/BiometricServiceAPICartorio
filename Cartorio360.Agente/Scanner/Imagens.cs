using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Tratamento das paginas digitalizadas: descarte de folha em branco,
/// redimensionamento e montagem do PDF.
///
/// Separado do formulario porque nao depende de scanner, de WIA nem de TWAIN — e
/// funcao pura sobre arquivo. E o unico pedaco do caminho de digitalizacao que da
/// para testar sem hardware, e e onde moram as regras que mais afetam o
/// resultado final para o cartorio.
/// </summary>
public static class Imagens
{
    public const int LarguraMaxima = 1280;
    public const int AlturaMaxima = 720;

    /// <summary>
    /// Decide se a folha veio em branco e deve ser descartada.
    ///
    /// Nao basta olhar brilho: uma ficha assinada a caneta e quase toda branca.
    /// Por isso ha dois criterios, e a pagina so e descartada se falhar nos DOIS —
    /// clara E uniforme.
    ///
    /// ATENCAO, medido no duplex do DR-C230: os VERSOS ficam no limite. Mediram
    /// 0,9864 e 0,9879 de branco, ambos ACIMA do limiar de 0,97, e so nao foram
    /// descartados porque a variacao entre pixels vizinhos passou de 0,01. Um
    /// verso com menos conteudo — assinatura solta, carimbo pequeno — cai nos dois
    /// criterios e se perde. Calibrar com documento de cartorio real antes de
    /// liberar; os numeros estao no log de cada digitalizacao.
    /// </summary>
    public static bool EstaEmBranco(
        string caminho,
        double limiarBranco = 0.97,
        int limiarBrilho = 240,
        double limiarBordas = 0.01)
    {
        using var bmp = new Bitmap(caminho);
        int brancos = 0, total = 0, bordas = 0;

        // Amostragem de 10 em 10 pixels. A 150 dpi isso da ~1,7 mm entre amostras,
        // enquanto um traco de caneta tem ~0,3 mm — escrita fina e esparsa pode
        // escapar. E o motivo de o criterio de bordas existir.
        for (int y = 0; y < bmp.Height; y += 10)
        {
            for (int x = 0; x < bmp.Width; x += 10)
            {
                var c = bmp.GetPixel(x, y);
                int brilho = (c.R + c.G + c.B) / 3;
                if (brilho > limiarBrilho) brancos++;

                if (x > 0 && y > 0)
                {
                    var anterior = bmp.GetPixel(x - 10, y - 10);
                    if (Math.Abs(brilho - ((anterior.R + anterior.G + anterior.B) / 3)) > 10) bordas++;
                }
                total++;
            }
        }

        if (total == 0) return false;

        double proporcaoBranca = (double)brancos / total;
        double proporcaoBordas = (double)bordas / total;
        bool branca = proporcaoBranca >= limiarBranco && proporcaoBordas < limiarBordas;

        // Registrar as MEDIDAS, e nao so a conclusao: quando um verso com conteudo
        // e descartado, a unica forma de saber se o detector errou por pouco ou se
        // a folha era mesmo vazia e ver o numero.
        Log.Info($"pagina-em-branco? {Path.GetFileName(caminho)}: branco={proporcaoBranca:F4} " +
                 $"(limite {limiarBranco:F2}) bordas={proporcaoBordas:F4} (limite {limiarBordas:F4}) " +
                 $"-> {(branca ? "DESCARTADA" : "mantida")}");

        return branca;
    }

    /// <summary>
    /// Reduz a imagem para caber no limite, preservando proporcao. Grava em
    /// temporario e troca, porque salvar no mesmo caminho com o Bitmap de origem
    /// aberto trava o arquivo.
    /// </summary>
    public static void Redimensionar(string caminho, int larguraMax = LarguraMaxima, int alturaMax = AlturaMaxima)
    {
        int novaLargura, novaAltura;

        using (var original = new Bitmap(caminho))
        {
            novaLargura = original.Width;
            novaAltura = original.Height;

            if (original.Width > larguraMax || original.Height > alturaMax)
            {
                float razao = Math.Min((float)larguraMax / original.Width,
                                       (float)alturaMax / original.Height);
                novaLargura = (int)(original.Width * razao);
                novaAltura = (int)(original.Height * razao);
            }

            using var redimensionada = new Bitmap(novaLargura, novaAltura);
            using (var g = Graphics.FromImage(redimensionada))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.DrawImage(original, 0, 0, novaLargura, novaAltura);
            }

            var temporario = caminho + ".tmp";
            redimensionada.Save(temporario, System.Drawing.Imaging.ImageFormat.Png);
            redimensionada.Dispose();
            original.Dispose();
            File.Delete(caminho);
            File.Move(temporario, caminho);
        }
    }

    /// <summary>
    /// Monta o PDF na ordem em que as paginas chegaram do alimentador. No duplex
    /// isso ja intercala frente e verso corretamente, porque e a ordem em que o
    /// scanner entrega.
    /// </summary>
    public static void MontarPdf(IReadOnlyList<string> imagens, string caminhoPdf)
    {
        var pdf = new PdfDocument();

        foreach (var imagem in imagens)
        {
            var pagina = pdf.AddPage();
            using var gfx = XGraphics.FromPdfPage(pagina);
            using var fluxo = File.OpenRead(imagem);
            var xImg = XImage.FromStream(() => fluxo);
            gfx.DrawImage(xImg, 0, 0, pagina.Width, pagina.Height);
        }

        if (pdf.PageCount == 0)
            throw new InvalidOperationException("Nenhuma página foi adicionada ao PDF.");

        pdf.Save(caminhoPdf);
    }

    /// <summary>Apaga arquivos sem deixar a falha derrubar a digitalizacao.</summary>
    public static void Apagar(IEnumerable<string> arquivos)
    {
        foreach (var arquivo in arquivos)
        {
            try { if (File.Exists(arquivo)) File.Delete(arquivo); }
            catch (Exception ex) { Log.Aviso($"nao consegui apagar {arquivo}: {ex.Message}"); }
        }
    }

    /// <summary>
    /// Carrega imagem sem manter o arquivo travado. Image.FromFile segura o
    /// arquivo enquanto a imagem viver, o que impediria o /upload de le-lo.
    /// </summary>
    public static Image CarregarSemTravar(string caminho)
    {
        using var fs = new FileStream(caminho, FileMode.Open, FileAccess.Read);
        using var temp = Image.FromStream(fs);
        return new Bitmap(temp);
    }
}
