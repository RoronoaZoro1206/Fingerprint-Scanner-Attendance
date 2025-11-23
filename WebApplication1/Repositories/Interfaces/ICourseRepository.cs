using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync();
    Task<Course?> GetByIdAsync(int courseId);
    Task<bool> AddAsync(Course course);
    Task<bool> UpdateAsync(Course course);
    Task<bool> DeleteAsync(int courseId);
    
    // Optimized methods to reduce N+1 queries
    Task<IEnumerable<CourseDetailDto>> GetAllWithProgramAsync();
    
    // ✅ NEW: Duplicate validation methods
    Task<Course?> GetByCourseCodeAsync(string courseCode);
    Task<Course?> GetByCourseNameAsync(string courseName);
    Task<bool> IsCourseCodeDuplicateAsync(string courseCode, int? excludeCourseId = null);
    Task<bool> IsCourseNameDuplicateAsync(string courseName, int? excludeCourseId = null);
}
