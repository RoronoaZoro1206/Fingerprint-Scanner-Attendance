using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    [RoleAuthorize(USER_ROLES.ADMIN)]
    public class AdminAuditController : Controller
    {
        private readonly ILogger<AdminAuditController> _logger;
        private readonly IAdministrationAuditRepository _auditRepo;
        private readonly IAdministrationRepository _adminRepo;
        private readonly IUserRepository _userRepo;
        private readonly IScanLogRepository _scanLogRepo; // NEW: Add scan logs

        public AdminAuditController(
            ILogger<AdminAuditController> logger,
            IAdministrationAuditRepository auditRepo,
            IAdministrationRepository adminRepo,
            IUserRepository userRepo,
            IScanLogRepository scanLogRepo) // NEW: Inject scan log repo
        {
            _logger = logger;
            _auditRepo = auditRepo;
            _adminRepo = adminRepo;
            _userRepo = userRepo;
            _scanLogRepo = scanLogRepo; // NEW: Store scan log repo
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/AdminAudit.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            try
            {
                var audits = await _auditRepo.GetAllAsync();
                return Json(new { 
                    success = true, 
                    count = audits.Count(),
                    audits = audits.Select(a => new {
                        a.AudId,
                        a.AdminId,
                        a.AudAction,
                        a.AudDetails,
                        a.AudActionTime
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

    // NEW: Get combined audit logs (admin actions + biometric logins)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var audits = await _auditRepo.GetAllAsync();
                _logger.LogInformation($"Found {audits.Count()} admin audit logs");
                
                var admins = await _adminRepo.GetAllAsync();
                _logger.LogInformation($"Found {admins.Count()} admins");
                
                var users = await _userRepo.GetAllAsync();
                _logger.LogInformation($"Found {users.Count()} users");

                // NEW: Get biometric scan logs
                var scanLogs = await _scanLogRepo.GetAllAsync();
                var biometricLogins = scanLogs
                    .Where(s => s.ScanSuccess == true && s.ScanLogMessage != null && 
              s.ScanLogMessage.Contains("biometric login", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                _logger.LogInformation($"Found {biometricLogins.Count} biometric login logs");

         // Map admin audit logs
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
                        audActionTime = audit.AudActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        logType = "Admin Action" // NEW: Identify log type
                    };
                }).ToList();

                // NEW: Map biometric login logs
                var biometricData = biometricLogins.Select(log =>
                {
                    var user = log.UserId.HasValue ? users.FirstOrDefault(u => u.UserId == log.UserId.Value) : null;
                    var admin = user != null ? admins.FirstOrDefault(a => a.UserId == user.UserId) : null;

                    return new
                    {
                        audId = log.ScanId, // Use scan ID as audit ID
                        adminId = admin?.AdminId ?? 0,
                        adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
                        adminCode = admin?.AdminCode ?? "N/A",
                        audAction = "Biometric Login", // NEW: Label as biometric login
                        audDetails = log.ScanLogMessage ?? "",
                        audActionTime = log.ScanTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        logType = "Biometric Login" // NEW: Identify log type
                    };
                });

   // NEW: Combine both log types and sort by time
                var combinedData = auditData.Concat(biometricData)
 .OrderByDescending(a => a.audActionTime)
                    .ToList();

                _logger.LogInformation($"Returning {combinedData.Count} combined audit records ({auditData.Count} admin actions + {biometricData.Count()} biometric logins)");
                return Json(new { data = combinedData });
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

        // NEW: Get audit statistics (including biometric logins)
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var audits = await _auditRepo.GetAllAsync();
                var scanLogs = await _scanLogRepo.GetAllAsync();
                var biometricLogins = scanLogs
                    .Where(s => s.ScanSuccess == true && s.ScanLogMessage != null && 
 s.ScanLogMessage.Contains("biometric login", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

 // Combine admin audits and biometric logins for stats
                var totalLogs = audits.Count() + biometricLogins.Count;
                var todayLogs = audits.Count(a => a.AudActionTime.Date == today) + 
                 biometricLogins.Count(b => b.ScanTime.Date == today);
                var weekLogs = audits.Count(a => a.AudActionTime >= thisWeek) + 
   biometricLogins.Count(b => b.ScanTime >= thisWeek);
                var monthLogs = audits.Count(a => a.AudActionTime >= thisMonth) + 
biometricLogins.Count(b => b.ScanTime >= thisMonth);

 // Get recent actions (both types)
                var users = await _userRepo.GetAllAsync();
                var recentAdminActions = audits
.OrderByDescending(a => a.AudActionTime)
                   .Take(3)
                     .Select(a => new {
    action = a.AudAction,
           time = a.AudActionTime.ToString("MMM dd, HH:mm"),
   type = "Admin"
      });

       var recentBiometricLogins = biometricLogins
.OrderByDescending(b => b.ScanTime)
      .Take(2)
       .Select(b => {
      var user = b.UserId.HasValue ? users.FirstOrDefault(u => u.UserId == b.UserId.Value) : null;
       var userName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown";
         return new {
        action = $"Biometric Login - {userName}",
      time = b.ScanTime.ToString("MMM dd, HH:mm"),
       type = "Biometric"
           };
             });

 var recentActions = recentAdminActions.Concat(recentBiometricLogins)
       .OrderByDescending(a => a.time)
      .Take(5);

             var stats = new
                {
       totalLogs = totalLogs,
   todayLogs = todayLogs,
           weekLogs = weekLogs,
     monthLogs = monthLogs,
         recentActions = recentActions
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
