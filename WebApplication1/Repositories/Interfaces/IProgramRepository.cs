using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;
public interface IProgramRepository
{
    Task<IEnumerable<ProgramModel>> GetAllAsync();
    Task<ProgramModel?> GetByIdAsync(int programId);
    Task<bool> AddAsync(ProgramModel program);
    Task<bool> UpdateAsync(ProgramModel program);
    Task<bool> DeleteAsync(int programId);
}