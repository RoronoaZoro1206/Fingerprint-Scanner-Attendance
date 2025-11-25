using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IEnrollmentRepository
{
    Task<IEnumerable<Enrollment>> GetAllAsync();
    Task<Enrollment?> GetByIdAsync(int enrollId);
    Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId);
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(int studId);
    Task<bool> AddAsync(Enrollment enroll);
    Task<bool> UpdateAsync(Enrollment enroll);
    Task<bool> DeleteAsync(int enrollId);
    Task<bool> ExistsAsync(int enrollId);
    
    // Optimized methods to reduce N+1 queries
    Task<IEnumerable<EnrollmentDetailDto>> GetStudentEnrollmentsWithDetailsAsync(int studId);
    Task<IEnumerable<AvailableClassDto>> GetAvailableClassesForStudentAsync(int studId, int programId);
    Task<IEnumerable<StudentClassDto>> GetStudentClassesAsync(int studId);
    
    // ✅ NEW: Get student enrollments with schedule details for conflict detection
    Task<IEnumerable<StudentEnrollmentScheduleDto>> GetStudentEnrollmentsWithSchedulesAsync(int studId);
}
