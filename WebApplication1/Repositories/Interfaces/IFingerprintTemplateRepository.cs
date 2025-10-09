using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IFingerprintTemplateRepository
{
    Task<IEnumerable<FingerprintTemplate>> GetAllAsync();
    Task<FingerprintTemplate?> GetByIdAsync(int ftempId);
    Task<bool> AddAsync(FingerprintTemplate template);
    Task<bool> UpdateAsync(FingerprintTemplate template);
    Task<bool> DeleteAsync(int ftempId);
    Task<bool> ExistsAsync(int ftempId);
}