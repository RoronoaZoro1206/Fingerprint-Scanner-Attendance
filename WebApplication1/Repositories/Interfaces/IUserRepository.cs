using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> AddAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(int userId);
    Task<bool> ExistsAsync(int userId);
    Task<bool> AdminExistsAsync();
    Task<User?> GetByEmailWithRoleCheckAsync(string email, int roleId);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<UserListDto>> GetUsersWithRolesAsync();
    Task<bool> UpdateLastLoginAsync(int userId);
}