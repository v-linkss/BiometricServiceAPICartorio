namespace HelloWorldApi.Models
{
    public class ScanResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int PageCount { get; set; }
        public string OutputFormat { get; set; } = string.Empty;
        public DateTime ScanDateTime { get; set; }
    }
}
