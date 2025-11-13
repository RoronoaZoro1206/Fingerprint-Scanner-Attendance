using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Data;
using System.Data;

namespace Fingerprint_Attendance_System.Controllers
{
    [Route("Attendance")]
    public class AttendanceScanController : Controller
    {
        private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IUserRepository _userRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IScanLogRepository _scanLogRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IClassScheduleRepository _scheduleRepo;
        private readonly DatabaseHelper _db;
        private readonly ILogger<AttendanceScanController> _logger;

        // Grace period in minutes for late detection (configurable)
        private const int LATE_GRACE_PERIOD_MINUTES = 15;

        public AttendanceScanController(
            FingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IUserRepository userRepo,
            IStudentRepository studentRepo,
            IScanLogRepository scanLogRepo,
            IAttendanceRepository attendanceRepo,
            IClassScheduleRepository scheduleRepo,
            DatabaseHelper db,
            ILogger<AttendanceScanController> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _scanLogRepo = scanLogRepo;
            _attendanceRepo = attendanceRepo;
            _scheduleRepo = scheduleRepo;
            _db = db;
            _logger = logger;
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
                // STEP 1: CAPTURE & IDENTIFY STUDENT
                // ============================================================
                
                // Capture fingerprint template
                var capturedTemplate = _fingerprintService.CaptureTemplate();
                
                if (capturedTemplate == null)
                {
                    _logger.LogWarning("No finger detected on scanner");
                    
                    // Log failed scan attempt
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = null,
                        ScanTime = DateTime.Now,
                        ScanSuccess = false,
                        ScanLogMessage = "No fingerprint detected - please place finger on scanner",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = false,
                        message = "No finger detected. Please place your finger firmly on the scanner."
                    });
                }

                _logger.LogInformation("Fingerprint captured successfully, matching against database...");

                // Match against all registered fingerprints
                var allTemplates = await _fingerprintRepo.GetAllAsync();
                
                if (!allTemplates.Any())
                {
                    _logger.LogWarning("No registered fingerprints in database");
                    
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = null,
                        ScanTime = DateTime.Now,
                        ScanSuccess = false,
                        ScanLogMessage = "No registered fingerprints in system",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = false,
                        message = "No registered users in the system. Please register fingerprints first."
                    });
                }

                int matchThreshold = 50;
                FingerprintTemplate? matchedTemplate = null;
                int bestScore = 0;

                foreach (var template in allTemplates)
                {
                    try
                    {
                        int score = _fingerprintService.GetMatchScore(capturedTemplate, template.FtempData);
                        _logger.LogInformation($"Match score for user {template.UserId}: {score}");

                        if (score > bestScore && score >= matchThreshold)
                        {
                            bestScore = score;
                            matchedTemplate = template;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error matching template for user {template.UserId}");
                    }
                }

                if (matchedTemplate == null)
                {
                    _logger.LogWarning($"Fingerprint not recognized. Best score: {bestScore}");
                    
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = null,
                        ScanTime = DateTime.Now,
                        ScanSuccess = false,
                        ScanLogMessage = $"Fingerprint not recognized (Best Score: {bestScore})",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = false,
                        message = "Fingerprint not recognized. Please ensure you are registered in the system."
                    });
                }

                _logger.LogInformation($"? Fingerprint matched! User ID: {matchedTemplate.UserId}, Score: {bestScore}");

                // Get user details
                var user = await _userRepo.GetByIdAsync(matchedTemplate.UserId);
                
                if (user == null)
                {
                    _logger.LogError($"User not found for ID: {matchedTemplate.UserId}");
                    
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = matchedTemplate.UserId,
                        ScanTime = DateTime.Now,
                        ScanSuccess = false,
                        ScanLogMessage = "User account not found",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = false,
                        message = "User account not found. Please contact administration."
                    });
                }

                // ? CRUCIAL VALIDATION: Check if user is a student
                if (user.RoleType != Models.Enums.USER_ROLES.STUDENT)
                {
                    _logger.LogWarning($"Non-student user attempted to log attendance: {user.UserEmail} (Role: {user.RoleType})");
                    
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = user.UserId,
                        ScanTime = DateTime.Now,
                        ScanSuccess = false,
                        ScanLogMessage = $"Non-student user attempted attendance scan - Role: {user.RoleType}",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = false,
                        message = "Only students can log attendance at this terminal."
                    });
                }

                // Get student record
                var student = await _studentRepo.GetByUserIdAsync(user.UserId);
                
                if (student == null)
                {
                    _logger.LogError($"Student record not found for user: {user.UserId}");
                    
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = user.UserId,
                        ScanTime = DateTime.Now,
                        ScanSuccess = false,
                        ScanLogMessage = "Student record not found",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = false,
                        message = "Student profile not found. Please complete your profile first."
                    });
                }

                _logger.LogInformation($"Student identified: {student.StudId} ({user.UserFirstName} {user.UserLastName})");

                // ============================================================
                // STEP 2: DETERMINE "TIME IN" vs "TIME OUT" STATUS
                // ============================================================

                var dailyStatusQuery = "SELECT * FROM get_daily_attendance_record(@stud_id, CURRENT_DATE)";
                var dailyStatusParams = new Dictionary<string, object> { { "@stud_id", student.StudId } };
                var dailyStatusDt = await _db.ExecuteQueryAsync(dailyStatusQuery, dailyStatusParams);

                bool canTimeIn = false;
                bool canTimeOut = false;
                int? existingDailyAttendId = null;

                if (dailyStatusDt.Rows.Count > 0)
                {
                    var row = dailyStatusDt.Rows[0];
                    canTimeIn = row["can_time_in"] != DBNull.Value && Convert.ToBoolean(row["can_time_in"]);
                    canTimeOut = row["can_time_out"] != DBNull.Value && Convert.ToBoolean(row["can_time_out"]);
                    existingDailyAttendId = row["daily_attend_id"] != DBNull.Value ? Convert.ToInt32(row["daily_attend_id"]) : null;
                }

                _logger.LogInformation($"Daily status - Can Time In: {canTimeIn}, Can Time Out: {canTimeOut}");

                // ============================================================
                // STEP 3: HANDLE TIME IN LOGIC
                // ============================================================

                if (canTimeIn)
                {
                    _logger.LogInformation("Processing TIME IN event");

                    // Find active class for the student
                    var currentDay = DateTime.Now.ToString("dddd"); // e.g., "Monday"
                    var currentTime = DateTime.Now.TimeOfDay;

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
                        _logger.LogWarning($"No active class found for student {student.StudId} on {currentDay} at {currentTime}");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = false,
                            ScanLogMessage = "No scheduled class at this time",
                            ScanCreatedAt = DateTime.Now
                        });

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

                    _logger.LogInformation($"Active class found: {courseCode} - {courseName} (Class ID: {classId})");

                    // Record time in using database function
                    var timeInQuery = "SELECT * FROM record_time_in(@stud_id, @timestamp)";
                    var timeInParams = new Dictionary<string, object>
                    {
                        { "@stud_id", student.StudId },
                        { "@timestamp", DateTime.Now }
                    };

                    var timeInDt = await _db.ExecuteQueryAsync(timeInQuery, timeInParams);

                    if (timeInDt.Rows.Count == 0)
                    {
                        _logger.LogError("Failed to execute record_time_in function");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = false,
                            ScanLogMessage = "Database error during time in",
                            ScanCreatedAt = DateTime.Now
                        });

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
                        _logger.LogError($"Time in failed: {timeInMessage}");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = false,
                            ScanLogMessage = $"Time in failed: {timeInMessage}",
                            ScanCreatedAt = DateTime.Now
                        });

                        return Json(new
                        {
                            success = false,
                            message = timeInMessage
                        });
                    }

                    _logger.LogInformation($"Daily time in recorded successfully. Daily Attend ID: {newDailyAttendId}");

                    // Determine attendance status (present vs late)
                    TimeSpan graceEnd = schedStartTime.Add(TimeSpan.FromMinutes(LATE_GRACE_PERIOD_MINUTES));
                    AttendanceStatus status = currentTime <= graceEnd ? AttendanceStatus.Present : AttendanceStatus.Late;

                    _logger.LogInformation($"Attendance status determined: {status} (Grace period: {LATE_GRACE_PERIOD_MINUTES} min)");

                    // Check if attendance record already exists for this class today
                    var existingAttendance = await _attendanceRepo.GetByClassStudentDateAsync(classId, student.StudId, DateTime.Today);

                    if (existingAttendance != null)
                    {
                        _logger.LogInformation($"Attendance record already exists for class {classId} on {DateTime.Today}");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = true,
                            ScanLogMessage = $"Time in recorded (attendance already marked for {courseCode})",
                            ScanCreatedAt = DateTime.Now
                        });

                        return Json(new
                        {
                            success = true,
                            message = "Time In Successful",
                            studentName = $"{user.UserFirstName} {user.UserLastName}",
                            subject = $"{courseCode} - {courseName}",
                            scanTime = DateTime.Now.ToString("hh:mm:ss tt"),
                            scanType = "Time In",
                            status = status.ToString()
                        });
                    }

                    // Insert class-specific attendance record
                    var attendance = new Attendance
                    {
                        ClassId = classId,
                        StudId = student.StudId,
                        AttendDate = DateTime.Today,
                        AttendStatus = status,
                        DailyAttendId = newDailyAttendId.Value,
                        IsPresent = true,
                        AttendScanTime = DateTime.Now,
                        AttendType = ScanType.In,
                        AttendCreatedAt = DateTime.Now
                    };

                    bool attendanceCreated = await _attendanceRepo.AddAsync(attendance);

                    if (!attendanceCreated)
                    {
                        _logger.LogError("Failed to create attendance record");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = false,
                            ScanLogMessage = "Failed to create class attendance record",
                            ScanCreatedAt = DateTime.Now
                        });

                        return Json(new
                        {
                            success = false,
                            message = "Failed to record attendance. Please contact administration."
                        });
                    }

                    _logger.LogInformation($"? TIME IN COMPLETE - Student: {user.UserFirstName} {user.UserLastName}, Class: {courseCode}, Status: {status}");

                    // Log successful scan
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = user.UserId,
                        ScanTime = DateTime.Now,
                        ScanSuccess = true,
                        ScanLogMessage = $"Successful Time In for {user.UserFirstName} {user.UserLastName} - {courseCode} ({status})",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = true,
                        message = "Time In Successful",
                        studentName = $"{user.UserFirstName} {user.UserLastName}",
                        subject = $"{courseCode} - {courseName}",
                        scanTime = DateTime.Now.ToString("hh:mm:ss tt"),
                        scanType = "Time In",
                        status = status.ToString()
                    });
                }

                // ============================================================
                // STEP 4: HANDLE TIME OUT LOGIC
                // ============================================================

                if (canTimeOut)
                {
                    _logger.LogInformation("Processing TIME OUT event");

                    // Record time out using database function
                    var timeOutQuery = "SELECT * FROM record_time_out(@stud_id, @timestamp)";
                    var timeOutParams = new Dictionary<string, object>
                    {
                        { "@stud_id", student.StudId },
                        { "@timestamp", DateTime.Now }
                    };

                    var timeOutDt = await _db.ExecuteQueryAsync(timeOutQuery, timeOutParams);

                    if (timeOutDt.Rows.Count == 0)
                    {
                        _logger.LogError("Failed to execute record_time_out function");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = false,
                            ScanLogMessage = "Database error during time out",
                            ScanCreatedAt = DateTime.Now
                        });

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
                        _logger.LogError($"Time out failed: {timeOutMessage}");
                        
                        await _scanLogRepo.AddAsync(new ScanLog
                        {
                            UserId = user.UserId,
                            ScanTime = DateTime.Now,
                            ScanSuccess = false,
                            ScanLogMessage = $"Time out failed: {timeOutMessage}",
                            ScanCreatedAt = DateTime.Now
                        });

                        return Json(new
                        {
                            success = false,
                            message = timeOutMessage
                        });
                    }

                    _logger.LogInformation($"? TIME OUT COMPLETE - Student: {user.UserFirstName} {user.UserLastName}");

                    // Log successful scan
                    await _scanLogRepo.AddAsync(new ScanLog
                    {
                        UserId = user.UserId,
                        ScanTime = DateTime.Now,
                        ScanSuccess = true,
                        ScanLogMessage = $"Successful Time Out for {user.UserFirstName} {user.UserLastName}",
                        ScanCreatedAt = DateTime.Now
                    });

                    return Json(new
                    {
                        success = true,
                        message = "Time Out Successful",
                        studentName = $"{user.UserFirstName} {user.UserLastName}",
                        subject = "N/A",
                        scanTime = DateTime.Now.ToString("hh:mm:ss tt"),
                        scanType = "Time Out"
                    });
                }

                // ============================================================
                // FALLBACK: Neither time in nor time out is allowed
                // ============================================================

                _logger.LogWarning($"No valid action for student {student.StudId}");
                
                await _scanLogRepo.AddAsync(new ScanLog
                {
                    UserId = user.UserId,
                    ScanTime = DateTime.Now,
                    ScanSuccess = false,
                    ScanLogMessage = "No valid action (already timed in/out)",
                    ScanCreatedAt = DateTime.Now
                });

                return Json(new
                {
                    success = false,
                    message = "No action available. You may have already completed your attendance for today."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? ERROR DURING ATTENDANCE SCAN");

                await _scanLogRepo.AddAsync(new ScanLog
                {
                    UserId = null,
                    ScanTime = DateTime.Now,
                    ScanSuccess = false,
                    ScanLogMessage = $"System error: {ex.Message}",
                    ScanCreatedAt = DateTime.Now
                });

                return Json(new
                {
                    success = false,
                    message = "System error occurred. Please try again or contact support."
                });
            }
        }

        /// <summary>
        /// Get today's scan statistics for live dashboard
        /// </summary>
        [HttpGet("GetTodayStats")]
        public async Task<IActionResult> GetTodayStats()
        {
            try
            {
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
                    WHERE DATE(scan_time) = CURRENT_DATE;";

                var dt = await _db.ExecuteQueryAsync(statsQuery);

                if (dt.Rows.Count == 0)
                {
                    return Json(new
                    {
                        successRate = "0%",
                        todaysScans = 0
                    });
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
                return Json(new
                {
                    successRate = "N/A",
                    todaysScans = 0
                });
            }
        }
    }
}
