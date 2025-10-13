using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IEnrollmentRepository
{
    Task<IEnumerable<Enrollment>> GetAllAsync();
    Task<Enrollment?> GetByIdAsync(int enrollId);
    Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId);
    Task<bool> AddAsync(Enrollment enroll);
    Task<bool> UpdateAsync(Enrollment enroll);
    Task<bool> DeleteAsync(int enrollId);
    Task<bool> ExistsAsync(int enrollId);
}