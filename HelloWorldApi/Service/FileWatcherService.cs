using System.IO;
using Microsoft.Extensions.Hosting;

namespace HelloWorldApi.Services
{
    public class FileWatcherService : IHostedService, IDisposable
    {
        private readonly string _directoryPath;
        private FileSystemWatcher? _fileSystemWatcher;

        public FileWatcherService(string directoryPath)
        {
            _directoryPath = directoryPath;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _fileSystemWatcher = new FileSystemWatcher(_directoryPath);
            _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName;
            _fileSystemWatcher.Created += (sender, args) =>
            {
                Console.WriteLine($"Arquivo criado: {args.FullPath}");
            };
            _fileSystemWatcher.EnableRaisingEvents = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _fileSystemWatcher.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _fileSystemWatcher.Dispose();
        }
    }
}