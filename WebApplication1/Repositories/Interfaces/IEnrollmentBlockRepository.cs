using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IEnrollmentBlockRepository
    {
        /// <summary>
        /// Add a new enrollment block (when faculty force-unenrolls a student)
        /// </summary>
        Task<bool> AddBlockAsync(EnrollmentBlock block);
        
        /// <summary>
        /// Check if a student is blocked from a specific class
        /// </summary>
        Task<bool> IsStudentBlockedAsync(int studId, int classId);
        
        /// <summary>
        /// Get all active blocks for a student
        /// </summary>
        Task<IEnumerable<BlockedClassDto>> GetStudentBlockedClassesAsync(int studId);
        
        /// <summary>
        /// Get all blocked class IDs for a student (for filtering available classes)
        /// </summary>
        Task<IEnumerable<int>> GetBlockedClassIdsForStudentAsync(int studId);
        
        /// <summary>
        /// Remove a block (admin only - lift the restriction)
        /// </summary>
        Task<bool> RemoveBlockAsync(int blockId, int unblockedByUserId);
        
        /// <summary>
        /// Get block details by ID
        /// </summary>
        Task<EnrollmentBlock?> GetByIdAsync(int blockId);
        
        /// <summary>
        /// Get all enrollment blocks (for admin management)
        /// </summary>
        Task<IEnumerable<EnrollmentBlock>> GetAllAsync();
    }
}
