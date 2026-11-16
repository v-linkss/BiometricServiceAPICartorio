using Cartorio360.Agente.Servicos;
using Xunit;

namespace Cartorio360.Agente.Testes;

/// <summary>
/// Fatiamento e validacao de ZPL — a parte da impressao que da para testar sem
/// etiquetadora. O envio em si e P/Invoke no spooler e exige hardware.
/// </summary>
public class ZebraPrinterTestes
{
    private const string UmaEtiqueta = "^XA^CF0,40^FO40,40^FDSELO 001^FS^XZ";

    [Fact]
    public void Uma_etiqueta_vira_um_job()
        => Assert.Single(ZebraPrinter.FatiarEtiquetas(UmaEtiqueta));

    // O caso que importa de verdade: o front manda um LOTE de selos num corpo so,
    // e cada etiqueta precisa virar um job separado. Se isto quebrar, o lote sai
    // como uma etiqueta gigante — e so se descobre com papel na mao.
    [Fact]
    public void Lote_vira_um_job_por_etiqueta()
    {
        var lote = "^XA^FDSELO 1^FS^XZ\n^XA^FDSELO 2^FS^XZ\n^XA^FDSELO 3^FS^XZ";
        var etiquetas = ZebraPrinter.FatiarEtiquetas(lote);

        Assert.Equal(3, etiquetas.Count);
        Assert.Contains("SELO 1", etiquetas[0]);
        Assert.Contains("SELO 2", etiquetas[1]);
        Assert.Contains("SELO 3", etiquetas[2]);
    }

    [Fact]
    public void Cada_etiqueta_fatiada_e_um_bloco_completo()
    {
        var lote = "^XA^FDA^FS^XZ^XA^FDB^FS^XZ";
        foreach (var e in ZebraPrinter.FatiarEtiquetas(lote))
        {
            Assert.StartsWith("^XA", e);
            Assert.EndsWith("^XZ", e);
        }
    }

    [Fact]
    public void Etiquetas_com_quebras_de_linha_dentro_sao_preservadas()
    {
        var lote = "^XA\n^CF0,40\n^FO40,40^FDSELO^FS\n^XZ\n^XA\n^FDOUTRO^FS\n^XZ";
        var etiquetas = ZebraPrinter.FatiarEtiquetas(lote);

        Assert.Equal(2, etiquetas.Count);
        Assert.Contains("\n", etiquetas[0]);
    }

    [Fact]
    public void Lixo_entre_etiquetas_e_ignorado()
    {
        var lote = "cabecalho qualquer ^XA^FDA^FS^XZ rodape ^XA^FDB^FS^XZ fim";
        Assert.Equal(2, ZebraPrinter.FatiarEtiquetas(lote).Count);
    }

    [Fact]
    public void Texto_sem_etiqueta_nenhuma_devolve_lista_vazia()
        => Assert.Empty(ZebraPrinter.FatiarEtiquetas("isto nao e zpl"));

    // -------------------------------------------------------------- validacao

    [Theory]
    [InlineData("^XA^FDteste^FS^XZ")]
    [InlineData("^XA")]
    [InlineData("^FO10,10")]
    public void Zpl_plausivel_e_aceito(string zpl)
        => Assert.True(ZebraPrinter.ZplValido(zpl));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("nao eh zpl")]
    public void Zpl_invalido_e_recusado(string? zpl)
        => Assert.False(ZebraPrinter.ZplValido(zpl));

    // -------------------------------------------------------------- porta

    // A armadilha achada na bancada: fila apontando para LPT1 aceita o job e nao
    // sai papel. Falha silenciosa, a pior de diagnosticar.
    [Theory]
    [InlineData("LPT1:", true)]
    [InlineData("COM3:", true)]
    [InlineData("USB001", false)]
    [InlineData("PORTPROMPT:", false)]
    public void Porta_serial_ou_paralela_e_marcada_como_suspeita(string porta, bool suspeita)
        => Assert.Equal(suspeita, new FilaImpressao("Zebra", "ZDesigner", porta).PortaSuspeita);
}
