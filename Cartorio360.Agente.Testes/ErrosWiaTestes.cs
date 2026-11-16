using Cartorio360.Agente.Scanner;
using Xunit;

namespace Cartorio360.Agente.Testes;

public class ErrosWiaTestes
{
    [Theory]
    [InlineData(0x80210002u, "atolado")]
    [InlineData(0x80210003u, "Não há papel")]
    [InlineData(0x80210005u, "desligado ou desconectado")]
    [InlineData(0x8021000Au, "cabo USB")]
    public void Codigos_conhecidos_viram_instrucao_acionavel(uint codigo, string trecho)
        => Assert.Contains(trecho, ErrosWia.Mensagem(codigo));

    [Fact]
    public void Codigo_desconhecido_ainda_diz_qual_foi()
    {
        var msg = ErrosWia.Mensagem(0x80211234);
        Assert.Contains("0x80211234", msg);
    }

    [Fact]
    public void Falha_inesperada_com_duplex_aponta_o_duplex()
    {
        var comDuplex = ErrosWia.Mensagem(ErrosWia.FalhaInesperada, duplexSolicitado: true);
        Assert.Contains("frente e verso", comDuplex);

        var semDuplex = ErrosWia.Mensagem(ErrosWia.FalhaInesperada, duplexSolicitado: false);
        Assert.DoesNotContain("frente e verso", semDuplex);
    }

    // A distincao que o WiaMultiPageScan nao fazia: o mesmo codigo de "alimentador
    // vazio" significa fim normal DEPOIS de puxar pagina, e "esqueceram o papel"
    // ANTES. Tratar os dois como fim normal transformava "sem papel" em sucesso
    // silencioso com zero paginas.
    [Fact]
    public void Alimentador_vazio_depois_de_paginas_e_fim_normal()
        => Assert.True(ErrosWia.EhFimNormal(ErrosWia.PapelAusente, paginasJaCapturadas: 3));

    [Fact]
    public void Alimentador_vazio_sem_nenhuma_pagina_e_erro()
        => Assert.False(ErrosWia.EhFimNormal(ErrosWia.PapelAusente, paginasJaCapturadas: 0));

    [Fact]
    public void Falha_inesperada_sem_nenhuma_pagina_e_erro()
        => Assert.False(ErrosWia.EhFimNormal(ErrosWia.FalhaInesperada, paginasJaCapturadas: 0));

    [Fact]
    public void Outros_codigos_nunca_sao_fim_normal()
        => Assert.False(ErrosWia.EhFimNormal(0x80210002, paginasJaCapturadas: 5));
}
