using HelloWorldApi.Interface;
using HelloWorldApi.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace HelloWorldApi.Services
{
    public class ScannerService : IScannerService
    {
        private readonly string _outputDirectory;
        private readonly ILogger<ScannerService> _logger;
        private readonly IWebHostEnvironment _environment;

        public ScannerService(IWebHostEnvironment environment, ILogger<ScannerService> logger)
        {
            _environment = environment;
            _logger = logger;
            _outputDirectory = Path.Combine(_environment.ContentRootPath, "ScannedDocuments");
            
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        public async Task<List<ScannerSource>> GetAvailableSourcesAsync()
        {
            try
            {
                var sources = new List<ScannerSource>();
                
                // Em um ambiente real, você usaria TwainDotNet para listar as fontes
                // Por enquanto, retornamos uma lista simulada
                sources.Add(new ScannerSource
                {
                    Name = "Default Scanner",
                    ProductName = "Generic Scanner",
                    VendorName = "Generic Vendor",
                    IsDefault = true,
                    SupportsDocumentFeeder = true,
                    SupportsDuplex = false,
                    MaxResolution = 600,
                    SupportedFormats = new[] { "PDF" }
                });

                return await Task.FromResult(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter fontes de scanner");
                throw;
            }
        }

        public async Task<ScanResponse> ScanDocumentAsync(ScanRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando digitalização de documento");
                
                // Forçar formato PDF para evitar problemas de compatibilidade
                request.OutputFormat = "PDF";
                
                var fileName = string.IsNullOrEmpty(request.OutputFileName) 
                    ? $"scan_{DateTime.Now:yyyyMMdd_HHmmss}" 
                    : request.OutputFileName;
                
                var filePath = Path.Combine(_outputDirectory, $"{fileName}.pdf");
                
                // Criar um documento de exemplo (em produção, seria a imagem real do scanner)
                await CreateSamplePdfAsync(filePath, 1);

                var fileInfo = new FileInfo(filePath);
                
                var response = new ScanResponse
                {
                    Success = true,
                    Message = "Documento digitalizado com sucesso",
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    PageCount = 1,
                    OutputFormat = "PDF",
                    ScanDateTime = DateTime.Now
                };

                _logger.LogInformation("Digitalização concluída: {FilePath}", filePath);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a digitalização");
                return new ScanResponse
                {
                    Success = false,
                    Message = $"Erro durante a digitalização: {ex.Message}"
                };
            }
        }

        public async Task<ScanResponse> ScanMultiplePagesAsync(ScanRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando digitalização de múltiplas páginas");
                
                // Forçar formato PDF para evitar problemas de compatibilidade
                request.OutputFormat = "PDF";
                
                var fileName = string.IsNullOrEmpty(request.OutputFileName) 
                    ? $"multipage_scan_{DateTime.Now:yyyyMMdd_HHmmss}" 
                    : request.OutputFileName;
                
                var filePath = Path.Combine(_outputDirectory, $"{fileName}.pdf");
                
                // Simular digitalização de múltiplas páginas
                int pageCount = request.UseDocumentFeeder ? 3 : 2; // Simular 2-3 páginas
                
                await CreateSamplePdfAsync(filePath, pageCount);

                var fileInfo = new FileInfo(filePath);
                
                var response = new ScanResponse
                {
                    Success = true,
                    Message = $"Documento de {pageCount} páginas digitalizado com sucesso",
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    PageCount = pageCount,
                    OutputFormat = "PDF",
                    ScanDateTime = DateTime.Now
                };

                _logger.LogInformation("Digitalização de múltiplas páginas concluída: {FilePath}", filePath);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a digitalização de múltiplas páginas");
                return new ScanResponse
                {
                    Success = false,
                    Message = $"Erro durante a digitalização: {ex.Message}"
                };
            }
        }

        public async Task<bool> TestConnectionAsync(string sourceName)
        {
            try
            {
                // Simular teste de conexão
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão com scanner");
                return false;
            }
        }

        public async Task<byte[]> GetScannedDocumentAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Documento não encontrado", filePath);
                }

                return await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao ler documento digitalizado");
                throw;
            }
        }

        public async Task<bool> DeleteScannedDocumentAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Documento deletado: {FilePath}", filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar documento");
                return false;
            }
        }

        private async Task CreateSamplePdfAsync(string filePath, int pageCount)
        {
            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);
            
            for (int i = 1; i <= pageCount; i++)
            {
                document.Add(new Paragraph($"Página {i} - Documento Digitalizado")
                    .SetFontSize(20)
                    .SetBold());
                
                document.Add(new Paragraph($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                    .SetFontSize(12));
                
                document.Add(new Paragraph("Este é um documento de exemplo gerado pelo sistema de scanner.")
                    .SetFontSize(12));
                
                document.Add(new Paragraph($"Resolução: {300} DPI")
                    .SetFontSize(10)
                    .SetItalic());
                
                if (i < pageCount)
                {
                    document.Add(new AreaBreak());
                }
            }
            
            await Task.CompletedTask;
        }
    }
}
