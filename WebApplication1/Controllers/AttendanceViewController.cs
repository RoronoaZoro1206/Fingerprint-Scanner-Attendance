using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers
{
    /// <summary>
    /// MVC Controller for attendance scanning views
    /// Routes: /Attendance/Scan
    /// </summary>
    public class AttendanceViewController : Controller
    {
        private readonly ILogger<AttendanceViewController> _logger;

        public AttendanceViewController(ILogger<AttendanceViewController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// GET: /Attendance/Scan
        /// Display the attendance scanning interface
        /// </summary>
        [HttpGet]
        [Route("Attendance/Scan")]
        public IActionResult Scan()
        {
            _logger.LogInformation("Loading attendance scan page");
            return View();
        }
    }
}
