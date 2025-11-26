using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class EnrollmentBlockRepository : IEnrollmentBlockRepository
    {
        private readonly DatabaseHelper _db;

        public EnrollmentBlockRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<bool> AddBlockAsync(EnrollmentBlock block)
        {
            // Use upsert to handle case where block already exists but was deactivated
            string query = @"
                INSERT INTO enrollment_blocks (stud_id, class_id, blocked_by_fac_id, block_reason, blocked_at, is_active)
                VALUES (@studId, @classId, @facId, @reason, @blockedAt, TRUE)
                ON CONFLICT (stud_id, class_id) 
                DO UPDATE SET 
                    blocked_by_fac_id = @facId,
                    block_reason = @reason,
                    blocked_at = @blockedAt,
                    is_active = TRUE,
                    unblocked_at = NULL,
                    unblocked_by = NULL;";

            var parameters = new Dictionary<string, object>
            {
                { "@studId", block.StudId },
                { "@classId", block.ClassId },
                { "@facId", block.BlockedByFacId },
                { "@reason", block.BlockReason ?? (object)DBNull.Value },
                { "@blockedAt", block.BlockedAt }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> IsStudentBlockedAsync(int studId, int classId)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM enrollment_blocks 
                WHERE stud_id = @studId 
                  AND class_id = @classId 
                  AND is_active = TRUE;";

            var parameters = new Dictionary<string, object>
            {
                { "@studId", studId },
                { "@classId", classId }
            };

            return await _db.RecordExistsAsync(query, parameters);
        }

        public async Task<IEnumerable<BlockedClassDto>> GetStudentBlockedClassesAsync(int studId)
        {
            string query = @"
                SELECT 
                    eb.block_id,
                    eb.class_id,
                    COALESCE(co.course_code, 'UNKNOWN') as course_code,
                    COALESCE(co.course_name, 'Unknown Course') as course_name,
                    COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'N/A') as faculty_name,
                    COALESCE(r.room_code, 'TBA') as room_code,
                    COALESCE(
                        STRING_AGG(
                            cs.sched_day_of_week || ' ' || 
                            TO_CHAR(cs.sched_start_time, 'HH12:MI AM') || '-' || 
                            TO_CHAR(cs.sched_end_time, 'HH12:MI AM'), 
                            ', ' ORDER BY 
                                CASE cs.sched_day_of_week
                                    WHEN 'Monday' THEN 1
                                    WHEN 'Tuesday' THEN 2
                                    WHEN 'Wednesday' THEN 3
                                    WHEN 'Thursday' THEN 4
                                    WHEN 'Friday' THEN 5
                                    WHEN 'Saturday' THEN 6
                                    WHEN 'Sunday' THEN 7
                                END,
                                cs.sched_start_time
                        ),
                        'TBA'
                    ) as schedule,
                    COALESCE(eb.block_reason, 'No reason provided') as block_reason,
                    eb.blocked_at
                FROM enrollment_blocks eb
                INNER JOIN classes cl ON eb.class_id = cl.class_id
                LEFT JOIN courses co ON cl.course_id = co.course_id
                LEFT JOIN rooms r ON cl.room_id = r.room_id
                LEFT JOIN faculty f ON cl.fac_id = f.fac_id
                LEFT JOIN users u ON f.user_id = u.user_id
                LEFT JOIN class_schedules cs ON cl.class_id = cs.class_id
                WHERE eb.stud_id = @studId AND eb.is_active = TRUE
                GROUP BY eb.block_id, eb.class_id, co.course_code, co.course_name,
                         u.user_first_name, u.user_last_name, r.room_code, 
                         eb.block_reason, eb.blocked_at
                ORDER BY eb.blocked_at DESC;";

            var parameters = new Dictionary<string, object> { { "@studId", studId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new BlockedClassDto
            {
                BlockId = Convert.ToInt32(row["block_id"]),
                ClassId = Convert.ToInt32(row["class_id"]),
                CourseCode = row["course_code"]?.ToString() ?? "UNKNOWN",
                CourseName = row["course_name"]?.ToString() ?? "Unknown Course",
                FacultyName = row["faculty_name"]?.ToString() ?? "N/A",
                RoomCode = row["room_code"]?.ToString() ?? "TBA",
                Schedule = row["schedule"]?.ToString() ?? "TBA",
                BlockReason = row["block_reason"]?.ToString() ?? "No reason provided",
                BlockedAt = Convert.ToDateTime(row["blocked_at"])
            });
        }

        public async Task<IEnumerable<int>> GetBlockedClassIdsForStudentAsync(int studId)
        {
            string query = @"
                SELECT class_id 
                FROM enrollment_blocks 
                WHERE stud_id = @studId AND is_active = TRUE;";

            var parameters = new Dictionary<string, object> { { "@studId", studId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => Convert.ToInt32(row["class_id"]));
        }

        public async Task<bool> RemoveBlockAsync(int blockId, int unblockedByUserId)
        {
            string query = @"
                UPDATE enrollment_blocks 
                SET is_active = FALSE, 
                    unblocked_at = NOW(), 
                    unblocked_by = @unblockedBy
                WHERE block_id = @blockId;";

            var parameters = new Dictionary<string, object>
            {
                { "@blockId", blockId },
                { "@unblockedBy", unblockedByUserId }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<EnrollmentBlock?> GetByIdAsync(int blockId)
        {
            string query = @"
                SELECT block_id, stud_id, class_id, blocked_by_fac_id, 
                       block_reason, blocked_at, is_active, unblocked_at, unblocked_by
                FROM enrollment_blocks 
                WHERE block_id = @blockId;";

            var parameters = new Dictionary<string, object> { { "@blockId", blockId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new EnrollmentBlock
            {
                BlockId = Convert.ToInt32(row["block_id"]),
                StudId = Convert.ToInt32(row["stud_id"]),
                ClassId = Convert.ToInt32(row["class_id"]),
                BlockedByFacId = Convert.ToInt32(row["blocked_by_fac_id"]),
                BlockReason = row["block_reason"]?.ToString(),
                BlockedAt = Convert.ToDateTime(row["blocked_at"]),
                IsActive = Convert.ToBoolean(row["is_active"]),
                UnblockedAt = row["unblocked_at"] == DBNull.Value ? null : Convert.ToDateTime(row["unblocked_at"]),
                UnblockedBy = row["unblocked_by"] == DBNull.Value ? null : Convert.ToInt32(row["unblocked_by"])
            };
        }

        public async Task<IEnumerable<EnrollmentBlock>> GetAllAsync()
        {
            string query = @"
                SELECT block_id, stud_id, class_id, blocked_by_fac_id, 
                       block_reason, blocked_at, is_active, unblocked_at, unblocked_by
                FROM enrollment_blocks 
                ORDER BY blocked_at DESC;";

            var dt = await _db.ExecuteQueryAsync(query, new Dictionary<string, object>());

            return dt.AsEnumerable().Select(row => new EnrollmentBlock
            {
                BlockId = Convert.ToInt32(row["block_id"]),
                StudId = Convert.ToInt32(row["stud_id"]),
                ClassId = Convert.ToInt32(row["class_id"]),
                BlockedByFacId = Convert.ToInt32(row["blocked_by_fac_id"]),
                BlockReason = row["block_reason"]?.ToString(),
                BlockedAt = Convert.ToDateTime(row["blocked_at"]),
                IsActive = Convert.ToBoolean(row["is_active"]),
                UnblockedAt = row["unblocked_at"] == DBNull.Value ? null : Convert.ToDateTime(row["unblocked_at"]),
                UnblockedBy = row["unblocked_by"] == DBNull.Value ? null : Convert.ToInt32(row["unblocked_by"])
            });
        }
    }
}
