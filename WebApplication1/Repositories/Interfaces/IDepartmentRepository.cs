using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<Department>> GetAllAsync();
        Task<Department?> GetByIdAsync(int deptId);
        Task<Department?> GetByCodeAsync(string deptCode);
        Task<bool> AddAsync(Department dept);
        Task<bool> UpdateAsync(Department dept);
        Task<bool> DeleteAsync(int deptId);
        Task<bool> ExistsAsync(int deptId);
    }
}