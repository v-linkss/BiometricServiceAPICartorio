using HelloWorldApi.Interface;
using HelloWorldApi.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Drawing;
using System.Drawing.Imaging;
using TwainDotNet;
using TwainDotNet.TwainNative;

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
                    SupportedFormats = new[] { "PDF", "JPEG", "PNG", "TIFF" }
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
                
                // Simular digitalização (em produção, usar TwainDotNet)
                var fileName = string.IsNullOrEmpty(request.OutputFileName) 
                    ? $"scan_{DateTime.Now:yyyyMMdd_HHmmss}" 
                    : request.OutputFileName;
                
                var filePath = Path.Combine(_outputDirectory, $"{fileName}.{request.OutputFormat.ToLower()}");
                
                // Criar um documento de exemplo (em produção, seria a imagem real do scanner)
                if (request.OutputFormat.ToUpper() == "PDF")
                {
                    await CreateSamplePdfAsync(filePath, 1);
                }
                else
                {
                    await CreateSampleImageAsync(filePath, request.OutputFormat);
                }

                var fileInfo = new FileInfo(filePath);
                
                var response = new ScanResponse
                {
                    Success = true,
                    Message = "Documento digitalizado com sucesso",
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    PageCount = 1,
                    OutputFormat = request.OutputFormat,
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
                
                var fileName = string.IsNullOrEmpty(request.OutputFileName) 
                    ? $"multipage_scan_{DateTime.Now:yyyyMMdd_HHmmss}" 
                    : request.OutputFileName;
                
                var filePath = Path.Combine(_outputDirectory, $"{fileName}.{request.OutputFormat.ToLower()}");
                
                // Simular digitalização de múltiplas páginas
                int pageCount = request.UseDocumentFeeder ? 3 : 2; // Simular 2-3 páginas
                
                if (request.OutputFormat.ToUpper() == "PDF")
                {
                    await CreateSamplePdfAsync(filePath, pageCount);
                }
                else
                {
                    await CreateSampleMultiPageImageAsync(filePath, request.OutputFormat, pageCount);
                }

                var fileInfo = new FileInfo(filePath);
                
                var response = new ScanResponse
                {
                    Success = true,
                    Message = $"Documento de {pageCount} páginas digitalizado com sucesso",
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    PageCount = pageCount,
                    OutputFormat = request.OutputFormat,
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
            using var document = new Document(PageSize.A4);
            using var writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
            
            document.Open();
            
            for (int i = 1; i <= pageCount; i++)
            {
                document.Add(new Paragraph($"Página {i} - Documento Digitalizado"));
                document.Add(new Paragraph($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"));
                document.Add(new Paragraph("Este é um documento de exemplo gerado pelo sistema de scanner."));
                
                if (i < pageCount)
                {
                    document.NewPage();
                }
            }
            
            document.Close();
            await Task.CompletedTask;
        }

        private async Task CreateSampleImageAsync(string filePath, string format)
        {
            using var bitmap = new Bitmap(800, 600);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.Clear(Color.White);
            graphics.DrawString("Documento Digitalizado", new System.Drawing.Font("Arial", 24), Brushes.Black, 50, 50);
            graphics.DrawString($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", new System.Drawing.Font("Arial", 12), Brushes.Black, 50, 100);
            graphics.DrawString("Este é um documento de exemplo", new System.Drawing.Font("Arial", 12), Brushes.Black, 50, 150);

            ImageFormat imageFormat = format.ToUpper() switch
            {
                "JPEG" => ImageFormat.Jpeg,
                "PNG" => ImageFormat.Png,
                "TIFF" => ImageFormat.Tiff,
                _ => ImageFormat.Png
            };

            bitmap.Save(filePath, imageFormat);
            await Task.CompletedTask;
        }

        private async Task CreateSampleMultiPageImageAsync(string filePath, string format, int pageCount)
        {
            if (format.ToUpper() == "TIFF")
            {
                await CreateMultiPageTiffAsync(filePath, pageCount);
            }
            else
            {
                // Para outros formatos, criar apenas uma imagem
                await CreateSampleImageAsync(filePath, format);
            }
        }

        private Task CreateMultiPageTiffAsync(string filePath, int pageCount)
        {
            // Implementar criação de TIFF multi-página se necessário
            return CreateSampleImageAsync(filePath, "PNG");
        }
    }
}
