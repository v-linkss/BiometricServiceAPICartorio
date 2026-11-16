using Cartorio360.Agente.Scanner;

namespace Cartorio360.Agente.Servicos;

/// <summary>
/// Guarda qual arquivo a ultima digitalizacao produziu, para o /upload saber o
/// que enviar.
///
/// Isto substitui o acoplamento por caminho fixo em disco que existia entre o
/// 360scan.exe e o ObserverUpload, e que causava o defeito mais traicoeiro do
/// sistema atual: o resolveUploadPath sempre tentava doc_ficha.png primeiro e so
/// caia para .pdf se o PNG nao existisse, sem olhar data. Uma digitalizacao em
/// modo imagem que ficasse sem subir deixava o PNG na pasta, e a proxima
/// digitalizacao em PDF subia o PNG velho.
///
/// Aqui nao ha caminho a adivinhar: ha o arquivo desta digitalizacao, ou nao ha
/// nada.
/// </summary>
public sealed class UltimaDigitalizacao
{
    private readonly object _trava = new();
    private ResultadoDigitalizacao? _atual;

    public void Registrar(ResultadoDigitalizacao resultado)
    {
        lock (_trava) _atual = resultado;
    }

    /// <summary>
    /// Devolve a ultima digitalizacao cujo arquivo ainda existe. Se o arquivo
    /// sumiu (o /upload apaga depois de enviar), devolve null — que e o que
    /// produz o 404 "Arquivo não encontrado" esperado pelo front.
    /// </summary>
    public ResultadoDigitalizacao? Obter()
    {
        lock (_trava)
        {
            if (_atual == null) return null;
            if (!_atual.Existe) { _atual = null; return null; }
            return _atual;
        }
    }

    public void Limpar()
    {
        lock (_trava) _atual = null;
    }
}
