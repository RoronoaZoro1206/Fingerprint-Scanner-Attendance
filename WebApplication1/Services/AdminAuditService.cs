using System.Security.Claims;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    public class AdminAuditService
    {
        private readonly IAdministrationAuditRepository _auditRepo;
        private readonly IAdministrationRepository _adminRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminAuditService(
            IAdministrationAuditRepository auditRepo,
            IAdministrationRepository adminRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _auditRepo = auditRepo;
            _adminRepo = adminRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(string action, string? details = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue) return;

                var admins = await _adminRepo.GetAllAsync();
                var admin = admins.FirstOrDefault(a => a.UserId == userId.Value);
                
                if (admin == null) return;

                var audit = new AdministrationAudit
                {
                    AdminId = admin.AdminId,
                    AudAction = action,
                    AudDetails = details,
                    AudActionTime = DateTime.Now
                };

                await _auditRepo.AddAsync(audit);
            }
            catch (Exception)
            {
                // Silently fail to not disrupt the main operation
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return null;
            
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}
