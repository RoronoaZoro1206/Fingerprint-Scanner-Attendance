using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IStudentIdPoolRepository
{
    Task<IEnumerable<StudentIdPool>> GetAllAsync();
    Task<StudentIdPool?> GetByIdAsync(int poolId);
    Task<StudentIdPool?> GetByCodeAsync(string studentIdCode);
    Task<bool> AddAsync(StudentIdPool pool);
    Task<bool> MarkAsUsedAsync(int poolId);
    Task<bool> UpdateRemarksAsync(int poolId, string remarks);
    Task<bool> DeleteAsync(int poolId);
    Task<bool> ExistsAsync(string studentIdCode);
}