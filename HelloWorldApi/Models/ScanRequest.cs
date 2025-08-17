using System.ComponentModel.DataAnnotations;

namespace HelloWorldApi.Models
{
    public class ScanRequest
    {
        [Required]
        public string SourceName { get; set; } = string.Empty;
        
        public bool UseDocumentFeeder { get; set; } = false;
        
        public bool ShowTwainUI { get; set; } = true;
        
        public bool ShowProgressIndicatorUI { get; set; } = true;
        
        public bool UseDuplex { get; set; } = false;
        
        public bool AutomaticRotate { get; set; } = true;
        
        public bool AutomaticBorderDetection { get; set; } = true;
        
        public int Resolution { get; set; } = 300;
        
        public string OutputFormat { get; set; } = "PDF"; // PDF, JPEG, PNG, TIFF
        
        public string OutputFileName { get; set; } = string.Empty;
    }
}
