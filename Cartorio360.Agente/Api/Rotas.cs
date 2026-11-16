using System.Text.Json;
using Cartorio360.Agente.Scanner;
using Cartorio360.Agente.Servicos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cartorio360.Agente.Api;

/// <summary>
/// Superficie HTTP do agente.
///
/// Os formatos de resposta NAO sao livres: o front ja existe e le campos
/// especificos. Em particular
///  - useEtiquetaImpressao.ts le `temZebra` no /status e
///    `e.data.message || e.data.error` no erro do /print;
///  - Scanner.vue e Documento.vue leem `erro.data.details`.
/// Por isso os erros carregam message, error e details ao mesmo tempo: e mais
/// barato repetir o texto do que arriscar um "undefined" na tela do cartorio.
/// </summary>
public static class Rotas
{
    private const string VERSAO = "2.0.0";
    private const string AGENTE = "360-serverLocal";  // nome mantido: o front compara com este

    public static void MapRotas(this WebApplication app)
    {
        var grupo = app.MapGroup("/apiservice");

        // -------------------------------------------------------------- status
        // Precisa responder 200 SEMPRE. O front chama isto em toda tela de
        // impressao para decidir entre etiqueta ZPL e HTML, com timeout de 1,5s,
        // e trata qualquer falha como "sem Zebra". Um 500 aqui viraria erro
        // visivel em toda estacao que imprime em impressora comum.
        grupo.MapGet("/status", (ZebraPrinter zebra) =>
        {
            try
            {
                var fila = zebra.Localizar();
                return Results.Json(new
                {
                    agente = AGENTE,
                    versao = VERSAO,
                    temZebra = fila != null,
                    impressora = fila?.Nome,
                    porta = fila?.Porta,
                    aviso = fila is { PortaSuspeita: true }
                        ? $"A fila '{fila.Nome}' está na porta {fila.Porta}. O job pode ser aceito sem sair papel."
                        : null
                });
            }
            catch (Exception ex)
            {
                Log.Erro("falha no /status", ex);
                return Results.Json(new { agente = AGENTE, versao = VERSAO, temZebra = false, message = ex.Message });
            }
        });

        // -------------------------------------------------------------- health
        grupo.MapGet("/health", (ZebraPrinter zebra, ScannerHost scanner, Configuracao cfg, UltimaDigitalizacao ultima) =>
        {
            var filas = zebra.ListarFilas();
            var digit = ultima.Obter();
            return Results.Json(new
            {
                agente = AGENTE,
                versao = VERSAO,
                porta = cfg.Porta,
                msManagementUrl = cfg.MsManagementUrl,
                pastaScans = cfg.PastaScansResolvida,
                pastaLogs = Log.PastaAtual,
                escaneando = scanner.Ocupado,
                ultimaDigitalizacao = digit == null ? null : new
                {
                    arquivo = digit.CaminhoArquivo,
                    formato = digit.Formato.ToString(),
                    paginas = digit.Paginas,
                    horario = digit.Horario
                },
                impressoras = filas.Select(f => new { f.Nome, f.Driver, f.Porta, portaSuspeita = f.PortaSuspeita })
            });
        });

        // --------------------------------------------------------- run-scanner
        grupo.MapGet("/run-scanner", async (string? format, ScannerHost scanner) =>
        {
            var formato = FormatoDigitalizacaoExtensoes.DoParametro(format);
            try
            {
                var (resultado, erro) = await scanner.DigitalizarAsync(formato);
                if (resultado == null)
                {
                    // Sobe o motivo real (sem papel, atolado, scanner ocupado...)
                    // em vez de um generico. E este texto que a tela do cartorio
                    // mostra, via erro.data.details.
                    var motivo = erro ?? "Digitalização cancelada ou nenhuma página escaneada.";
                    return Results.Json(new { message = motivo, details = motivo, success = false }, statusCode: 400);
                }

                return Results.Json(new
                {
                    message = "Scanner executado com sucesso",
                    success = true,
                    formato = formato.ToString().ToLowerInvariant(),
                    paginas = resultado.Paginas
                });
            }
            catch (Exception ex)
            {
                Log.Erro("falha no /run-scanner", ex);
                return Results.Json(new { message = ex.Message, details = ex.Message, success = false }, statusCode: 400);
            }
        });

        // --------------------------------------------------------------- upload
        // /upload e /uploadAnexo sao a MESMA coisa. O front chama as duas: as
        // telas de ficha usam /upload e tres telas de anexo chamam /uploadAnexo,
        // que nunca existiu no ObserverUpload e devolvia 404 silencioso.
        grupo.MapPost("/upload", (HttpRequest req, UploadService up) => Anexo(req, up));
        grupo.MapPost("/uploadAnexo", (HttpRequest req, UploadService up) => Anexo(req, up));

        grupo.MapPost("/uploadPessoa", async (HttpRequest req, UploadService up) =>
        {
            var pedido = await LerPedido(req);
            var r = await up.EnviarPessoaAsync(pedido);
            return Responder(r);
        });

        // ---------------------------------------------------------------- print
        grupo.MapPost("/print", async (HttpRequest req, ZebraPrinter zebra) =>
        {
            string? zpl = await LerZpl(req);

            if (string.IsNullOrWhiteSpace(zpl))
                return Results.Json(new { error = "ZPL inválido", message = "ZPL é obrigatório" }, statusCode: 400);

            if (!ZebraPrinter.ZplValido(zpl))
                return Results.Json(new
                {
                    error = "ZPL inválido",
                    message = "ZPL deve conter comandos válidos (^XA, ^XZ, etc.)"
                }, statusCode: 400);

            if (zebra.LocalizarCandidatas().Count == 0)
            {
                // Texto preservado do ObserverUpload: e exatamente esta string que
                // o useEtiquetaImpressao.ts mostra ao atendente.
                return Results.Json(new
                {
                    error = "Impressora não encontrada",
                    message = "Nenhuma impressora Zebra encontrada conectada via USB"
                }, statusCode: 404);
            }

            try
            {
                // Tenta as filas em ordem: estacao com fila duplicada ou morta
                // (o classico "(Copiar N)" em porta WSD) nao pode derrubar a
                // impressao se existe uma fila boa ao lado.
                var (quantidade, fila) = zebra.Imprimir(zpl);
                return Results.Json(new
                {
                    success = true,
                    message = $"ZPL enviado para impressão com sucesso ({quantidade} etiqueta(s))",
                    impressora = fila.Nome
                });
            }
            catch (Exception ex)
            {
                Log.Erro("falha no /print", ex);
                return Results.Json(new { error = "Erro na impressão", message = ex.Message }, statusCode: 500);
            }
        });

        // Compatibilidade: o ObserverUpload servia estas rotas na raiz, sem o
        // prefixo /apiservice. Mantidas para o front antigo continuar funcionando
        // durante a migracao — custam tres linhas e evitam uma janela de quebra.
        app.MapPost("/upload", (HttpRequest req, UploadService up) => Anexo(req, up));
        app.MapPost("/uploadAnexo", (HttpRequest req, UploadService up) => Anexo(req, up));
        app.MapPost("/uploadPessoa", async (HttpRequest req, UploadService up) => Responder(await up.EnviarPessoaAsync(await LerPedido(req))));
        app.MapGet("/status", (ZebraPrinter zebra) => Results.Json(new
        {
            agente = AGENTE,
            versao = VERSAO,
            temZebra = zebra.Localizar() != null
        }));
        app.MapPost("/print", async (HttpRequest req, ZebraPrinter zebra) =>
        {
            var zpl = await LerZpl(req);
            if (!ZebraPrinter.ZplValido(zpl))
                return Results.Json(new { error = "ZPL inválido", message = "ZPL é obrigatório" }, statusCode: 400);
            if (zebra.LocalizarCandidatas().Count == 0)
                return Results.Json(new { error = "Impressora não encontrada", message = "Nenhuma impressora Zebra encontrada conectada via USB" }, statusCode: 404);
            try
            {
                var (q, _) = zebra.Imprimir(zpl!);
                return Results.Json(new { success = true, message = $"ZPL enviado para impressão com sucesso ({q} etiqueta(s))" });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = "Erro na impressão", message = ex.Message }, statusCode: 500);
            }
        });

        app.MapGet("/", () => Results.Text("Agente 360 no ar. Diagnóstico em /apiservice/health"));
    }

    private static async Task<IResult> Anexo(HttpRequest req, UploadService up)
        => Responder(await up.EnviarAnexoAsync(await LerPedido(req)));

    private static IResult Responder(RespostaUpload r)
    {
        if (r.Sucesso)
            return Results.Json(new { message = "Upload realizado com sucesso!", data = r.Corpo });

        return Results.Json(new { message = r.Corpo, details = r.Corpo, success = false }, statusCode: r.StatusCode);
    }

    private static async Task<PedidoUpload> LerPedido(HttpRequest req)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(req.Body);
            var raiz = doc.RootElement;
            return new PedidoUpload(
                Texto(raiz, "tipo"),
                Texto(raiz, "token"),
                Texto(raiz, "cartorio_token"),
                Texto(raiz, "pessoa_token"));
        }
        catch
        {
            // Corpo ausente ou invalido nao e motivo para 500: o upload so precisa
            // mesmo do arquivo, e os campos vao vazios.
            return new PedidoUpload(null, null, null, null);
        }
    }

    private static string? Texto(JsonElement e, string nome)
        => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>
    /// Aceita corpo JSON { "zpl": "..." } ou o ZPL cru, como o ObserverUpload.
    /// </summary>
    private static async Task<string?> LerZpl(HttpRequest req)
    {
        using var leitor = new StreamReader(req.Body);
        var corpo = await leitor.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(corpo)) return null;

        var aparado = corpo.TrimStart();
        if (aparado.StartsWith("{"))
        {
            try
            {
                using var doc = JsonDocument.Parse(corpo);
                if (doc.RootElement.TryGetProperty("zpl", out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
            catch { /* nao era JSON valido; trata como ZPL cru */ }
        }
        return corpo;
    }
}
