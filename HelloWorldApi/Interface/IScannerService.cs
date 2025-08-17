using HelloWorldApi.Models;

namespace HelloWorldApi.Interface
{
    public interface IScannerService
    {
        Task<List<ScannerSource>> GetAvailableSourcesAsync();
        Task<ScanResponse> ScanDocumentAsync(ScanRequest request);
        Task<ScanResponse> ScanMultiplePagesAsync(ScanRequest request);
        Task<bool> TestConnectionAsync(string sourceName);
        Task<byte[]> GetScannedDocumentAsync(string filePath);
        Task<bool> DeleteScannedDocumentAsync(string filePath);
    }
}
