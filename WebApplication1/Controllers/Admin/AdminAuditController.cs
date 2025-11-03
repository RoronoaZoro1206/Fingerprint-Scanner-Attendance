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

    // Get all audit logs from administration_audit table only (unified source)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
   {
       // Get all audit logs from administration_audit table
var audits = await _auditRepo.GetAllAsync();
        _logger.LogInformation($"Found {audits.Count()} audit logs");
   
       var admins = await _adminRepo.GetAllAsync();
    _logger.LogInformation($"Found {admins.Count()} admins");
         
     var users = await _userRepo.GetAllAsync();
        _logger.LogInformation($"Found {users.Count()} users");

       // Map audit logs with admin and user information
       var auditData = audits.Select(audit =>
                {
         var admin = admins.FirstOrDefault(a => a.AdminId == audit.AdminId);
          var user = admin != null ? users.FirstOrDefault(u => u.UserId == admin.UserId) : null;

  // Determine log type based on action
      string logType = audit.AudAction == "Biometric Login" ? "Biometric Login" : "Admin Action";

      return new
           {
   audId = audit.AudId,
           adminId = audit.AdminId,
  adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
       adminCode = admin?.AdminCode ?? "N/A",
    audAction = audit.AudAction,
           audDetails = audit.AudDetails ?? "",
            audActionTime = audit.AudActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
          logType = logType
      };
    }).OrderByDescending(a => a.audActionTime).ToList();

      _logger.LogInformation($"Returning {auditData.Count} audit records (sorted by newest first)");
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

  // Get audit statistics from administration_audit table only
        [HttpGet]
        public async Task<IActionResult> GetStats()
 {
 try
            {
                var audits = await _auditRepo.GetAllAsync();

      var today = DateTime.Today;
          var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

     // Calculate statistics from administration_audit table only
              var totalLogs = audits.Count();
                var todayLogs = audits.Count(a => a.AudActionTime.Date == today);
        var weekLogs = audits.Count(a => a.AudActionTime >= thisWeek);
       var monthLogs = audits.Count(a => a.AudActionTime >= thisMonth);

  // Get top 3 recent actions from administration_audit
                var recentActions = audits
             .OrderByDescending(a => a.AudActionTime)
  .Take(3)
.Select(a => new {
       action = a.AudAction,
   time = a.AudActionTime.ToString("MMM dd, HH:mm"),
           type = a.AudAction == "Biometric Login" ? "Biometric" : "Admin"
                 });

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
