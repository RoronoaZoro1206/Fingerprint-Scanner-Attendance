using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Data;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    [RoleAuthorize(USER_ROLES.ADMIN)]
    public class AdminOverviewController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IFacultyRepository _facultyRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly IAdministrationAuditRepository _auditRepo;
        private readonly DatabaseHelper _db;

        public AdminOverviewController(
            IUserRepository userRepo,
            ICourseRepository courseRepo,
            IFacultyRepository facultyRepo,
            IRoomRepository roomRepo,
            IAdministrationAuditRepository auditRepo,
            DatabaseHelper db)
        {
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _facultyRepo = facultyRepo;
            _roomRepo = roomRepo;
            _auditRepo = auditRepo;
            _db = db;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/AdminOverview.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var users = await _userRepo.GetAllAsync();
                var courses = await _courseRepo.GetAllAsync();
                var faculty = await _facultyRepo.GetAllAsync();
                var rooms = await _roomRepo.GetAllAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalUsers = users.Count(),
                        totalCourses = courses.Count(),
                        totalFaculty = faculty.Count(),
                        totalRooms = rooms.Count()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
            {
                var audits = await _auditRepo.GetAllAsync();
                var recentAudits = audits.OrderByDescending(a => a.AudActionTime).Take(5);

                var activities = recentAudits.Select(a => new
                {
                    action = a.AudAction,
                    details = a.AudDetails,
                    timestamp = a.AudActionTime.ToString("yyyy-MM-dd HH:mm:ss")
                });

                return Json(new { success = true, data = activities });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckDatabaseConnection()
        {
            try
            {
                var query = "SELECT 1";
                await _db.ExecuteQueryAsync(query);
                return Json(new { success = true, status = "Online" });
            }
            catch
            {
                return Json(new { success = false, status = "Offline" });
            }
        }
    }
}
