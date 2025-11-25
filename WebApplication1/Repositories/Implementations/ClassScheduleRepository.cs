using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class ClassScheduleRepository : IClassScheduleRepository
{
    private readonly DatabaseHelper _db;

    public ClassScheduleRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ClassSchedule>> GetAllAsync()
    {
        string query = @"SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time 
                         FROM class_schedules;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_start_time"],
            SchedEndTime = (TimeSpan)row["sched_end_time"]
        });
    }

    public async Task<ClassSchedule?> GetByIdAsync(int schedId)
    {
        string query = @"SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time 
                         FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ClassSchedule
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            SchedDayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            SchedStartTime = (TimeSpan)row["sched_start_time"],
            SchedEndTime = (TimeSpan)row["sched_end_time"]
        };
    }

    public async Task<IEnumerable<ClassSchedule>> GetByClassIdAsync(int classId)
    {
        string query = @"SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time 
                         FROM class_schedules WHERE class_id = @classId 
                         ORDER BY sched_day_of_week, sched_start_time;";
        var parameters = new Dictionary<string, object> { { "@classId", classId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        // ✅ CRITICAL DEBUG: Log row count to verify data retrieval
        Console.WriteLine($"[REPO DEBUG] ===== GetByClassIdAsync({classId}) =====");
        Console.WriteLine($"[REPO DEBUG] SQL Query: SELECT sched_id, class_id, sched_day_of_week, sched_start_time, sched_end_time FROM class_schedules WHERE class_id = {classId}");
        Console.WriteLine($"[REPO DEBUG] Retrieved {dt.Rows.Count} row(s) from database");
        
        if (dt.Rows.Count == 0)
        {
            Console.WriteLine($"[REPO DEBUG] ⚠️ WARNING: No schedules found for Class ID {classId}");
            Console.WriteLine($"[REPO DEBUG] Possible reasons:");
            Console.WriteLine($"[REPO DEBUG]   1. Class has no schedules assigned");
            Console.WriteLine($"[REPO DEBUG]   2. Database connection issue");
            Console.WriteLine($"[REPO DEBUG]   3. Invalid class_id parameter");
            Console.WriteLine($"[REPO DEBUG] ===== END GetByClassIdAsync({classId}) (EMPTY) =====");
            return Enumerable.Empty<ClassSchedule>();
        }

        var schedules = new List<ClassSchedule>();
        
        for (int index = 0; index < dt.Rows.Count; index++)
        {
            var row = dt.Rows[index];
            
            // ✅ CRITICAL FIX: Validate row data before creating object
            if (row["sched_id"] == DBNull.Value || 
                row["class_id"] == DBNull.Value || 
                row["sched_day_of_week"] == DBNull.Value ||
                row["sched_start_time"] == DBNull.Value ||
                row["sched_end_time"] == DBNull.Value)
            {
                Console.WriteLine($"[REPO DEBUG] ⚠️ Row {index + 1}: Contains NULL values - SKIPPING");
                Console.WriteLine($"[REPO DEBUG]   sched_id: {row["sched_id"]}");
                Console.WriteLine($"[REPO DEBUG]   class_id: {row["class_id"]}");
                Console.WriteLine($"[REPO DEBUG]   sched_day_of_week: {row["sched_day_of_week"]}");
                Console.WriteLine($"[REPO DEBUG]   sched_start_time: {row["sched_start_time"]}");
                Console.WriteLine($"[REPO DEBUG]   sched_end_time: {row["sched_end_time"]}");
                continue;
            }
            
            var schedule = new ClassSchedule
            {
                SchedId = Convert.ToInt32(row["sched_id"]),
                ClassId = Convert.ToInt32(row["class_id"]),
                SchedDayOfWeek = row["sched_day_of_week"]?.ToString() ?? "",
                SchedStartTime = (TimeSpan)row["sched_start_time"],
                SchedEndTime = (TimeSpan)row["sched_end_time"]
            };
            
            // ✅ CRITICAL DEBUG: Log each schedule retrieved with detailed information
            Console.WriteLine($"[REPO DEBUG] ----- Row {index + 1} -----");
            Console.WriteLine($"[REPO DEBUG]   sched_id: {schedule.SchedId}");
            Console.WriteLine($"[REPO DEBUG]   class_id: {schedule.ClassId}");
            Console.WriteLine($"[REPO DEBUG]   Day: '{schedule.SchedDayOfWeek}' (length: {schedule.SchedDayOfWeek.Length}, bytes: {string.Join(" ", System.Text.Encoding.UTF8.GetBytes(schedule.SchedDayOfWeek).Select(b => b.ToString("X2")))})");
            Console.WriteLine($"[REPO DEBUG]   Start: {schedule.SchedStartTime:hh\\:mm\\:ss} (total seconds: {schedule.SchedStartTime.TotalSeconds})");
            Console.WriteLine($"[REPO DEBUG]   End: {schedule.SchedEndTime:hh\\:mm\\:ss} (total seconds: {schedule.SchedEndTime.TotalSeconds})");
            
            // ✅ VALIDATE: Check for common data issues
            if (string.IsNullOrWhiteSpace(schedule.SchedDayOfWeek))
            {
                Console.WriteLine($"[REPO DEBUG]   ⚠️ WARNING: Day of week is empty or whitespace!");
            }
            
            if (schedule.SchedStartTime >= schedule.SchedEndTime && schedule.SchedEndTime != TimeSpan.Zero)
            {
                Console.WriteLine($"[REPO DEBUG]   ℹ️ INFO: Start time >= End time (possible midnight-crossing schedule)");
            }
            
            schedules.Add(schedule);
        }
        
        Console.WriteLine($"[REPO DEBUG] Successfully converted {schedules.Count} schedule(s) to ClassSchedule objects");
        Console.WriteLine($"[REPO DEBUG] ===== END GetByClassIdAsync({classId}) (SUCCESS) =====");
        
        return schedules;
    }

    public async Task<bool> AddAsync(ClassSchedule sched)
    {
        string query = @"INSERT INTO class_schedules (class_id, sched_day_of_week, sched_start_time, sched_end_time)
                         VALUES (@class, @day, @start, @end);";
        var parameters = new Dictionary<string, object>
        {
            { "@class", sched.ClassId },
            { "@day", sched.SchedDayOfWeek },
            { "@start", sched.SchedStartTime },
            { "@end", sched.SchedEndTime }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(ClassSchedule sched)
    {
        string query = @"UPDATE class_schedules 
                         SET class_id = @class, sched_day_of_week = @day, 
                             sched_start_time = @start, sched_end_time = @end 
                         WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", sched.SchedId },
            { "@class", sched.ClassId },
            { "@day", sched.SchedDayOfWeek },
            { "@start", sched.SchedStartTime },
            { "@end", sched.SchedEndTime }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int schedId)
    {
        string query = "DELETE FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int schedId)
    {
        string query = "SELECT COUNT(*) FROM class_schedules WHERE sched_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", schedId } };
        return await _db.RecordExistsAsync(query, parameters);
    }

    /// <summary>
    /// Gets all class schedules with course information in a single optimized query
    /// Eliminates N+1 query problem by using JOINs instead of multiple repository calls
    /// </summary>
    public async Task<IEnumerable<ClassScheduleDetailDto>> GetAllWithDetailsAsync()
    {
        string query = @"
            SELECT 
                cs.sched_id,
                cs.class_id,
                COALESCE(co.course_name, 'N/A') as course_name,
                cs.sched_day_of_week,
                TO_CHAR(cs.sched_start_time, 'HH24:MI') as sched_start_time,
                TO_CHAR(cs.sched_end_time, 'HH24:MI') as sched_end_time
            FROM class_schedules cs
            INNER JOIN classes cl ON cs.class_id = cl.class_id
            LEFT JOIN courses co ON cl.course_id = co.course_id
            ORDER BY cs.sched_id;";
        
        var dt = await _db.ExecuteQueryAsync(query);
        
        return dt.AsEnumerable().Select(row => new ClassScheduleDetailDto
        {
            SchedId = Convert.ToInt32(row["sched_id"]),
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            SchedDayOfWeek = row["sched_day_of_week"]?.ToString() ?? "",
            SchedStartTime = row["sched_start_time"]?.ToString() ?? "",
            SchedEndTime = row["sched_end_time"]?.ToString() ?? ""
        });
    }

    // ============================================================
    // NEW METHODS FOR SCHEDULE INTEGRATION - PHASE 4
    // ============================================================

    /// <summary>
    /// Get student's class schedule for a specific date
    /// Includes all class details, faculty info, and time slots
    /// Used for late detection when student times in
    /// </summary>
    public async Task<IEnumerable<StudentScheduleDto>> GetStudentScheduleForDateAsync(int studId, DateTime date)
    {
        // Get day of week (Monday, Tuesday, etc.)
        string dayOfWeek = date.DayOfWeek.ToString();
        
        string query = @"
            SELECT 
                c.class_id,
                co.course_id,
                co.course_code,
                co.course_name,
                co.course_units,
                cs.sched_day_of_week,
                cs.sched_start_time,
                cs.sched_end_time,
                COALESCE(r.room_code, 'TBA') as room_code,
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'TBA') as faculty_name
            FROM enrollment e
            INNER JOIN classes c ON e.class_id = c.class_id
            INNER JOIN courses co ON c.course_id = co.course_id
            INNER JOIN class_schedules cs ON c.class_id = cs.class_id
            LEFT JOIN rooms r ON c.room_id = r.room_id
            LEFT JOIN faculty f ON c.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            WHERE e.stud_id = @studId
              AND cs.sched_day_of_week = @dayOfWeek
              AND c.term_id = (SELECT term_id FROM academic_terms WHERE is_active = TRUE LIMIT 1)
            ORDER BY cs.sched_start_time;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@studId", studId },
            { "@dayOfWeek", dayOfWeek }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new StudentScheduleDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseId = Convert.ToInt32(row["course_id"]),
            CourseCode = row["course_code"].ToString() ?? "",
            CourseName = row["course_name"].ToString() ?? "",
            CourseUnits = Convert.ToInt32(row["course_units"]),
            DayOfWeek = row["sched_day_of_week"].ToString() ?? "",
            StartTime = (TimeSpan)row["sched_start_time"],
            EndTime = (TimeSpan)row["sched_end_time"],
            RoomCode = row["room_code"]?.ToString() ?? "TBA",
            FacultyName = row["faculty_name"]?.ToString() ?? "TBA"
        });
    }

    /// <summary>
    /// Get student's schedule for a specific day of week (e.g., "Monday")
    /// Helper method that calls GetStudentScheduleForDateAsync
    /// </summary>
    public async Task<IEnumerable<StudentScheduleDto>> GetStudentScheduleForDayAsync(int studId, string dayOfWeek)
    {
        // Calculate the next occurrence of this day of week
        var targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfWeek);
        var today = DateTime.Today;
        var daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
        var targetDate = today.AddDays(daysUntilTarget);
        
        return await GetStudentScheduleForDateAsync(studId, targetDate);
    }
    
    /// <summary>
    /// ✅ NEW: Gets only classes that have NO schedules assigned (TBA classes)
    /// This is used for the "Add Schedule" dropdown to show only classes that need scheduling
    /// Excludes any class that already has at least one schedule entry
    /// </summary>
    public async Task<IEnumerable<AvailableClassDto>> GetUnscheduledClassesAsync()
    {
        string query = @"
            SELECT 
                c.class_id,
                COALESCE(co.course_code, 'N/A') as course_code,
                COALESCE(co.course_name, 'N/A') as course_name
            FROM classes c
            LEFT JOIN courses co ON c.course_id = co.course_id
            WHERE NOT EXISTS (
                -- Exclude classes that already have schedules
                SELECT 1 
                FROM class_schedules cs 
                WHERE cs.class_id = c.class_id
            )
            ORDER BY co.course_code;";
        
        var dt = await _db.ExecuteQueryAsync(query);
        
        return dt.AsEnumerable().Select(row => new AvailableClassDto
        {
            ClassId = Convert.ToInt32(row["class_id"]),
            CourseCode = row["course_code"]?.ToString() ?? "N/A",
            CourseName = row["course_name"]?.ToString() ?? "N/A",
            FacultyName = "", // Not needed for dropdown
            Schedule = "TBA", // These are all TBA classes
            RoomCode = "" // Not needed for dropdown
        });
    }

    // ============================================================
    // ✅ UPDATED: FACULTY SCHEDULE CONFLICT VALIDATION WITH FACULTY NAME
    // ============================================================
    
    /// <summary>
    /// Checks if a faculty member has a schedule conflict for the proposed time slot
    /// ✅ UPDATED: Now also returns the faculty member's name for personalized error messages
    /// Step A: Gets the faculty ID and name from the class
    /// Step B: Checks for overlapping schedules for the same faculty on the same day
    /// Step C: Returns conflict status, conflicting course name, AND faculty name
    /// </summary>
    /// <param name="classId">The class being scheduled</param>
    /// <param name="dayOfWeek">Day of week (e.g., "Monday")</param>
    /// <param name="startTime">Proposed start time</param>
    /// <param name="endTime">Proposed end time</param>
    /// <param name="excludeSchedId">Schedule ID to exclude (for updates)</param>
    /// <returns>Tuple with conflict status, conflicting course name, and faculty name</returns>
    public async Task<(bool HasConflict, string ConflictingCourseName, string FacultyName)> CheckFacultyScheduleConflictAsync(
        int classId,
        string dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        int? excludeSchedId = null)
    {
        // ✅ STEP A: Get the faculty ID and NAME from the class
        // ✅ STEP B: Check for overlapping schedules
        // Time overlap logic: (NewStart < ExistingEnd) AND (NewEnd > ExistingStart)
        string query = @"
            SELECT 
                co.course_name,
                cs.sched_start_time,
                cs.sched_end_time,
                -- ✅ NEW: Get faculty name for personalized error message
                COALESCE(
                    CONCAT(u.user_first_name, ' ', u.user_last_name, ' (FACULTY)'),
                    'The faculty member'
                ) as faculty_name
            FROM class_schedules cs
            INNER JOIN classes c ON cs.class_id = c.class_id
            INNER JOIN courses co ON c.course_id = co.course_id
            -- ✅ NEW: Join to get faculty name
            LEFT JOIN faculty f ON c.fac_id = f.fac_id
            LEFT JOIN users u ON f.user_id = u.user_id
            WHERE c.fac_id = (SELECT fac_id FROM classes WHERE class_id = @classId)
              AND TRIM(cs.sched_day_of_week) = TRIM(@dayOfWeek)
              AND cs.sched_start_time < @endTime
              AND cs.sched_end_time > @startTime
              AND cs.class_id != @classId";
        
        // If updating an existing schedule, exclude it from conflict check
        if (excludeSchedId.HasValue)
        {
            query += " AND cs.sched_id != @excludeSchedId";
        }
        
        query += " LIMIT 1;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@classId", classId },
            { "@dayOfWeek", dayOfWeek },
            { "@startTime", startTime },
            { "@endTime", endTime }
        };
        
        if (excludeSchedId.HasValue)
        {
            parameters.Add("@excludeSchedId", excludeSchedId.Value);
        }
        
        var dt = await _db.ExecuteQueryAsync(query, parameters);
        
        if (dt.Rows.Count > 0)
        {
            var conflictingCourse = dt.Rows[0]["course_name"]?.ToString() ?? "Unknown Course";
            var facultyName = dt.Rows[0]["faculty_name"]?.ToString() ?? "The faculty member";
            var conflictStart = (TimeSpan)dt.Rows[0]["sched_start_time"];
            var conflictEnd = (TimeSpan)dt.Rows[0]["sched_end_time"];
            
            Console.WriteLine($"[CONFLICT] Faculty conflict detected:");
            Console.WriteLine($"  - Faculty: {facultyName}");
            Console.WriteLine($"  - Conflicting Course: {conflictingCourse}");
            Console.WriteLine($"  - Conflict Time: {conflictStart:hh\\:mm} - {conflictEnd:hh\\:mm}");
            Console.WriteLine($"  - Proposed Time: {startTime:hh\\:mm} - {endTime:hh\\:mm}");
            
            return (true, conflictingCourse, facultyName);
        }
        
        return (false, string.Empty, string.Empty);
    }
}
