using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IScanLogRepository
{
    Task<IEnumerable<ScanLog>> GetAllAsync();
    Task<ScanLog?> GetByIdAsync(int scanId);
    Task<bool> AddAsync(ScanLog scanLog);
    Task<bool> UpdateAsync(ScanLog scanLog);
    Task<bool> DeleteAsync(int scanId);
    Task<bool> ExistsAsync(int scanId);
}