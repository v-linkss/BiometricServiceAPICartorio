using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace BiometricService.Controllers
{
    [ApiController]
    [Route("apiservice/")]
    public class APIController : ControllerBase
    {
        private readonly Biometric _biometric;

        public APIController(Biometric biometric)
        {
            _biometric = biometric;
        }

        [HttpGet("capture-hash")]
        public IActionResult Capture()
        {
            return _biometric.CaptureHash();
        }

        [HttpGet("run-scanner")]
        public IActionResult RunScanner()
        {
            return _biometric.RunScannerApp(); // Chama o método para executar o scanner
        }

        [HttpGet("capture-finger")]
        public IActionResult CaptureFinger()
        {
            return _biometric.CaptureFinger();
        }  

        [HttpGet("device-unique-id")]
        public IActionResult DeviceUniqueSerialID()
        {
            return _biometric.DeviceUniqueSerialID();
        }

        

    }
}