using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    [RoleAuthorize(USER_ROLES.FACULTY)]
    public class FacultyController : Controller
    {
        private readonly ILogger<FacultyController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IClassRepository _classRepository;

        public FacultyController(
            ILogger<FacultyController> logger,
            IFacultyRepository facultyRepository,
            IClassRepository classRepository)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _classRepository = classRepository;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult FacultyAttendance()
        {
            return View();
        }
        
        public IActionResult FacultyReports()
        {
            return View();
        }
        
        public IActionResult FacultyRegisterFingerprint()
        {
            return View();
        }
    }
}
