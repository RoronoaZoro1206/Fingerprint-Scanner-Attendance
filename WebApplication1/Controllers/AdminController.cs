using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Controllers;

public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Departments()
    {
        return View();
    }
    
    public IActionResult Programs()
    {
        return View();
    }
    
    public IActionResult Courses()
    {
        return View();
    }
    
    public IActionResult AcademicTerms()
    {
        return View();
    }
    
    public IActionResult Sections()
    {
        return View();
    }
    
    public IActionResult Rooms()
    {
        return View();
    }
    
    public IActionResult Classes()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}