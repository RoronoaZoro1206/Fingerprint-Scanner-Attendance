using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IUserRoleRepository
{
    Task<IEnumerable<UserRole>> GetAllAsync();
    Task<UserRole?> GetByIdAsync(int userId);
}