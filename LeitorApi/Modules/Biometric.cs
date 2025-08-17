using BiometricService;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
// using NITGEN.SDK.NBioBSP;
using System.Text.Json.Nodes;
// using static NITGEN.SDK.NBioBSP.NBioAPI.Export;
// using static NITGEN.SDK.NBioBSP.NBioAPI.Type;

public class Biometric
{
    private readonly APIService APIServiceInstance;

    public Biometric(APIService apiService)
    {
        APIServiceInstance = apiService;
    }
    public IActionResult CaptureHash()
    {
        // HFIR auditHFIR = new HFIR();
        // APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        // uint ret = APIServiceInstance._NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.ENROLL, out NBioAPI.Type.HFIR hCapturedFIR, NBioAPI.Type.TIMEOUT.DEFAULT, auditHFIR, null);
        // APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        // if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
        //     new JsonObject
        //     {
        //         ["message"] = $"Error on Capture: {ret}",
        //         ["success"] = false
        //     }
        // );

        // NBioAPI.Export NBioExport = new NBioAPI.Export(APIServiceInstance._NBioAPI);
        // NBioExport.NBioBSPToImage(auditHFIR, out NBioAPI.Export.EXPORT_AUDIT_DATA exportAuditData);

        // string tempPath = Environment.ExpandEnvironmentVariables(@"%TEMP%\fingers-registered");

        // if (!Directory.Exists(tempPath))
        // {
        //     Directory.CreateDirectory(tempPath);
        // }

        // DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
        // FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
        // foreach (FileInfo file in files)
        // {
        //     if (file.Extension.ToLower() == ".jpg")
        //     {
        //         file.Delete();
        //     }
        // }

        // APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out NBioAPI.Type.FIR_TEXTENCODE textFIR, true);

        // foreach (NBioAPI.Export.AUDIT_DATA finger in exportAuditData.AuditData)
        // {
        //     APIServiceInstance._NBioAPI.ImgConvRawToJpgBuf(finger.Image[0].Data, exportAuditData.ImageWidth, exportAuditData.ImageHeight, 1, out byte[] imgData);
        //     Directory.CreateDirectory(tempPath);
        //     File.WriteAllBytes($"{tempPath}\\finger_{finger.FingerID}.jpg", imgData);
        // }

        // return new OkObjectResult(
        // new JsonObject
        // {
        //     ["fingers-registered"] = exportAuditData.AuditData.GetLength(0),
        //     ["template"] = textFIR.TextFIR,
        //     ["success"] = true,
        // });
    }
    
    public IActionResult RunScannerApp()
    {
        // try
        // {
        //     string relativePath = Path.Combine(Directory.GetCurrentDirectory(), @"..\ScannerAPP\TestApp\bin\Debug\TestApp.exe");
        //     string scannerAppPath = Path.GetFullPath(relativePath); // Obter o caminho absoluto

        //     Process process = new Process();
        //     process.StartInfo.FileName = scannerAppPath;
        //     process.StartInfo.UseShellExecute = true; 
        //     process.Start();

        //     process.WaitForExit();

        //     if (process.ExitCode != 0)
        //     {
        //         return new OkObjectResult(new { message = "Scanner executado com sucesso", success = true });
        //     }
        //     else
        //     {
        //         return new BadRequestObjectResult(new { message = "Falha ao executar o scanner", success = false });
        //     }
        // }
        // catch (Exception ex)
        // {
        //     return new BadRequestObjectResult(new { message = ex.Message, success = false });
        // }
    }
    public IActionResult CaptureFinger()
    {
        // APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);

        // uint ret = APIServiceInstance._NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.VERIFY, out NBioAPI.Type.HFIR hCapturedFIR, NBioAPI.Type.TIMEOUT.DEFAULT, null, null);
        // APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);

        // if (ret != NBioAPI.Error.NONE) return new BadRequestObjectResult(
        //     new JsonObject
        //     {
        //         ["message"] = $"Error on Capture: {ret}",
        //         ["success"] = false
        //     }
        // );

        // APIServiceInstance._NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out NBioAPI.Type.FIR_TEXTENCODE textFIR, true);

        // string fingerprintHash = textFIR.TextFIR;

        // return new OkObjectResult(
        //     new JsonObject
        //     {
        //         ["hash"] = fingerprintHash,
        //         ["success"] = true
        //     }
        // );
    }

    public IActionResult DeviceUniqueSerialID()
    {
        // APIServiceInstance._NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        // byte[] input = new byte[8];
        // APIServiceInstance._NBioAPI.DeviceIoControl(514, input, out byte[] deviceId);
        // APIServiceInstance._NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
        // return new OkObjectResult(
        //     new JsonObject
        //     {
        //         ["serial"] = BitConverter.ToString(deviceId),
        //         ["success"] = true
        //     }
        // );
    }

    
}
