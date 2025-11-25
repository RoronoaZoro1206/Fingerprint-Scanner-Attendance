using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IClassScheduleRepository
{
    Task<IEnumerable<ClassSchedule>> GetAllAsync();
    Task<ClassSchedule?> GetByIdAsync(int schedId);
    Task<IEnumerable<ClassSchedule>> GetByClassIdAsync(int classId);
    Task<bool> AddAsync(ClassSchedule sched);
    Task<bool> UpdateAsync(ClassSchedule sched);
    Task<bool> DeleteAsync(int schedId);
    Task<bool> ExistsAsync(int schedId);
    
    // Optimized methods to reduce N+1 queries
    Task<IEnumerable<ClassScheduleDetailDto>> GetAllWithDetailsAsync();
    
    // ✅ NEW: Get only classes without schedules (TBA)
    Task<IEnumerable<AvailableClassDto>> GetUnscheduledClassesAsync();
    
    // 📅 NEW: Schedule integration methods for Phase 4
    Task<IEnumerable<StudentScheduleDto>> GetStudentScheduleForDateAsync(int studId, DateTime date);
    Task<IEnumerable<StudentScheduleDto>> GetStudentScheduleForDayAsync(int studId, string dayOfWeek);
    
    // ✅ NEW: Faculty schedule conflict validation (returns faculty name too)
    Task<(bool HasConflict, string ConflictingCourseName, string FacultyName)> CheckFacultyScheduleConflictAsync(
        int classId, 
        string dayOfWeek, 
        TimeSpan startTime, 
        TimeSpan endTime, 
        int? excludeSchedId = null);
}
