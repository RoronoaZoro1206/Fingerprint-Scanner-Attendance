using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IStudentIdPoolRepository
{
    Task<IEnumerable<StudentIdPool>> GetAllAsync();
    Task<IEnumerable<StudentIdPool>> GetAvailableAsync();
    Task<StudentIdPool?> GetByIdAsync(int poolId);
    Task<StudentIdPool?> GetByCodeAsync(string studentIdCode);
    Task<bool> AddAsync(StudentIdPool pool);
    Task<bool> MarkAsUsedAsync(int poolId);
    Task<bool> ReleaseStudentIdAsync(int poolId, string releaseReason); // ✅ NEW: Release student ID back to pool
    Task<bool> UpdateRemarksAsync(int poolId, string remarks);
    Task<bool> DeleteAsync(int poolId);
    Task<bool> ExistsAsync(string studentIdCode);
}
