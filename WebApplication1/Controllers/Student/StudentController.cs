using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

public class StudentController : Controller
{
    private readonly ILogger<StudentController> _logger;
    private readonly IStudentRepository _studentRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IClassRepository _classRepo;
    private readonly IClassScheduleRepository _scheduleRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly IProgramRepository _programRepo;

    public StudentController(
        ILogger<StudentController> logger,
        IStudentRepository studentRepo,
        IEnrollmentRepository enrollmentRepo,
        IClassRepository classRepo,
        IClassScheduleRepository scheduleRepo,
        ICourseRepository courseRepo,
        IProgramRepository programRepo)
    {
        _logger = logger;
        _studentRepo = studentRepo;
        _enrollmentRepo = enrollmentRepo;
        _classRepo = classRepo;
        _scheduleRepo = scheduleRepo;
        _courseRepo = courseRepo;
        _programRepo = programRepo;
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