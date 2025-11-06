using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    /// <summary>
    /// Repository for authentication audit logs
    /// Handles ALL authentication events (password, biometric, logout) for ALL roles
    /// </summary>
    public interface IAuthenticationAuditRepository
    {
        Task<IEnumerable<AuthenticationAudit>> GetAllAsync();
        Task<AuthenticationAudit?> GetByIdAsync(int authAuditId);
        Task<IEnumerable<AuthenticationAudit>> GetByUserIdAsync(int userId);
        Task<IEnumerable<AuthenticationAudit>> GetByActionAsync(string action);
        Task<IEnumerable<AuthenticationAudit>> GetRecentAsync(int count = 50);
        Task<bool> AddAsync(AuthenticationAudit audit);
        Task<bool> DeleteAsync(int authAuditId);
        Task<bool> ExistsAsync(int authAuditId);
    }
}
