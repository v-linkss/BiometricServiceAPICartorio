using Cartorio360.Agente.Scanner;
using Xunit;

namespace Cartorio360.Agente.Testes;

/// <summary>
/// O ?format= da rota /run-scanner.
///
/// Regra que NAO pode mudar: tres telas do front (Procuracao/Anexos,
/// Procuracao/atualizar/Anexos e Documentos) chamam /run-scanner SEM o parametro e
/// dependem do padrao ser imagem. Alterar isso faria essas telas gerarem PDF onde
/// o backend espera PNG.
/// </summary>
public class FormatoDigitalizacaoTestes
{
    [Theory]
    [InlineData("pdf")]
    [InlineData("PDF")]
    [InlineData("Pdf")]
    [InlineData(" pdf ")]
    public void Pdf_em_qualquer_caixa_vira_Pdf(string entrada)
        => Assert.Equal(FormatoDigitalizacao.Pdf, FormatoDigitalizacaoExtensoes.DoParametro(entrada));

    [Theory]
    [InlineData("img")]
    [InlineData("IMG")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("qualquer-coisa")]
    [InlineData(null)]
    public void Qualquer_outro_valor_vira_Imagem(string? entrada)
        => Assert.Equal(FormatoDigitalizacao.Imagem, FormatoDigitalizacaoExtensoes.DoParametro(entrada));

    [Fact]
    public void Ausencia_do_parametro_vira_Imagem()
        => Assert.Equal(FormatoDigitalizacao.Imagem, FormatoDigitalizacaoExtensoes.DoParametro(null));

    [Fact]
    public void Nome_do_arquivo_segue_o_formato()
    {
        Assert.Equal("doc_ficha.pdf", FormatoDigitalizacao.Pdf.NomeArquivo());
        Assert.Equal("doc_ficha.png", FormatoDigitalizacao.Imagem.NomeArquivo());
    }

    [Fact]
    public void Extensao_segue_o_formato()
    {
        Assert.Equal(".pdf", FormatoDigitalizacao.Pdf.Extensao());
        Assert.Equal(".png", FormatoDigitalizacao.Imagem.Extensao());
    }
}
