namespace Cartorio360.Agente.Scanner;

/// <summary>
/// O que o /upload precisa saber depois que o /run-scanner terminou.
///
/// Este objeto e o que substitui o caminho fixo em disco
/// (C:\cartorio360\ScannerApp\Imagens\doc_ficha.png) que o ObserverUpload lia.
/// Com ele o defeito do "PNG velho sequestra o upload de um PDF novo" deixa de
/// existir por construcao: nao ha caminho a adivinhar, ha o arquivo desta
/// digitalizacao.
/// </summary>
public sealed record ResultadoDigitalizacao(
    string CaminhoArquivo,
    FormatoDigitalizacao Formato,
    int Paginas,
    DateTime Horario)
{
    public bool Existe => File.Exists(CaminhoArquivo);
}
