using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace HelloWorldApi.Services
{
    public class DocumentCleanupService : BackgroundService
    {
        private readonly ILogger<DocumentCleanupService> _logger;
        private readonly string _documentsDirectory;
        private readonly TimeSpan _cleanupInterval;
        private readonly int _maxFileAgeHours;

        public DocumentCleanupService(ILogger<DocumentCleanupService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _documentsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ScannedDocuments");
            _cleanupInterval = TimeSpan.FromHours(24); // Limpeza diária
            _maxFileAgeHours = 24; // Manter arquivos por 24 horas
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de limpeza de documentos iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldDocumentsAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante a limpeza automática de documentos");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Tentar novamente em 1 hora
                }
            }

            _logger.LogInformation("Serviço de limpeza de documentos parado");
        }

        private async Task CleanupOldDocumentsAsync()
        {
            if (!Directory.Exists(_documentsDirectory))
            {
                return;
            }

            var cutoffTime = DateTime.Now.AddHours(-_maxFileAgeHours);
            var deletedCount = 0;

            try
            {
                var files = Directory.GetFiles(_documentsDirectory, "*.*", SearchOption.TopDirectoryOnly);
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffTime)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                            _logger.LogDebug("Arquivo antigo deletado: {FileName}", fileInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Não foi possível deletar arquivo: {FileName}", fileInfo.Name);
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Limpeza automática concluída: {DeletedCount} arquivos removidos", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a limpeza automática");
            }

            await Task.CompletedTask;
        }
    }
}
