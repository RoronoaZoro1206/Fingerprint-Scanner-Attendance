using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface ISectionRepository
    {
        Task<IEnumerable<Section>> GetAllAsync();
        Task<Section?> GetByIdAsync(int sectionId);
        Task<Section?> GetByNameAsync(string sectionName);
        Task<bool> AddAsync(Section section);
        Task<bool> UpdateAsync(Section section);
        Task<bool> DeleteAsync(int sectionId);
        Task<bool> ExistsAsync(int sectionId);
        
        // Optimized DTO methods
        Task<IEnumerable<SectionListDto>> GetAllWithDetailsAsync();
        Task<SectionDetailDto?> GetDetailByIdAsync(int sectionId);
    }
}