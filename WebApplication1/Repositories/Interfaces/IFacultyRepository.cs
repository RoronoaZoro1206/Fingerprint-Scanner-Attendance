using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;

public interface IFacultyRepository
{
    Task<IEnumerable<Faculty>> GetAllAsync();
    Task<Faculty?> GetByIdAsync(int facId);
    Task<Faculty?> GetByUserIdAsync(int userId);
    Task<IEnumerable<Faculty>> GetByDepartmentAsync(int deptId);
    Task<bool> AddAsync(Faculty faculty);
    Task<bool> UpdateAsync(Faculty faculty);
    Task<bool> DeleteAsync(int facId);
    Task<bool> ExistsAsync(int facId);
}