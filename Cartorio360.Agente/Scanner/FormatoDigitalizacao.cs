namespace Cartorio360.Agente.Scanner;

public enum FormatoDigitalizacao
{
    Imagem,
    Pdf
}

public static class FormatoDigitalizacaoExtensoes
{
    /// <summary>
    /// Traduz o ?format= da rota /run-scanner.
    ///
    /// Regra copiada do WiaMultiPageScan: "pdf" (case-insensitive) e PDF,
    /// QUALQUER outra coisa — inclusive ausencia — e imagem. Isso nao e detalhe:
    /// tres telas do front (Procuracao/Anexos, Procuracao/atualizar/Anexos e
    /// Documentos) chamam /run-scanner SEM o parametro e dependem do padrao.
    /// </summary>
    public static FormatoDigitalizacao DoParametro(string? format)
        => string.Equals(format?.Trim(), "pdf", StringComparison.OrdinalIgnoreCase)
            ? FormatoDigitalizacao.Pdf
            : FormatoDigitalizacao.Imagem;

    public static string Extensao(this FormatoDigitalizacao f)
        => f == FormatoDigitalizacao.Pdf ? ".pdf" : ".png";

    public static string NomeArquivo(this FormatoDigitalizacao f)
        => f == FormatoDigitalizacao.Pdf ? "doc_ficha.pdf" : "doc_ficha.png";
}
