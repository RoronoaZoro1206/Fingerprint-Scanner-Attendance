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
    Task<IEnumerable<FacultyClassDto>> GetFacultyClassesAsync(int facId);
    
    // ✅ NEW: Get class schedules for conflict detection
    Task<IEnumerable<ClassScheduleDto>> GetClassSchedulesAsync(int classId);
    
    // ✅ NEW: Check for duplicate class (same course + faculty + term)
    Task<Class?> GetDuplicateClassAsync(int? courseId, int? facId, int? termId, int? excludeClassId = null);
    
    // ✅ NEW: Get enrolled students for a class (for faculty student management)
    Task<IEnumerable<EnrolledStudentDto>> GetEnrolledStudentsForClassAsync(int classId);
}
