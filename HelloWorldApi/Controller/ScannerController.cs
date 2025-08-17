using Microsoft.AspNetCore.Mvc;
using HelloWorldApi.Interface;
using HelloWorldApi.Models;

namespace HelloWorldApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScannerController : ControllerBase
    {
        private readonly IScannerService _scannerService;
        private readonly ILogger<ScannerController> _logger;

        public ScannerController(IScannerService scannerService, ILogger<ScannerController> logger)
        {
            _scannerService = scannerService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém todas as fontes de scanner disponíveis
        /// </summary>
        [HttpGet("sources")]
        public async Task<ActionResult<List<ScannerSource>>> GetAvailableSources()
        {
            try
            {
                var sources = await _scannerService.GetAvailableSourcesAsync();
                return Ok(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter fontes de scanner");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Testa a conexão com uma fonte de scanner específica
        /// </summary>
        [HttpGet("test-connection/{sourceName}")]
        public async Task<ActionResult<bool>> TestConnection(string sourceName)
        {
            try
            {
                var isConnected = await _scannerService.TestConnectionAsync(sourceName);
                return Ok(new { sourceName, isConnected });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão com scanner");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Digitaliza um documento de uma página
        /// </summary>
        [HttpPost("scan")]
        public async Task<ActionResult<ScanResponse>> ScanDocument([FromBody] ScanRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _scannerService.ScanDocumentAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a digitalização");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Digitaliza um documento de múltiplas páginas
        /// </summary>
        [HttpPost("scan-multipage")]
        public async Task<ActionResult<ScanResponse>> ScanMultiplePages([FromBody] ScanRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _scannerService.ScanMultiplePagesAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a digitalização de múltiplas páginas");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém um documento digitalizado pelo caminho do arquivo
        /// </summary>
        [HttpGet("document/{*filePath}")]
        public async Task<IActionResult> GetDocument(string filePath)
        {
            try
            {
                var documentBytes = await _scannerService.GetScannedDocumentAsync(filePath);
                
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);
                
                return File(documentBytes, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "Documento não encontrado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter documento");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Deleta um documento digitalizado
        /// </summary>
        [HttpDelete("document/{*filePath}")]
        public async Task<ActionResult<bool>> DeleteDocument(string filePath)
        {
            try
            {
                var deleted = await _scannerService.DeleteScannedDocumentAsync(filePath);
                
                if (deleted)
                {
                    return Ok(new { message = "Documento deletado com sucesso" });
                }
                else
                {
                    return NotFound(new { error = "Documento não encontrado" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar documento");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };
        }
    }
}
