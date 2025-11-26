using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Data;
using System.Data;
using StudentModel = Fingerprint_Attendance_System.Models.Student;

namespace Fingerprint_Attendance_System.Controllers
{
    /// <summary>
    /// MVC Controller for fingerprint-based attendance scanning.
    /// Uses FingerprintMatchingService for shared matching logic.
    /// Settings loaded from appsettings.json (no hardcoded magic numbers).
    /// </summary>
    [Route("Attendance")]
    public class AttendanceScanController : Controller
    {
        private readonly FingerprintMatchingService _matchingService;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly DatabaseHelper _db;
        private readonly ILogger<AttendanceScanController> _logger;
        private readonly PhilippineTimeService _pht;

        public AttendanceScanController(
            FingerprintMatchingService matchingService,
            IAttendanceRepository attendanceRepo,
            DatabaseHelper db,
            ILogger<AttendanceScanController> logger,
            PhilippineTimeService pht)
        {
            _matchingService = matchingService;
            _attendanceRepo = attendanceRepo;
            _db = db;
            _logger = logger;
            _pht = pht;
        }

        // GET: /Attendance
        public IActionResult Index()
        {
            return View("~/Views/Home/Index.cshtml");
        }

        /// <summary>
        /// Main scanning endpoint - handles real-time fingerprint scanning and attendance logging
        /// </summary>
        [HttpPost("Scan")]
        public async Task<IActionResult> Scan()
        {
            try
            {
                _logger.LogInformation("=== ATTENDANCE SCAN STARTED ===");

                // ============================================================
                // STEP 1: CAPTURE & IDENTIFY STUDENT (using shared service)
                // ============================================================
                
                var capturedTemplate = _matchingService.CaptureFingerprint();
                
                if (capturedTemplate == null)
                {
                    _logger.LogWarning("No finger detected on scanner");
                    // ✅ FIX: Do NOT log "no finger detected" events to prevent scan_logs flooding
                    // Only log meaningful events (successful scans, fingerprint not recognized, etc.)

                    return Json(new
                    {
                        success = false,
                        message = "No finger detected. Please place your finger firmly on the scanner."
                    });
                }

                _logger.LogInformation("Fingerprint captured successfully, matching against database...");

                // Use shared matching service (eliminates duplicate code)
                var matchResult = await _matchingService.MatchAndValidateFingerprintAsync(capturedTemplate, requireStudentRole: true);

                if (!matchResult.Success)
                {
                    _logger.LogWarning("Match failed: {Message}", matchResult.Message);
                    await _matchingService.LogScanAttemptAsync(matchResult.UserId, false, matchResult.Message);

                    return Json(new
                    {
                        success = false,
                        message = matchResult.Message
                    });
                }

                var student = matchResult.Student!;
                var user = matchResult.User!;
                
                _logger.LogInformation("Student identified: {StudId} ({Name})", 
                    student.StudId, $"{user.UserFirstName} {user.UserLastName}");

                // ============================================================
                // STEP 2: DETERMINE "TIME IN" vs "TIME OUT" STATUS
                // ============================================================

                var dailyStatusQuery = "SELECT * FROM get_daily_attendance_record(@stud_id, CURRENT_DATE)";
                var dailyStatusParams = new Dictionary<string, object> { { "@stud_id", student.StudId } };
                var dailyStatusDt = await _db.ExecuteQueryAsync(dailyStatusQuery, dailyStatusParams);

                bool canTimeIn = false;
                bool canTimeOut = false;

                if (dailyStatusDt.Rows.Count > 0)
                {
                    var row = dailyStatusDt.Rows[0];
                    canTimeIn = row["can_time_in"] != DBNull.Value && Convert.ToBoolean(row["can_time_in"]);
                    canTimeOut = row["can_time_out"] != DBNull.Value && Convert.ToBoolean(row["can_time_out"]);
                }

                _logger.LogInformation("Daily status - Can Time In: {CanTimeIn}, Can Time Out: {CanTimeOut}", canTimeIn, canTimeOut);

                // ============================================================
                // STEP 3: HANDLE TIME IN LOGIC
                // ============================================================

                if (canTimeIn)
                {
                    return await ProcessTimeInAsync(student, user);
                }

                // ============================================================
                // STEP 4: HANDLE TIME OUT LOGIC
                // ============================================================

                if (canTimeOut)
                {
                    return await ProcessTimeOutAsync(student, user);
                }

                // ============================================================
                // FALLBACK: Neither time in nor time out is allowed
                // ============================================================

                _logger.LogWarning("No valid action for student {StudId}", student.StudId);
                await _matchingService.LogScanAttemptAsync(user.UserId, false, "No valid action (already timed in/out)");

                return Json(new
                {
                    success = false,
                    message = "No action available. You may have already completed your attendance for today."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR DURING ATTENDANCE SCAN");
                await _matchingService.LogScanAttemptAsync(null, false, $"System error: {ex.Message}");

                return Json(new
                {
                    success = false,
                    message = "System error occurred. Please try again or contact support."
                });
            }
        }

        /// <summary>
        /// Process TIME IN event
        /// </summary>
        private async Task<IActionResult> ProcessTimeInAsync(StudentModel student, User user)
        {
            _logger.LogInformation("Processing TIME IN event");

            var currentDay = _pht.Now.ToString("dddd"); // Use PHT for schedule matching
            var currentTime = _pht.TimeOfDay; // Use PHT for schedule matching

            var scheduleQuery = @"
                SELECT * FROM v_student_daily_schedule
                WHERE stud_id = @stud_id
                AND TRIM(sched_day_of_week) = @day_of_week
                AND @current_time BETWEEN sched_start_time AND sched_end_time
                LIMIT 1;";

            var scheduleParams = new Dictionary<string, object>
            {
                { "@stud_id", student.StudId },
                { "@day_of_week", currentDay },
                { "@current_time", currentTime }
            };

            var scheduleDt = await _db.ExecuteQueryAsync(scheduleQuery, scheduleParams);

            if (scheduleDt.Rows.Count == 0)
            {
                _logger.LogWarning("No active class found for student {StudId} on {Day} at {Time}", 
                    student.StudId, currentDay, currentTime);
                
                await _matchingService.LogScanAttemptAsync(user.UserId, false, "No scheduled class at this time");

                return Json(new
                {
                    success = false,
                    message = "You do not have a scheduled class at this time.",
                    studentName = $"{user.UserFirstName} {user.UserLastName}"
                });
            }

            var scheduleRow = scheduleDt.Rows[0];
            int classId = Convert.ToInt32(scheduleRow["class_id"]);
            string courseName = scheduleRow["course_name"]?.ToString() ?? "Unknown Course";
            string courseCode = scheduleRow["course_code"]?.ToString() ?? "N/A";
            TimeSpan schedStartTime = (TimeSpan)scheduleRow["sched_start_time"];

            _logger.LogInformation("Active class found: {CourseCode} - {CourseName} (Class ID: {ClassId})", 
                courseCode, courseName, classId);

            // Record time in using database function
            var timeInQuery = "SELECT * FROM record_time_in(@stud_id, @class_id, @timestamp)";
            var timeInParams = new Dictionary<string, object>
            {
                { "@stud_id", student.StudId },
                { "@class_id", classId },
                { "@timestamp", _pht.Now } // Use PHT for time in timestamp
            };

            var timeInDt = await _db.ExecuteQueryAsync(timeInQuery, timeInParams);

            if (timeInDt.Rows.Count == 0)
            {
                _logger.LogError("Failed to execute record_time_in function");
                await _matchingService.LogScanAttemptAsync(user.UserId, false, "Database error during time in");

                return Json(new
                {
                    success = false,
                    message = "Failed to record time in. Please try again."
                });
            }

            var timeInRow = timeInDt.Rows[0];
            bool timeInSuccess = Convert.ToBoolean(timeInRow["success"]);
            string timeInMessage = timeInRow["message"]?.ToString() ?? "Unknown error";
            int? newDailyAttendId = timeInRow["daily_attend_id"] != DBNull.Value 
                ? Convert.ToInt32(timeInRow["daily_attend_id"]) 
                : null;

            if (!timeInSuccess || newDailyAttendId == null)
            {
                _logger.LogError("Time in failed: {Message}", timeInMessage);
                await _matchingService.LogScanAttemptAsync(user.UserId, false, $"Time in failed: {timeInMessage}");

                return Json(new
                {
                    success = false,
                    message = timeInMessage
                });
            }

            _logger.LogInformation("Daily time in recorded successfully. Daily Attend ID: {DailyAttendId}", newDailyAttendId);

            // Determine attendance status using configurable grace period
            var status = _matchingService.DetermineAttendanceStatus(currentTime, schedStartTime);
            
            _logger.LogInformation("Attendance status determined: {Status} (Grace period: {GracePeriod} min)", 
                status, _matchingService.Settings.LateGracePeriodMinutes);

            // Check if attendance record already exists
            var existingAttendance = await _attendanceRepo.GetByClassStudentDateAsync(classId, student.StudId, _pht.Today);

            if (existingAttendance != null)
            {
                _logger.LogInformation("Attendance record already exists for class {ClassId} on {Date}", classId, _pht.Today);
                await _matchingService.LogScanAttemptAsync(user.UserId, true, $"Time in recorded (attendance already marked for {courseCode})");

                return Json(new
                {
                    success = true,
                    message = "Time In Successful",
                    studentName = $"{user.UserFirstName} {user.UserLastName}",
                    subject = $"{courseCode} - {courseName}",
                    scanTime = _pht.Now.ToString("hh:mm:ss tt"), // Display PHT to user
                    scanType = "Time In",
                    status = status.ToString()
                });
            }

            // Insert class-specific attendance record
            var attendance = new Attendance
            {
                ClassId = classId,
                StudId = student.StudId,
                AttendDate = _pht.Today, // Use PHT date
                AttendStatus = status,
                DailyAttendId = newDailyAttendId.Value,
                IsPresent = true,
                AttendScanTime = _pht.Now, // Use PHT for scan time
                AttendType = ScanType.In,
                AttendCreatedAt = DateTime.Now // Server time for audit
            };

            bool attendanceCreated = await _attendanceRepo.AddAsync(attendance);

            if (!attendanceCreated)
            {
                _logger.LogError("Failed to create attendance record");
                await _matchingService.LogScanAttemptAsync(user.UserId, false, "Failed to create class attendance record");

                return Json(new
                {
                    success = false,
                    message = "Failed to record attendance. Please contact administration."
                });
            }

            _logger.LogInformation("✅ TIME IN COMPLETE - Student: {Name}, Class: {CourseCode}, Status: {Status}", 
                $"{user.UserFirstName} {user.UserLastName}", courseCode, status);

            await _matchingService.LogScanAttemptAsync(user.UserId, true, 
                $"Successful Time In for {user.UserFirstName} {user.UserLastName} - {courseCode} ({status})");

            return Json(new
            {
                success = true,
                message = "Time In Successful",
                studentName = $"{user.UserFirstName} {user.UserLastName}",
                subject = $"{courseCode} - {courseName}",
                scanTime = _pht.Now.ToString("hh:mm:ss tt"), // Display PHT to user
                scanType = "Time In",
                status = status.ToString()
            });
        }

        /// <summary>
        /// Process TIME OUT event
        /// </summary>
        private async Task<IActionResult> ProcessTimeOutAsync(StudentModel student, User user)
        {
            _logger.LogInformation("Processing TIME OUT event");

            var timeOutQuery = "SELECT * FROM record_time_out(@stud_id, @timestamp)";
            var timeOutParams = new Dictionary<string, object>
            {
                { "@stud_id", student.StudId },
                { "@timestamp", _pht.Now } // Use PHT for time out timestamp
            };

            var timeOutDt = await _db.ExecuteQueryAsync(timeOutQuery, timeOutParams);

            if (timeOutDt.Rows.Count == 0)
            {
                _logger.LogError("Failed to execute record_time_out function");
                await _matchingService.LogScanAttemptAsync(user.UserId, false, "Database error during time out");

                return Json(new
                {
                    success = false,
                    message = "Failed to record time out. Please try again."
                });
            }

            var timeOutRow = timeOutDt.Rows[0];
            bool timeOutSuccess = Convert.ToBoolean(timeOutRow["success"]);
            string timeOutMessage = timeOutRow["message"]?.ToString() ?? "Unknown error";

            if (!timeOutSuccess)
            {
                _logger.LogError("Time out failed: {Message}", timeOutMessage);
                await _matchingService.LogScanAttemptAsync(user.UserId, false, $"Time out failed: {timeOutMessage}");

                return Json(new
                {
                    success = false,
                    message = timeOutMessage
                });
            }

            _logger.LogInformation("✅ TIME OUT COMPLETE - Student: {Name}", $"{user.UserFirstName} {user.UserLastName}");

            await _matchingService.LogScanAttemptAsync(user.UserId, true, 
                $"Successful Time Out for {user.UserFirstName} {user.UserLastName}");

            return Json(new
            {
                success = true,
                message = "Time Out Successful",
                studentName = $"{user.UserFirstName} {user.UserLastName}",
                subject = "N/A",
                scanTime = _pht.Now.ToString("hh:mm:ss tt"), // Display PHT to user
                scanType = "Time Out"
            });
        }

        /// <summary>
        /// Get today's scan statistics for live dashboard
        /// ✅ FIX: Count ALL meaningful scans (fingerprint captured) for Today's Scans
        /// </summary>
        [HttpGet("GetTodayStats")]
        public async Task<IActionResult> GetTodayStats()
        {
            try
            {
                // ✅ FIX: Calculate success rate based on ALL meaningful scan attempts
                // Include all scans where fingerprint was captured (regardless of outcome)
                // Exclude only: no finger detected, scanner unavailable, capture failed
                var statsQuery = @"
                    SELECT 
                        COUNT(*) FILTER (WHERE scan_success = TRUE) as successful_scans,
                        COUNT(*) as total_scans,
                        CASE 
                            WHEN COUNT(*) > 0 
                            THEN ROUND((COUNT(*) FILTER (WHERE scan_success = TRUE)::DECIMAL / COUNT(*)) * 100, 1)
                            ELSE 0 
                        END as success_rate
                    FROM scan_logs
                    WHERE DATE(scan_time) = CURRENT_DATE
                    AND scan_log_message IS NOT NULL
                    AND scan_log_message NOT ILIKE '%no finger detected%'
                    AND scan_log_message NOT ILIKE '%scanner unavailable%'
                    AND scan_log_message NOT ILIKE '%capture failed%';";

                var dt = await _db.ExecuteQueryAsync(statsQuery);

                if (dt.Rows.Count == 0)
                {
                    return Json(new { successRate = "0%", todaysScans = 0 });
                }

                var row = dt.Rows[0];
                decimal successRate = row["success_rate"] != DBNull.Value ? Convert.ToDecimal(row["success_rate"]) : 0;
                int totalScans = row["total_scans"] != DBNull.Value ? Convert.ToInt32(row["total_scans"]) : 0;

                return Json(new
                {
                    successRate = $"{successRate}%",
                    todaysScans = totalScans
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's stats");
                return Json(new { successRate = "N/A", todaysScans = 0 });
            }
        }
    }
}
