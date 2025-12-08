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
