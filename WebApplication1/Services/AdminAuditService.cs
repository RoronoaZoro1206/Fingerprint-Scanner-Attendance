using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    public class AdminAuditService
    {
        private readonly IAdministrationAuditRepository _auditRepo;
        private readonly IAdministrationRepository _adminRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AdminAuditService> _logger;

        public AdminAuditService(
            IAdministrationAuditRepository auditRepo,
            IAdministrationRepository adminRepo,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AdminAuditService> logger)
        {
            _auditRepo = auditRepo;
            _adminRepo = adminRepo;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActionAsync(string action, string? details = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Audit log skipped: No user ID found in session");
                    return;
                }

                var admins = await _adminRepo.GetAllAsync();
                var admin = admins.FirstOrDefault(a => a.UserId == userId.Value);
                
                if (admin == null)
                {
                    _logger.LogWarning($"Audit log skipped: No admin record found for user ID {userId.Value}");
                    return;
                }

                var audit = new AdministrationAudit
                {
                    AdminId = admin.AdminId,
                    AudAction = action,
                    AudDetails = details,
                    AudActionTime = DateTime.Now
                };

                var result = await _auditRepo.AddAsync(audit);
                
                if (result)
                {
                    _logger.LogInformation($"Audit logged: {action} by admin {admin.AdminCode}");
                }
                else
                {
                    _logger.LogError($"Failed to log audit: {action}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error logging audit action: {action}");
                // Silently fail to not disrupt the main operation
            }
        }

        private int? GetCurrentUserId()
        {
            // Get user ID from session
            var userId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
            return userId;
        }
    }
}
