using System.Reflection;

namespace Cartorio360.Agente.Infra;

/// <summary>
/// Icone da aplicacao, embutido como recurso.
///
/// Embutido e nao lido de disco porque o publish e single-file: um .ico ao lado
/// do executavel seria mais um arquivo para o instalador copiar e para alguem
/// apagar sem querer.
/// </summary>
public static class Recursos
{
    private const string NOME = "cartorio_icon.ico";

    /// <summary>
    /// Icone no tamanho pedido. `new Icon(stream, tamanho)` escolhe a resolucao
    /// mais proxima dentro do .ico multi-resolucao, em vez de redimensionar uma
    /// so — e a diferenca entre um icone nitido e um borrado na bandeja.
    /// </summary>
    public static Icon Carregar(Size tamanho)
    {
        try
        {
            using var fluxo = Assembly.GetExecutingAssembly().GetManifestResourceStream(NOME);
            if (fluxo != null) return new Icon(fluxo, tamanho);
        }
        catch (Exception ex)
        {
            Log.Aviso($"nao foi possivel carregar o icone embutido: {ex.Message}");
        }
        // Icone e cosmetico: nunca vale derrubar o agente por causa dele.
        return (Icon)SystemIcons.Application.Clone();
    }

    public static Icon CarregarPequeno() => Carregar(SystemInformation.SmallIconSize);

    public static Icon CarregarGrande() => Carregar(new Size(32, 32));
}
