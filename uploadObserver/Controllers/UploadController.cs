using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using BiometricService.Observers;

namespace BiometricService.Controllers
{
    public class UploadController : Controller
    {
        private readonly UploadFileObservable _observable;

        public UploadController(UploadFileObservable observable)
        {
            _observable = observable;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string pessoa_token, string cartorio_token, string tipo)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var path = Path.Combine(uploadDir, file.FileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            var uploadFile = new UploadFile
            {
                FileName = file.FileName,
                Content = await ReadFileToByteArrayAsync(file)
            };

            // Notifique os observadores sobre o novo arquivo
            _observable.Notify(uploadFile);

            return Ok(new { path });
        }

        private async Task<byte[]> ReadFileToByteArrayAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}