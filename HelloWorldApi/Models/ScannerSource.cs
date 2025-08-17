namespace HelloWorldApi.Models
{
    public class ScannerSource
    {
        public string Name { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool SupportsDocumentFeeder { get; set; }
        public bool SupportsDuplex { get; set; }
        public int MaxResolution { get; set; }
        public string[] SupportedFormats { get; set; } = Array.Empty<string>();
    }
}
