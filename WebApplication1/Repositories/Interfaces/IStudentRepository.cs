using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(int studId);
    Task<bool> AddAsync(Student student);
    Task<bool> UpdateAsync(Student student);
    Task<bool> DeleteAsync(int studId);
}