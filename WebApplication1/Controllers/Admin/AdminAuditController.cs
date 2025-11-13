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
        private readonly IAuthenticationAuditRepository _authAuditRepo;

        public AdminAuditController(
            ILogger<AdminAuditController> logger,
            IAdministrationAuditRepository auditRepo,
            IAdministrationRepository adminRepo,
            IUserRepository userRepo,
            IAuthenticationAuditRepository authAuditRepo)
        {
            _logger = logger;
            _auditRepo = auditRepo;
            _adminRepo = adminRepo;
            _userRepo = userRepo;
            _authAuditRepo = authAuditRepo;
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
                // STEP 1: Get administration audit logs
                var adminAudits = await _auditRepo.GetAllAsync();
                _logger.LogInformation($"Found {adminAudits.Count()} administration audit logs");
                
                // STEP 2: Get authentication audit logs
                var authAudits = await _authAuditRepo.GetAllAsync();
                _logger.LogInformation($"Found {authAudits.Count()} authentication audit logs");
                
                var admins = await _adminRepo.GetAllAsync();
                _logger.LogInformation($"Found {admins.Count()} admins");
                
                var users = await _userRepo.GetAllAsync();
                _logger.LogInformation($"Found {users.Count()} users");

                // STEP 3: Map administration audit logs
                var adminAuditData = adminAudits.Select(audit =>
                {
                    var admin = admins.FirstOrDefault(a => a.AdminId == audit.AdminId);
                    var user = admin != null ? users.FirstOrDefault(u => u.UserId == admin.UserId) : null;

                    string logType = audit.AudAction == "Biometric Login" ? "Biometric Login" : "Admin Action";

                    return new
                    {
                        audId = $"ADM-{audit.AudId}",
                        adminId = audit.AdminId,
                        adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
                        adminCode = admin?.AdminCode ?? "N/A",
                        audAction = audit.AudAction,
                        audDetails = audit.AudDetails ?? "",
                        audActionTime = audit.AudActionTime,
                        audActionTimeFormatted = audit.AudActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        logType = logType
                    };
                }).ToList();

                // STEP 4: Map authentication audit logs
                var authAuditData = authAudits.Select(authAudit =>
                {
                    var user = users.FirstOrDefault(u => u.UserId == authAudit.UserId);
                    var admin = admins.FirstOrDefault(a => a.UserId == authAudit.UserId);

                    // Determine log type based on action
                    string logType = authAudit.AuthAction switch
                    {
                        "Password Login" => "Password Login",
                        "Biometric Login" => "Biometric Login",
                        "Failed Login" => "Failed Login",
                        "User Logout" => "User Logout",
                        _ => "Authentication"
                    };

                    return new
                    {
                        audId = $"AUTH-{authAudit.AuthAuditId}",
                        adminId = admin?.AdminId ?? 0,
                        adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
                        adminCode = admin?.AdminCode ?? "N/A",
                        audAction = authAudit.AuthAction,
                        audDetails = authAudit.AuthDetails ?? "",
                        audActionTime = authAudit.AuthActionTime,
                        audActionTimeFormatted = authAudit.AuthActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        logType = logType
                    };
                }).ToList();

                // STEP 5: Combine and sort by timestamp (newest first)
                var combinedData = adminAuditData.Concat(authAuditData)
                    .OrderByDescending(a => a.audActionTime)
                    .ToList();

                _logger.LogInformation($"? Returning {combinedData.Count} total audit records ({adminAuditData.Count} admin actions + {authAuditData.Count} authentication events)");
                
                return Json(new { data = combinedData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit logs");
                return Json(new { error = "Failed to fetch audit logs" });
            }
        }

        // ? FIX: Updated to handle both admin and auth audit logs
        [HttpGet]
        public async Task<IActionResult> GetById(int id, bool isAuth = false)
        {
            try
            {
                if (isAuth)
                {
                    // Get from authentication_audit table
                    var authAudit = await _authAuditRepo.GetByIdAsync(id);
                    if (authAudit == null)
                    {
                        return Json(new { success = false, message = "Authentication audit log not found" });
                    }

                    var users = await _userRepo.GetAllAsync();
                    var user = users.FirstOrDefault(u => u.UserId == authAudit.UserId);
                    
                    var admins = await _adminRepo.GetAllAsync();
                    var admin = admins.FirstOrDefault(a => a.UserId == authAudit.UserId);

                    var auditData = new
                    {
                        audId = id,
                        adminId = admin?.AdminId ?? 0,
                        adminName = user != null ? $"{user.UserFirstName} {user.UserLastName}" : "Unknown",
                        adminCode = admin?.AdminCode ?? "N/A",
                        audAction = authAudit.AuthAction,
                        audDetails = authAudit.AuthDetails ?? "",
                        audActionTime = authAudit.AuthActionTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    return Json(new { success = true, data = auditData });
                }
                else
                {
                    // Get from administration_audit table
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
                // Get logs from BOTH tables
                var adminAudits = await _auditRepo.GetAllAsync();
                var authAudits = await _authAuditRepo.GetAllAsync();

                var today = DateTime.Today;
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                // Calculate combined statistics
                var totalLogs = adminAudits.Count() + authAudits.Count();
                var todayLogs = adminAudits.Count(a => a.AudActionTime.Date == today) + 
                               authAudits.Count(a => a.AuthActionTime.Date == today);
                var weekLogs = adminAudits.Count(a => a.AudActionTime >= thisWeek) + 
                              authAudits.Count(a => a.AuthActionTime >= thisWeek);
                var monthLogs = adminAudits.Count(a => a.AudActionTime >= thisMonth) + 
                               authAudits.Count(a => a.AuthActionTime >= thisMonth);

                // Get top 3 recent actions from BOTH tables
                var adminRecentActions = adminAudits
                    .OrderByDescending(a => a.AudActionTime)
                    .Take(3)
                    .Select(a => new {
                        action = a.AudAction,
                        time = a.AudActionTime.ToString("MMM dd, HH:mm"),
                        type = a.AudAction == "Biometric Login" ? "Biometric" : "Admin",
                        timestamp = a.AudActionTime
                    });

                var authRecentActions = authAudits
                    .OrderByDescending(a => a.AuthActionTime)
                    .Take(3)
                    .Select(a => new {
                        action = a.AuthAction,
                        time = a.AuthActionTime.ToString("MMM dd, HH:mm"),
                        type = a.AuthAction == "Biometric Login" ? "Biometric" : "Authentication",
                        timestamp = a.AuthActionTime
                    });

                // Combine and take top 3 most recent
                var recentActions = adminRecentActions.Concat(authRecentActions)
                    .OrderByDescending(a => a.timestamp)
                    .Take(3)
                    .Select(a => new {
                        action = a.action,
                        time = a.time,
                        type = a.type
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

        // ? FIX: Updated to handle both admin and auth audit logs
        [HttpDelete]
        public async Task<IActionResult> Delete(int id, bool isAuth = false)
        {
            try
            {
                if (isAuth)
                {
                    // Delete from authentication_audit table
                    var authAudit = await _authAuditRepo.GetByIdAsync(id);
                    if (authAudit == null)
                    {
                        return Json(new { success = false, message = "Authentication audit log not found" });
                    }

                    var result = await _authAuditRepo.DeleteAsync(id);

                    if (result)
                    {
                        return Json(new { success = true, message = "Authentication audit log deleted successfully" });
                    }
                    return Json(new { success = false, message = "Failed to delete authentication audit log" });
                }
                else
                {
                    // Delete from administration_audit table
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
