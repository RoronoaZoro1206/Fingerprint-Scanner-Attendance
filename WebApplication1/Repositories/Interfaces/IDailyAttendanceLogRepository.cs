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
        /// ✅ NEW: Get attendance record by student, class, and date
        /// Used to check if attendance already completed for specific class
        /// </summary>
        Task<DailyAttendanceLog?> GetByStudentClassAndDateAsync(int studId, int classId, DateTime date);

        /// <summary>
        /// ✅ NEW: Get ACTIVE (non-invalidated) attendance record by student, class, and date
        /// Used for attendance validation - ignores invalidated records from schedule changes
        /// </summary>
        Task<DailyAttendanceLog?> GetActiveByStudentClassAndDateAsync(int studId, int classId, DateTime date);

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
        /// ✅ NEW: Delete a daily attendance log record by ID
        /// Required for faculty to completely remove student attendance
        /// Enables re-scanning during active class time
        /// </summary>
        Task<bool> DeleteAsync(int dailyAttendId);

        /// <summary>
        /// Save changes (for consistency with EF Core pattern)
        /// </summary>
        Task SaveChangesAsync();

        /// <summary>
        /// ✅ NEW: Invalidate attendance records when schedule time changes
        /// Marks old attendance as invalidated so students can re-attend for new time slot
        /// </summary>
        /// <param name="classId">Class ID</param>
        /// <param name="schedId">Schedule ID that was changed</param>
        /// <param name="oldStartTime">Original start time</param>
        /// <param name="oldEndTime">Original end time</param>
        /// <param name="newStartTime">New start time</param>
        /// <param name="newEndTime">New end time</param>
        /// <param name="dayOfWeek">Day of week</param>
        /// <returns>Tuple with counts of invalidated records</returns>
        Task<(int DailyCount, int AttendanceCount, string Details)> InvalidateAttendanceOnScheduleChangeAsync(
            int classId,
            int schedId,
            TimeSpan oldStartTime,
            TimeSpan oldEndTime,
            TimeSpan newStartTime,
            TimeSpan newEndTime,
            string dayOfWeek);
    }
}
