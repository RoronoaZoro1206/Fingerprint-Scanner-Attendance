using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ILogger<AdminController> logger,
        IAdminOverviewRepository overviewRepo,
        DatabaseHelper db)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    

    public IActionResult Users()
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
    
    public IActionResult Faculty()
    {
        return View();
    }
    
    public IActionResult StudentId()
    {
        return View();
    }
    
    public IActionResult Sections()
    {
        return View();
    }
    
    public IActionResult Students()
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
    
    public IActionResult ClassSchedules()
    {
        return View();
    }
    
    public IActionResult AdminRegisterFingerprint()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}