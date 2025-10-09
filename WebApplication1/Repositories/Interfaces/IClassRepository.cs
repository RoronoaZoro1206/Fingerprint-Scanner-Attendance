using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IClassRepository
{
    Task<IEnumerable<Class>> GetAllAsync();
    Task<Class?> GetByIdAsync(int classId);
    Task<bool> AddAsync(Class cls);
    Task<bool> UpdateAsync(Class cls);
    Task<bool> DeleteAsync(int classId);
    Task<bool> ExistsAsync(int classId);
}