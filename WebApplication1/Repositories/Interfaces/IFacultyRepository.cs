using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;

public interface IFacultyRepository
{
    Task<IEnumerable<Faculty>> GetAllAsync();
    Task<Faculty?> GetByIdAsync(int facId);
    Task<bool> AddAsync(Faculty faculty);
    Task<bool> UpdateAsync(Faculty faculty);
    Task<bool> DeleteAsync(int facId);
}