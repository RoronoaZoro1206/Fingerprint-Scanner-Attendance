using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IFingerprintTemplateRepository
    {
        /// <summary>
        /// Check if a user has a registered fingerprint
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if fingerprint exists, false otherwise</returns>
        Task<bool> HasFingerprintAsync(int userId);

        /// <summary>
        /// Get fingerprint template by user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>FingerprintTemplate or null if not found</returns>
        Task<FingerprintTemplate?> GetByUserIdAsync(int userId);

        /// <summary>
        /// Get fingerprint template by template ID
        /// </summary>
        /// <param name="ftempId">The template ID</param>
        /// <returns>FingerprintTemplate or null if not found</returns>
        Task<FingerprintTemplate?> GetByIdAsync(int ftempId);

        /// <summary>
        /// Create a new fingerprint template
        /// </summary>
        /// <param name="template">The fingerprint template to create</param>
        /// <returns>The created template ID</returns>
        Task<int> CreateAsync(FingerprintTemplate template);

        /// <summary>
        /// Update an existing fingerprint template
        /// </summary>
        /// <param name="template">The fingerprint template to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateAsync(FingerprintTemplate template);

        /// <summary>
        /// Delete a fingerprint template by user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteByUserIdAsync(int userId);

        /// <summary>
        /// Get all fingerprint templates
        /// </summary>
        /// <returns>List of all fingerprint templates</returns>
        Task<IEnumerable<FingerprintTemplate>> GetAllAsync();

        /// <summary>
        /// Get user by fingerprint template's user ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>User or null if not found</returns>
        Task<User?> GetUserByTemplateAsync(int userId);
    }
}
