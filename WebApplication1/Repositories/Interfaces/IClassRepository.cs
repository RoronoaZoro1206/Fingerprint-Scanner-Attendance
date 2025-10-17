using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IClassRepository
{
    Task<IEnumerable<Class>> GetAllAsync();
    Task<IEnumerable<Class>> GetAllWithDetailsAsync();
    Task<IEnumerable<Class>> GetClassesByFacultyIdAsync(int facId);
    Task<Class?> GetByIdAsync(int classId);
    Task<bool> AddAsync(Class cls);
    Task<bool> UpdateAsync(Class cls);
    Task<bool> DeleteAsync(int classId);
    Task<bool> ExistsAsync(int classId);
    
    // Optimized DTO methods
    Task<IEnumerable<ClassListDto>> GetAllClassListAsync();
    Task<ClassDetailDto?> GetClassDetailByIdAsync(int classId);
}