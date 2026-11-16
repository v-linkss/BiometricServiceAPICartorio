using System.Net.Http.Headers;
using Cartorio360.Agente.Scanner;

namespace Cartorio360.Agente.Servicos;

public sealed record PedidoUpload(string? Tipo, string? Token, string? CartorioToken, string? PessoaToken);

public sealed record RespostaUpload(bool Sucesso, int StatusCode, string Corpo);

/// <summary>
/// Envia a digitalizacao ao backend, reproduzindo os multipart que o
/// ObserverUpload montava (api.service.js).
///
/// Diferenca deliberada: a base da URL vem da configuracao. No original havia
/// duas constantes apontando para ambientes diferentes — handleFileUploadAnexos
/// para dev.cartorio360.com.br e handleFileUpload para www.cartorio360.com.br —
/// o que fazia anexo de producao subir para o servidor de desenvolvimento.
/// </summary>
public sealed class UploadService
{
    private readonly Configuracao _config;
    private readonly UltimaDigitalizacao _ultima;
    private readonly HttpClient _http;

    public UploadService(Configuracao config, UltimaDigitalizacao ultima, HttpClient http)
    {
        _config = config;
        _ultima = ultima;
        _http = http;
    }

    /// <summary>Rotas /upload e /uploadAnexo — anexo de ato.</summary>
    public Task<RespostaUpload> EnviarAnexoAsync(PedidoUpload pedido)
        => EnviarAsync("/upload", pedido, incluirPessoaToken: false);

    /// <summary>Rota /uploadPessoa — documento de pessoa.</summary>
    public Task<RespostaUpload> EnviarPessoaAsync(PedidoUpload pedido)
        => EnviarAsync("/uploadPessoa", pedido, incluirPessoaToken: true);

    private async Task<RespostaUpload> EnviarAsync(string rota, PedidoUpload pedido, bool incluirPessoaToken)
    {
        var digitalizacao = _ultima.Obter();
        if (digitalizacao == null)
            return new RespostaUpload(false, 404, "Arquivo não encontrado. Faça a digitalização antes de enviar.");

        var url = _config.MsManagementUrl.TrimEnd('/') + rota;
        Log.Info($"upload {rota}: {digitalizacao.CaminhoArquivo} -> {url}");

        try
        {
            using var conteudo = new MultipartFormDataContent();
            var bytes = await File.ReadAllBytesAsync(digitalizacao.CaminhoArquivo);
            var arquivo = new ByteArrayContent(bytes);
            arquivo.Headers.ContentType = new MediaTypeHeaderValue(
                digitalizacao.Formato == FormatoDigitalizacao.Pdf ? "application/pdf" : "image/png");

            // Nome do campo e do arquivo iguais aos do ObserverUpload: o backend
            // depende do nome "file" e reconhece doc_ficha.png / doc_ficha.pdf.
            conteudo.Add(arquivo, "file", Path.GetFileName(digitalizacao.CaminhoArquivo));

            if (!string.IsNullOrEmpty(pedido.Tipo)) conteudo.Add(new StringContent(pedido.Tipo), "tipo");

            if (incluirPessoaToken)
            {
                if (!string.IsNullOrEmpty(pedido.PessoaToken))
                    conteudo.Add(new StringContent(pedido.PessoaToken), "pessoa_token");
            }
            else
            {
                if (!string.IsNullOrEmpty(pedido.Token))
                    conteudo.Add(new StringContent(pedido.Token), "token");
                if (!string.IsNullOrEmpty(pedido.CartorioToken))
                    conteudo.Add(new StringContent(pedido.CartorioToken), "cartorio_token");
            }

            using var resposta = await _http.PostAsync(url, conteudo);
            var corpo = await resposta.Content.ReadAsStringAsync();

            if (resposta.IsSuccessStatusCode)
            {
                Log.Info($"upload concluido ({(int)resposta.StatusCode})");
                ApagarTemporario(digitalizacao);
                return new RespostaUpload(true, (int)resposta.StatusCode, corpo);
            }

            // Falha do backend NAO apaga o arquivo: o ObserverUpload apagava no
            // finally mesmo quando o upload falhava, e a ficha escaneada se perdia.
            // Mantendo o arquivo, o atendente pode repetir o envio sem reescanear.
            Log.Erro($"upload recusado pelo backend ({(int)resposta.StatusCode}): {corpo}");
            return new RespostaUpload(false, (int)resposta.StatusCode, corpo);
        }
        catch (Exception ex)
        {
            Log.Erro("falha no upload", ex);
            return new RespostaUpload(false, 502, $"Falha ao falar com o servidor: {ex.Message}");
        }
    }

    private void ApagarTemporario(ResultadoDigitalizacao digitalizacao)
    {
        try
        {
            if (File.Exists(digitalizacao.CaminhoArquivo))
                File.Delete(digitalizacao.CaminhoArquivo);

            var pasta = Path.GetDirectoryName(digitalizacao.CaminhoArquivo);
            if (pasta != null && Directory.Exists(pasta) && !Directory.EnumerateFileSystemEntries(pasta).Any())
                Directory.Delete(pasta);

            _ultima.Limpar();
        }
        catch (Exception ex)
        {
            Log.Aviso($"nao consegui limpar a digitalizacao enviada: {ex.Message}");
        }
    }
}
