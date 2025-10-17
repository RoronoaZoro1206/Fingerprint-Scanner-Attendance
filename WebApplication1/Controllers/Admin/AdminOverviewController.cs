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
        private readonly IAdminOverviewRepository _overviewRepo;
        private readonly DatabaseHelper _db;

        public AdminOverviewController(
            IAdminOverviewRepository overviewRepo,
            DatabaseHelper db)
        {
            _overviewRepo = overviewRepo;
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
                // Optimized: Single query to get all counts
                var stats = await _overviewRepo.GetOverviewStatsAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalUsers = stats.TotalUsers,
                        totalCourses = stats.TotalCourses,
                        totalFaculty = stats.TotalFaculty,
                        totalRooms = stats.TotalRooms
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
                // Optimized: Single query with LIMIT to get recent activities
                var activities = await _overviewRepo.GetRecentActivityAsync(5);

                var result = activities.Select(a => new
                {
                    action = a.Action,
                    details = a.Details,
                    timestamp = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                });

                return Json(new { success = true, data = result });
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
