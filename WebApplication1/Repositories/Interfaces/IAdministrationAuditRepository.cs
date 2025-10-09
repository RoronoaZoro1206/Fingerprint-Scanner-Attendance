using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IAdministrationAuditRepository
{
    Task<IEnumerable<AdministrationAudit>> GetAllAsync();
    Task<AdministrationAudit?> GetByIdAsync(int audId);
    Task<bool> AddAsync(AdministrationAudit audit);
    Task<bool> UpdateAsync(AdministrationAudit audit);
    Task<bool> DeleteAsync(int audId);
    Task<bool> ExistsAsync(int audId);
}