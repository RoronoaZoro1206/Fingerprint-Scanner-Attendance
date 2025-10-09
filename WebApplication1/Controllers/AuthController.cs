using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers;

public class AuthController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult FingerprintLogin()
    {
        return View();
    }
}