namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Traduz o HRESULT do WIA para algo acionavel pelo atendente.
///
/// O WiaMultiPageScan original tratava UM codigo (0x80210005) e deixava os demais
/// passarem em silencio: o atendente clicava em "Escanear", nada acontecia, e nao
/// havia o que olhar. Papel ausente e atolamento sao os erros do dia a dia no
/// balcao, e eram justamente os que nao tinham mensagem.
///
/// Separado em classe propria por ser tabela pura — da para testar sem scanner.
/// </summary>
public static class ErrosWia
{
    public const uint PapelAusente = 0x80210003;
    public const uint FalhaInesperada = 0x8000FFFF;

    /// <summary>
    /// Mensagem para o atendente. <paramref name="duplexSolicitado"/> muda o texto
    /// de 0x8000FFFF: com duplex ligado, o culpado provavel e o driver ter aceitado
    /// o modo frente-e-verso sem conseguir executa-lo — e dizer isso poupa o
    /// atendente de concluir que o scanner quebrou.
    /// </summary>
    public static string Mensagem(uint codigo, bool duplexSolicitado = false) => codigo switch
    {
        0x80210001 => "Erro geral no scanner. Desligue e ligue o equipamento e tente novamente.",
        0x80210002 => "Papel atolado, ou a bandeja do alimentador está aberta. Verifique o equipamento e tente novamente.",
        0x80210003 => "Não há papel no alimentador. Coloque o documento e tente novamente.",
        0x80210004 => "Problema com o papel no alimentador. Reposicione o documento e tente novamente.",
        0x80210005 => "O scanner está desligado ou desconectado.",
        0x80210006 => "O scanner está ocupado com outra tarefa. Aguarde e tente novamente.",
        0x80210007 => "O scanner está aquecendo. Aguarde alguns segundos e tente novamente.",
        0x80210008 => "O scanner precisa de atenção. Verifique o painel do equipamento.",
        0x8021000A => "Falha de comunicação com o scanner. Verifique o cabo USB.",
        0x8021000C => "Configuração incompatível com este scanner.",
        0x8021000D => "O scanner está sendo usado por outro programa.",
        0x8021000E => "Falha no driver do scanner. Reinstalar o driver costuma resolver.",

        0x8000FFFF when duplexSolicitado =>
            "O scanner não conseguiu digitalizar em frente e verso. " +
            "Desmarque \"Scan frente e verso (Duplex)\" e tente novamente.",
        0x8000FFFF => "Falha inesperada no scanner. Desligue e ligue o equipamento e tente novamente.",

        _ => $"Falha na digitalização (código 0x{codigo:X8})."
    };

    /// <summary>
    /// Fim normal do documento: o alimentador esvaziou DEPOIS de ja ter puxado
    /// pagina. Nao e erro — e como o ADF diz "acabou".
    ///
    /// A distincao importa: o mesmo 0x80210003 na PRIMEIRA pagina significa
    /// "esqueceram de por o papel". O original tratava os dois como fim normal, e
    /// entao "sem papel" virava sucesso silencioso com zero paginas.
    /// </summary>
    public static bool EhFimNormal(uint codigo, int paginasJaCapturadas)
        => paginasJaCapturadas > 0 && codigo is PapelAusente or FalhaInesperada;
}
