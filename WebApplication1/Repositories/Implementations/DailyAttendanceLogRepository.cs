using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class DailyAttendanceLogRepository : IDailyAttendanceLogRepository
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<DailyAttendanceLogRepository> _logger;

        public DailyAttendanceLogRepository(
            DatabaseHelper db,
            ILogger<DailyAttendanceLogRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<DailyAttendanceLog?> GetByStudentAndDateAsync(int studId, DateTime date)
        {
            // ✅ CRITICAL FIX: Added class_id to SELECT statement
            var query = @"
                SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                       status, forgot_timeout, remarks, created_at, updated_at
                FROM daily_attendance_log
                WHERE stud_id = @stud_id 
                  AND attend_date = @attend_date";

            var parameters = new Dictionary<string, object>
            {
                { "@stud_id", studId },
                { "@attend_date", date.Date }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return MapFromDataRow(row);
        }

        /// <summary>
        /// ✅ NEW: Get attendance record by student, class, and date
        /// Used to check if attendance already completed for specific class
        /// </summary>
        public async Task<DailyAttendanceLog?> GetByStudentClassAndDateAsync(int studId, int classId, DateTime date)
        {
            var query = @"
                SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                       status, forgot_timeout, remarks, created_at, updated_at
                FROM daily_attendance_log
                WHERE stud_id = @stud_id 
                  AND class_id = @class_id
                  AND attend_date = @attend_date";

            var parameters = new Dictionary<string, object>
            {
                { "@stud_id", studId },
                { "@class_id", classId },
                { "@attend_date", date.Date }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return MapFromDataRow(row);
        }

        /// <summary>
        /// ✅ NEW: Get ACTIVE (non-invalidated) attendance record by student, class, and date
        /// Used for attendance validation - ignores invalidated records from schedule changes
        /// ✅ FIX: Handles case where is_invalidated column may not exist in database
        /// </summary>
        public async Task<DailyAttendanceLog?> GetActiveByStudentClassAndDateAsync(int studId, int classId, DateTime date)
        {
            try
            {
                // Try query with is_invalidated column first
                var query = @"
                    SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                           status, forgot_timeout, remarks, created_at, updated_at
                    FROM daily_attendance_log
                    WHERE stud_id = @stud_id 
                      AND class_id = @class_id
                      AND attend_date = @attend_date
                      AND COALESCE(is_invalidated, FALSE) = FALSE";

                var parameters = new Dictionary<string, object>
                {
                    { "@stud_id", studId },
                    { "@class_id", classId },
                    { "@attend_date", date.Date }
                };

                var result = await _db.ExecuteQueryAsync(query, parameters);

                if (result.Rows.Count == 0)
                    return null;

                var row = result.Rows[0];
                return MapFromDataRow(row);
            }
            catch (Exception ex)
            {
                // If is_invalidated column doesn't exist, fall back to simple query
                _logger.LogWarning(ex, "GetActiveByStudentClassAndDateAsync failed with is_invalidated column, falling back to simple query");
                
                var fallbackQuery = @"
                    SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                           status, forgot_timeout, remarks, created_at, updated_at
                    FROM daily_attendance_log
                    WHERE stud_id = @stud_id 
                      AND class_id = @class_id
                      AND attend_date = @attend_date";

                var parameters = new Dictionary<string, object>
                {
                    { "@stud_id", studId },
                    { "@class_id", classId },
                    { "@attend_date", date.Date }
                };

                var result = await _db.ExecuteQueryAsync(fallbackQuery, parameters);

                if (result.Rows.Count == 0)
                    return null;

                var row = result.Rows[0];
                return MapFromDataRow(row);
            }
        }

        public async Task<DailyAttendanceLog?> GetByIdAsync(int dailyAttendId)
        {
            var query = @"
                SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                       status, forgot_timeout, remarks, created_at, updated_at
                FROM daily_attendance_log
                WHERE daily_attend_id = @daily_attend_id";

            var parameters = new Dictionary<string, object>
            {
                { "@daily_attend_id", dailyAttendId }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return MapFromDataRow(row);
        }

        public async Task<IEnumerable<DailyAttendanceLog>> GetByDateAsync(DateTime date)
        {
            var query = @"
                SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                       status, forgot_timeout, remarks, created_at, updated_at
                FROM daily_attendance_log
                WHERE attend_date = @attend_date
                ORDER BY time_in";

            var parameters = new Dictionary<string, object>
            {
                { "@attend_date", date.Date }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);
            var records = new List<DailyAttendanceLog>();

            foreach (DataRow row in result.Rows)
            {
                records.Add(MapFromDataRow(row));
            }

            return records;
        }

        public async Task<IEnumerable<DailyAttendanceLog>> GetIncompleteRecordsAsync()
        {
            var query = @"
                SELECT daily_attend_id, stud_id, class_id, attend_date, time_in, time_out, 
                       status, forgot_timeout, remarks, created_at, updated_at
                FROM daily_attendance_log
                WHERE attend_date < CURRENT_DATE 
                  AND time_out IS NULL 
                  AND forgot_timeout = FALSE
                ORDER BY attend_date DESC, stud_id";

            var result = await _db.ExecuteQueryAsync(query);
            var records = new List<DailyAttendanceLog>();

            foreach (DataRow row in result.Rows)
            {
                records.Add(MapFromDataRow(row));
            }

            return records;
        }

        public async Task<DailyAttendanceLog> AddAsync(DailyAttendanceLog record)
        {
            var query = @"
                INSERT INTO daily_attendance_log 
                    (stud_id, class_id, attend_date, time_in, time_out, status, forgot_timeout, remarks, created_at, updated_at)
                VALUES 
                    (@stud_id, @class_id, @attend_date, @time_in, @time_out, @status, @forgot_timeout, @remarks, @created_at, @updated_at)
                RETURNING daily_attend_id";

            var parameters = new Dictionary<string, object>
            {
                { "@stud_id", record.StudId },
                { "@class_id", record.ClassId ?? (object)DBNull.Value },
                { "@attend_date", record.AttendDate.Date },
                { "@time_in", record.TimeIn ?? (object)DBNull.Value },
                { "@time_out", record.TimeOut ?? (object)DBNull.Value },
                { "@status", record.Status },
                { "@forgot_timeout", record.ForgotTimeout },
                { "@remarks", record.Remarks ?? (object)DBNull.Value },
                { "@created_at", record.CreatedAt },
                { "@updated_at", record.UpdatedAt }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count > 0)
            {
                record.DailyAttendId = Convert.ToInt32(result.Rows[0][0]);
                _logger.LogInformation($"Daily attendance record created: ID {record.DailyAttendId}");
            }

            return record;
        }

        public async Task UpdateAsync(DailyAttendanceLog record)
        {
            var query = @"
                UPDATE daily_attendance_log
                SET time_out = @time_out,
                    status = @status,
                    forgot_timeout = @forgot_timeout,
                    remarks = @remarks,
                    updated_at = @updated_at
                WHERE daily_attend_id = @daily_attend_id";

            var parameters = new Dictionary<string, object>
            {
                { "@daily_attend_id", record.DailyAttendId },
                { "@time_out", record.TimeOut ?? (object)DBNull.Value },
                { "@status", record.Status },
                { "@forgot_timeout", record.ForgotTimeout },
                { "@remarks", record.Remarks ?? (object)DBNull.Value },
                { "@updated_at", record.UpdatedAt }
            };

            await _db.ExecuteNonQueryAsync(query, parameters);
            _logger.LogInformation($"Daily attendance record updated: ID {record.DailyAttendId}");
        }

        /// <summary>
        /// ✅ NEW: Delete a daily attendance log record by ID
        /// Enables faculty to completely remove attendance records
        /// Allows students to re-scan during active class time
        /// </summary>
        public async Task<bool> DeleteAsync(int dailyAttendId)
        {
            var query = @"
                DELETE FROM daily_attendance_log
                WHERE daily_attend_id = @daily_attend_id";

            var parameters = new Dictionary<string, object>
            {
                { "@daily_attend_id", dailyAttendId }
            };

            var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation($"Daily attendance record deleted: ID {dailyAttendId}");
            }
            
            return rowsAffected > 0;
        }

        public async Task SaveChangesAsync()
        {
            // DatabaseHelper executes changes immediately, so this is a no-op
            // Kept for interface consistency with EF Core
            await Task.CompletedTask;
        }

        /// <summary>
        /// ✅ NEW: Invalidate attendance records when schedule time changes
        /// Marks old attendance as invalidated so students can re-attend for new time slot
        /// ✅ FIX: Handles case where is_invalidated column may not exist
        /// </summary>
        public async Task<(int DailyCount, int AttendanceCount, string Details)> InvalidateAttendanceOnScheduleChangeAsync(
            int classId,
            int schedId,
            TimeSpan oldStartTime,
            TimeSpan oldEndTime,
            TimeSpan newStartTime,
            TimeSpan newEndTime,
            string dayOfWeek)
        {
            try
            {
                _logger.LogInformation($"Invalidating attendance for class {classId} due to schedule change: " +
                    $"{oldStartTime:hh\\:mm}-{oldEndTime:hh\\:mm} → {newStartTime:hh\\:mm}-{newEndTime:hh\\:mm} on {dayOfWeek}");

                // Build invalidation reason
                string reason = $"Schedule changed from {oldStartTime:hh\\:mm}-{oldEndTime:hh\\:mm} to " +
                               $"{newStartTime:hh\\:mm}-{newEndTime:hh\\:mm} on {dayOfWeek}";

                var today = DateTime.Today;
                int dailyCount = 0;
                int attendanceCount = 0;

                // ✅ STEP 1: Try to invalidate daily_attendance_log records for TODAY
                try
                {
                    var dailyQuery = @"
                        UPDATE daily_attendance_log
                        SET is_invalidated = TRUE,
                            invalidation_reason = @reason,
                            invalidated_at = NOW(),
                            remarks = CONCAT(COALESCE(remarks, ''), ' | INVALIDATED: ', @reason),
                            updated_at = NOW()
                        WHERE class_id = @class_id
                          AND attend_date = @today
                          AND COALESCE(is_invalidated, FALSE) = FALSE
                          AND status IN ('active', 'completed')";

                    var dailyParams = new Dictionary<string, object>
                    {
                        { "@class_id", classId },
                        { "@today", today },
                        { "@reason", reason }
                    };

                    dailyCount = await _db.ExecuteNonQueryAsync(dailyQuery, dailyParams);
                    _logger.LogInformation($"✅ Invalidated {dailyCount} daily_attendance_log records");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate daily_attendance_log (is_invalidated column may not exist). Trying alternative approach.");
                    
                    // Fallback: Delete today's records instead of invalidating
                    // This allows students to time in again
                    try
                    {
                        var deleteQuery = @"
                            DELETE FROM daily_attendance_log
                            WHERE class_id = @class_id
                              AND attend_date = @today
                              AND status IN ('active', 'completed')";

                        var deleteParams = new Dictionary<string, object>
                        {
                            { "@class_id", classId },
                            { "@today", today }
                        };

                        dailyCount = await _db.ExecuteNonQueryAsync(deleteQuery, deleteParams);
                        _logger.LogInformation($"✅ Deleted {dailyCount} daily_attendance_log records (fallback)");
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to delete daily_attendance_log records");
                    }
                }

                // ✅ STEP 2: Try to invalidate corresponding attendance records for TODAY
                try
                {
                    var attendanceQuery = @"
                        UPDATE attendance
                        SET is_invalidated = TRUE,
                            invalidation_reason = @reason,
                            attend_remark = CONCAT(COALESCE(attend_remark, ''), ' | INVALIDATED: ', @reason)
                        WHERE class_id = @class_id
                          AND attend_date = @today
                          AND COALESCE(is_invalidated, FALSE) = FALSE";

                    var attendanceParams = new Dictionary<string, object>
                    {
                        { "@class_id", classId },
                        { "@today", today },
                        { "@reason", reason }
                    };

                    attendanceCount = await _db.ExecuteNonQueryAsync(attendanceQuery, attendanceParams);
                    _logger.LogInformation($"✅ Invalidated {attendanceCount} attendance records");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate attendance records (is_invalidated column may not exist). Trying alternative approach.");
                    
                    // Fallback: Delete today's attendance records
                    try
                    {
                        var deleteQuery = @"
                            DELETE FROM attendance
                            WHERE class_id = @class_id
                              AND attend_date = @today";

                        var deleteParams = new Dictionary<string, object>
                        {
                            { "@class_id", classId },
                            { "@today", today }
                        };

                        attendanceCount = await _db.ExecuteNonQueryAsync(deleteQuery, deleteParams);
                        _logger.LogInformation($"✅ Deleted {attendanceCount} attendance records (fallback)");
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to delete attendance records");
                    }
                }

                string details = $"Invalidated {dailyCount} daily + {attendanceCount} attendance records for class {classId}. Reason: {reason}";
                
                _logger.LogInformation($"✅ {details}");

                return (dailyCount, attendanceCount, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invalidating attendance for class {classId}");
                throw;
            }
        }

        private DailyAttendanceLog MapFromDataRow(DataRow row)
        {
            return new DailyAttendanceLog
            {
                DailyAttendId = Convert.ToInt32(row["daily_attend_id"]),
                StudId = Convert.ToInt32(row["stud_id"]),
                ClassId = row["class_id"] == DBNull.Value ? null : Convert.ToInt32(row["class_id"]),
                AttendDate = Convert.ToDateTime(row["attend_date"]),
                TimeIn = row["time_in"] == DBNull.Value ? null : Convert.ToDateTime(row["time_in"]),
                TimeOut = row["time_out"] == DBNull.Value ? null : Convert.ToDateTime(row["time_out"]),
                Status = row["status"].ToString() ?? "active",
                ForgotTimeout = Convert.ToBoolean(row["forgot_timeout"]),
                Remarks = row["remarks"] == DBNull.Value ? null : row["remarks"].ToString(),
                CreatedAt = Convert.ToDateTime(row["created_at"]),
                UpdatedAt = Convert.ToDateTime(row["updated_at"])
            };
        }
    }
}
