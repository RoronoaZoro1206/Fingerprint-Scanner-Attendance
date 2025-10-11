using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Controllers;

public class DepartmentController : Controller
{
    private readonly ILogger<DepartmentController> _logger;

    
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}