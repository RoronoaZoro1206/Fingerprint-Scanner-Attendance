using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IAttendanceRepository
{
    Task<IEnumerable<Attendance>> GetAllAsync();
    Task<Attendance?> GetByIdAsync(int attendId);
    Task<bool> AddAsync(Attendance attendance);
    Task<bool> UpdateAsync(Attendance attendance);
    Task<bool> DeleteAsync(int attendId);
    Task<bool> ExistsAsync(int attendId);
    
    // Optimized methods to reduce N+1 queries
    Task<ClassAttendanceDto?> GetClassAttendanceWithDetailsAsync(int classId, DateTime date, int facId);
}