using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IAdministrationRepository
{
    Task<IEnumerable<Administration>> GetAllAsync();
    Task<Administration?> GetByIdAsync(int adminId);
    Task<Administration?> GetByUserIdAsync(int userId);
    Task<bool> AddAsync(Administration admin);
    Task<bool> UpdateAsync(Administration admin);
    Task<bool> DeleteAsync(int adminId);
    Task<bool> ExistsAsync(int adminId);
    Task<string> GenerateNextAdminCodeAsync();
}
