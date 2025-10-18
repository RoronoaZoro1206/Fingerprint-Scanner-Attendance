using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentController : Controller
{
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        ILogger<StudentController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult StudentEnrollment()
    {
        return View();
    }
    
    public IActionResult StudentClasses()
    {
        return View();
    }
    
    public IActionResult StudentRegisterFingerprint()
    {
        return View();
    }
    
    public IActionResult StudentAttendance()
    {
        return View();
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}