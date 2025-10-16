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
}