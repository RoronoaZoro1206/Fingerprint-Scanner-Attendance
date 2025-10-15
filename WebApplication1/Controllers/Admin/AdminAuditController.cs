using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    public class AdminAuditController : Controller
    {
        private readonly ILogger<AdminAuditController> _logger;
        private readonly IAdministrationAuditRepository _auditRepo;
        private readonly IAdministrationRepository _adminRepo;
        private readonly IUserRepository _userRepo;

        public AdminAuditController(
            ILogger<AdminAuditController> logger,
            IAdministrationAuditRepository auditRepo,
            IAdministrationRepository adminRepo,
            IUserRepository userRepo)
        {
            _logger = logger;
            _auditRepo = auditRepo;
            _adminRepo = adminRepo;
            _userRepo = userRepo;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/AdminAudit.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var audits = await _auditRepo.GetAllAsync();
                var admins = await _adminRepo.GetAllAsync();
                var users = await _userRepo.GetAllAsync();

                var auditData = audits.Select(audit =>
                {
                    var admin = admins.FirstOrDefault(a => a.AdminId == audit.AdminId);
                    var user = admin != null ? users.FirstOrDefault(u => u.UserId == admin.UserId) : null;

                    return new
                    {
                        audId = audit.AudId,
                        adminId = audit.AdminId,
                        adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
                        adminCode = admin?.AdminCode ?? "N/A",
                        audAction = audit.AudAction,
                        audDetails = audit.AudDetails ?? "",
                        audActionTime = audit.AudActionTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }).OrderByDescending(a => a.audActionTime);

                return Json(new { data = auditData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit logs");
                return Json(new { error = "Failed to fetch audit logs" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var audit = await _auditRepo.GetByIdAsync(id);
                if (audit == null)
                {
                    return Json(new { success = false, message = "Audit log not found" });
                }

                var admins = await _adminRepo.GetAllAsync();
                var admin = admins.FirstOrDefault(a => a.AdminId == audit.AdminId);
                
                var users = await _userRepo.GetAllAsync();
                var user = admin != null ? users.FirstOrDefault(u => u.UserId == admin.UserId) : null;

                var auditData = new
                {
                    audId = audit.AudId,
                    adminId = audit.AdminId,
                    adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
                    adminCode = admin?.AdminCode ?? "N/A",
                    audAction = audit.AudAction,
                    audDetails = audit.AudDetails ?? "",
                    audActionTime = audit.AudActionTime.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Json(new { success = true, data = auditData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit log by ID");
                return Json(new { success = false, message = "Failed to fetch audit log" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var audits = await _auditRepo.GetAllAsync();
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var stats = new
                {
                    totalLogs = audits.Count(),
                    todayLogs = audits.Count(a => a.AudActionTime.Date == today),
                    weekLogs = audits.Count(a => a.AudActionTime >= thisWeek),
                    monthLogs = audits.Count(a => a.AudActionTime >= thisMonth),
                    recentActions = audits
                        .OrderByDescending(a => a.AudActionTime)
                        .Take(5)
                        .Select(a => new
                        {
                            action = a.AudAction,
                            time = a.AudActionTime.ToString("MMM dd, HH:mm")
                        })
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit stats");
                return Json(new { success = false, message = "Failed to fetch statistics" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var exists = await _auditRepo.ExistsAsync(id);
                if (!exists)
                {
                    return Json(new { success = false, message = "Audit log not found" });
                }

                var result = await _auditRepo.DeleteAsync(id);

                if (result)
                {
                    return Json(new { success = true, message = "Audit log deleted successfully" });
                }
                return Json(new { success = false, message = "Failed to delete audit log" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit log");
                return Json(new { success = false, message = "An error occurred while deleting audit log" });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
