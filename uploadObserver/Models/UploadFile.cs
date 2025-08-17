using System;

namespace BiometricService.Models
{
    public class UploadFile
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }

        public UploadFile(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }
    }
}