using Cartorio360.Agente.Servicos;
using Xunit;

namespace Cartorio360.Agente.Testes;

/// <summary>
/// Escolha de fila numa estacao com filas duplicadas.
///
/// Caso real que originou estes testes: um cartorio tinha
///   "ZDesigner ZD220-203dpi ZPL (Copiar 4)"  em porta WSD-43073d0e-...
///   "ZDesigner ZD220-203dpi ZPL"             em porta USB001
/// O agente pegava a primeira e a impressao morria com ERROR_PRINTER_DELETED
/// (Win32 1905), porque o aparelho por tras da fila WSD ja tinha sido removido.
/// </summary>
public class FilaImpressaoTestes
{
    [Theory]
    [InlineData("LPT1:")]
    [InlineData("COM3:")]
    [InlineData("WSD-43073d0e-4d04-4690-a483-4a8")]
    public void Portas_que_costumam_aceitar_o_job_sem_imprimir_sao_suspeitas(string porta)
        => Assert.True(new FilaImpressao("ZDesigner", "ZDesigner", porta).PortaSuspeita);

    [Theory]
    [InlineData("USB001")]
    [InlineData("USB002")]
    [InlineData("PORTPROMPT:")]
    public void Porta_usb_nao_e_suspeita(string porta)
        => Assert.False(new FilaImpressao("ZDesigner", "ZDesigner", porta).PortaSuspeita);
}
