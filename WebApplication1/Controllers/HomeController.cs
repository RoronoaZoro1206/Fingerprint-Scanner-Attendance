using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Fingerprint;
using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly FingerprintService _fingerprintService;

    public HomeController(ILogger<HomeController> logger, FingerprintService fingerprintService)
    {
        _logger = logger;
        _fingerprintService = fingerprintService;
    }

    private void SetFingerprintDiagnostics()
    {
        ViewData["FingerprintStatus"] = _fingerprintService.IsInitialized ? "Initialized" : "Not initialized";
        ViewData["DeviceCount"] = _fingerprintService.DeviceCount;
        ViewData["ImageWidth"] = _fingerprintService.ImageWidth;
        ViewData["ImageHeight"] = _fingerprintService.ImageHeight;
        ViewData["LastError"] = _fingerprintService.LastError;
    }

    public IActionResult Index()
    {
        _fingerprintService.Initialize();
        SetFingerprintDiagnostics();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> TestCapture()
    {
        ViewData["Capturing"] = true;
        SetFingerprintDiagnostics();
        // Simulate waiting for animation (e.g., 3 seconds)
        await Task.Delay(3000);
        var template = _fingerprintService.CaptureFingerprintTemplate();
        ViewData["CaptureResult"] = template != null && template.Length > 0
            ? $"Fingerprint captured successfully. Template size: {template.Length} bytes."
            : "Fingerprint capture failed.";
        ViewData["Capturing"] = false;
        SetFingerprintDiagnostics();
        return View("Index");
    }

    [HttpPost]
    public IActionResult Reinitialize()
    {
        bool result = _fingerprintService.CloseAndReinitialize();
        ViewData["ReinitResult"] = result ? "Reinitialized successfully." : "Reinitialization failed.";
        SetFingerprintDiagnostics();
        return View("Index");
    }

    [HttpPost]
    public IActionResult CloseDevice()
    {
        bool result = _fingerprintService.CloseDeviceOnly();
        ViewData["CloseResult"] = result ? "Device closed successfully." : "Device was not open.";
        SetFingerprintDiagnostics();
        return View("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}