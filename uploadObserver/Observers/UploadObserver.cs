using System;
using System.Net.Http;
using System.Threading.Tasks;
using BiometricService.Observers;

namespace BiometricService.Observers
{
    public class UploadObserver : IObserver<UploadFile>
    {
        private readonly HttpClient _httpClient;
        private readonly string _uploadUrl;

        public UploadObserver(HttpClient httpClient, string uploadUrl)
        {
            _httpClient = httpClient;
            _uploadUrl = uploadUrl;
        }

        public async void Update(UploadFile file)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(file.Content), "file", file.FileName);

            var response = await _httpClient.PostAsync(_uploadUrl, content);
            response.EnsureSuccessStatusCode();
        }
    }
}