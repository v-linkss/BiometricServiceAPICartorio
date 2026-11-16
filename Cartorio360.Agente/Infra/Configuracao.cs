using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cartorio360.Agente.Infra;

/// <summary>
/// config.json, ao lado do executavel em C:\cartorio360\Agente360\.
///
/// O motivo de a URL do backend ser configuracao, e nao constante: hoje o
/// ObserverUpload tem duas URLs fixas no codigo apontando para ambientes
/// DIFERENTES — /upload vai para dev.cartorio360.com.br e /uploadPessoa vai para
/// www.cartorio360.com.br. Ou seja, anexo de producao sobe para o servidor de
/// desenvolvimento. Uma base unica configurada no instalador elimina a classe
/// inteira desse erro.
/// </summary>
public sealed class Configuracao
{
    [JsonPropertyName("porta")]
    public int Porta { get; set; } = 5150;

    /// <summary>Base do MS_MANAGEMENT, sem barra no fim. Ex.: https://www.cartorio360.com.br:14193</summary>
    [JsonPropertyName("msManagementUrl")]
    public string MsManagementUrl { get; set; } = "https://www.cartorio360.com.br:14193";

    /// <summary>Vazio = subpasta "scans" ao lado do executavel.</summary>
    [JsonPropertyName("pastaScans")]
    public string PastaScans { get; set; } = "";

    /// <summary>
    /// Fila da Zebra a usar. Vazio = detecta pelo nome/driver. Serve para a
    /// estacao que tem mais de uma etiquetadora instalada.
    /// </summary>
    [JsonPropertyName("impressoraZebra")]
    public string ImpressoraZebra { get; set; } = "";

    [JsonIgnore]
    public string CaminhoArquivo { get; private set; } = "";

    [JsonIgnore]
    public string PastaScansResolvida =>
        string.IsNullOrWhiteSpace(PastaScans)
            ? Path.Combine(AppContext.BaseDirectory, "scans")
            : PastaScans;

    private static readonly JsonSerializerOptions _opcoes = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static Configuracao Carregar()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "config.json");

        // Ausencia de config.json e caso normal, nao erro: primeira execucao antes
        // de o instalador escrever, ou execucao a partir do bin durante o
        // desenvolvimento. Os padroes precisam ser utilizaveis sozinhos.
        if (!File.Exists(caminho))
        {
            var padrao = new Configuracao { CaminhoArquivo = caminho };
            try
            {
                File.WriteAllText(caminho, JsonSerializer.Serialize(padrao, _opcoes));
                Log.Info($"config.json nao existia; criado com padroes em {caminho}");
            }
            catch (Exception ex)
            {
                // Pasta sem permissao de escrita. Segue com os padroes em memoria
                // em vez de nao subir — mas registra, porque e o sintoma do
                // icacls faltando na instalacao.
                Log.Aviso($"nao foi possivel criar config.json ({ex.Message}); usando padroes em memoria");
            }
            return padrao;
        }

        try
        {
            var json = File.ReadAllText(caminho);
            var cfg = JsonSerializer.Deserialize<Configuracao>(json, _opcoes) ?? new Configuracao();
            cfg.CaminhoArquivo = caminho;
            cfg.MsManagementUrl = cfg.MsManagementUrl.TrimEnd('/');
            return cfg;
        }
        catch (Exception ex)
        {
            // config.json corrompido nao pode impedir o agente de subir: sem ele a
            // estacao perde scanner e impressao. Padroes valem mais que parada.
            Log.Erro("config.json invalido; usando padroes", ex);
            return new Configuracao { CaminhoArquivo = caminho };
        }
    }
}
