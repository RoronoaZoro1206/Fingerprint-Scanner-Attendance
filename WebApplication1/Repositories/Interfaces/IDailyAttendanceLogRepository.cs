using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IDailyAttendanceLogRepository
    {
        /// <summary>
        /// Get daily attendance record by student ID and date
        /// </summary>
        Task<DailyAttendanceLog?> GetByStudentAndDateAsync(int studId, DateTime date);

        /// <summary>
        /// Get daily attendance record by ID
        /// </summary>
        Task<DailyAttendanceLog?> GetByIdAsync(int dailyAttendId);

        /// <summary>
        /// Get all daily attendance records for a specific date
        /// </summary>
        Task<IEnumerable<DailyAttendanceLog>> GetByDateAsync(DateTime date);

        /// <summary>
        /// Get all incomplete records (forgot to time out)
        /// </summary>
        Task<IEnumerable<DailyAttendanceLog>> GetIncompleteRecordsAsync();

        /// <summary>
        /// Add a new daily attendance record
        /// </summary>
        Task<DailyAttendanceLog> AddAsync(DailyAttendanceLog record);

        /// <summary>
        /// Update an existing daily attendance record
        /// </summary>
        Task UpdateAsync(DailyAttendanceLog record);

        /// <summary>
        /// Save changes (for consistency with EF Core pattern)
        /// </summary>
        Task SaveChangesAsync();
    }
}
