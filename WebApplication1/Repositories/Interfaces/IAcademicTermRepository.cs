using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IAcademicTermRepository
    {
        Task<IEnumerable<AcademicTerm>> GetAllAsync();
        Task<AcademicTerm?> GetByIdAsync(int termId);
        Task<AcademicTerm?> GetByNameAsync(string termName);
        Task<AcademicTerm?> GetActiveTermAsync();
        Task<bool> AddAsync(AcademicTerm term);
        Task<bool> UpdateAsync(AcademicTerm term);
        Task<bool> DeleteAsync(int termId);
        Task<bool> SetActiveTermAsync(int termId);
        Task<bool> ExistsAsync(int termId);
    }
}