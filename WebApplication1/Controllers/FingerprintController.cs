using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Fingerprint;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class FingerprintController : Controller
{
    private readonly FingerprintService _fingerService = new FingerprintService();

    public ActionResult Index()
    {
        _fingerService.Initialize();
        return View();
    }

    [HttpPost]
    public ActionResult Capture()
    {
        var template = _fingerService.CaptureFingerprintTemplate();
        if (template == null)
            return Json(new { success = false, message = "Failed to capture fingerprint" });

        string base64Template = Convert.ToBase64String(template);
        // Store in DB or send to view
        return Json(new { success = true, data = base64Template });
    }

    protected override void Dispose(bool disposing)
    {
        _fingerService.Close();
        base.Dispose(disposing);
    }
}