using System.IO;

namespace MyApplication.Interfaces
{
    public interface IFileWatcherService
    {
        void SetDirectoryPath(string directoryPath);
    }
}