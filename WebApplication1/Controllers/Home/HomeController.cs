using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using StudentModel = Fingerprint_Attendance_System.Models.Student; // ✅ FIX: Add alias to avoid namespace conflict

namespace Fingerprint_Attendance_System.Controllers.Home;

public class HomeController : Controller
{
    private readonly FingerprintService _fingerprintService;
    private readonly IFingerprintTemplateRepository _fingerprintRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IClassScheduleRepository _classScheduleRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo; // ✅ NEW
    private readonly IClassRepository _classRepo; // ✅ NEW
    private readonly ICourseRepository _courseRepo; // ✅ NEW
    private readonly IFacultyRepository _facultyRepo; // ✅ NEW
    private readonly IRoomRepository _roomRepo; // ✅ NEW
    private readonly IUserRepository _userRepo; // ✅ NEW
    private readonly AttendanceProcessingService _attendanceService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        FingerprintService fingerprintService,
        IFingerprintTemplateRepository fingerprintRepo,
        IStudentRepository studentRepo,
        IClassScheduleRepository classScheduleRepo,
        IEnrollmentRepository enrollmentRepo,
        IDailyAttendanceLogRepository dailyAttendanceRepo, // ✅ NEW
        IClassRepository classRepo, // ✅ NEW
        ICourseRepository courseRepo, // ✅ NEW
        IFacultyRepository facultyRepo, // ✅ NEW
        IRoomRepository roomRepo, // ✅ NEW
        IUserRepository userRepo, // ✅ NEW
        AttendanceProcessingService attendanceService,
        ILogger<HomeController> logger)
    {
        _fingerprintService = fingerprintService;
        _fingerprintRepo = fingerprintRepo;
        _studentRepo = studentRepo;
        _classScheduleRepo = classScheduleRepo;
        _enrollmentRepo = enrollmentRepo;
        _dailyAttendanceRepo = dailyAttendanceRepo; // ✅ NEW
        _classRepo = classRepo; // ✅ NEW
        _courseRepo = courseRepo; // ✅ NEW
        _facultyRepo = facultyRepo; // ✅ NEW
        _roomRepo = roomRepo; // ✅ NEW
        _userRepo = userRepo; // ✅ NEW
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Public homepage with fingerprint scanner
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Check scanner availability for public page
    /// GET: /Home/CheckScannerStatus
    /// </summary>
    [HttpGet]
    public IActionResult CheckScannerStatus()
    {
        try
        {
            var status = _fingerprintService.GetStatus();
            var isAvailable = _fingerprintService.IsDeviceConnected;

            return Json(new
            {
                success = true,
                isAvailable = isAvailable,
                status = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking scanner status");
            return Json(new
            {
                success = false,
                isAvailable = false,
                status = "Error",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Initialize scanner for public page
    /// POST: /Home/InitializeScanner
    /// </summary>
    [HttpPost]
    public IActionResult InitializeScanner()
    {
        try
        {
            _logger.LogInformation("Initializing scanner for public page...");

            // Check if already initialized
            if (_fingerprintService.IsDeviceConnected)
            {
                return Json(new { success = true, message = "Scanner already connected" });
            }

            // Initialize SDK
            var initialized = _fingerprintService.Initialize();
            if (!initialized)
            {
                return Json(new { success = false, message = "Failed to initialize SDK" });
            }

            // Turn on device
            var deviceOpened = _fingerprintService.TurnOnDevice();
            if (!deviceOpened)
            {
                return Json(new { success = false, message = "Failed to open device" });
            }

            _logger.LogInformation("Scanner initialized successfully");
            return Json(new { success = true, message = "Scanner connected" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing scanner");
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    /// <summary>
    /// Process public fingerprint scan for attendance with intelligent schedule validation
    /// ✅ ENHANCED: Prevents duplicate time-in after completing attendance for same class
    /// POST: /Home/ScanFingerprint
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ScanFingerprint()
    {
        try
        {
            _logger.LogInformation("=== Public Attendance Scan Started with Schedule Validation ===");

            // STEP 1: Validate scanner availability
            if (!_fingerprintService.IsDeviceConnected)
            {
                _logger.LogWarning("Scanner not available");
                return Json(new
                {
                    success = false,
                    messageType = "scanner_unavailable",
                    message = "⚠️ Scanner not available. Please check device connection."
                });
            }

            // STEP 2: Capture fingerprint with timeout
            _logger.LogInformation("Capturing fingerprint...");
            var (imageData, width, height, template, errorCode, errorMessage) = _fingerprintService.CaptureFingerprintWithImage();

            if (template == null || template.Length == 0)
            {
                _logger.LogWarning($"Failed to capture fingerprint: {errorMessage}");
                return Json(new
                {
                    success = false,
                    messageType = "capture_failed",
                    message = string.IsNullOrEmpty(errorMessage) 
                        ? "⚠️ Failed to capture fingerprint. Please try again." 
                        : $"⚠️ {errorMessage}"
                });
            }

            // STEP 3: Match fingerprint against database and check user role
            _logger.LogInformation("Matching fingerprint against database...");
            var matchResult = await MatchFingerprintWithRoleCheckAsync(template);

            // Handle fingerprint not found or wrong role
            if (matchResult.Student == null && matchResult.User == null)
            {
                _logger.LogWarning("Fingerprint not recognized");
                
                // Error feedback: red light + beep
                _fingerprintService.SetLedLight("red", true);
                _fingerprintService.Beep(true);
                _ = Task.Delay(500).ContinueWith(_ =>
                {
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                });
                
                return Json(new
                {
                    success = false,
                    messageType = "unregistered",
                    message = "🔍 Fingerprint not recognized.\n\n" +
                             "This fingerprint is not registered in the system.\n" +
                             "Please contact the administration office to register your fingerprint."
                });
            }

            if (matchResult.Student == null && matchResult.User != null)
            {
                _logger.LogWarning($"Non-student user detected: {matchResult.User.UserEmail} (Role: {matchResult.UserRoleName})");
                
                // Error feedback: red light + beep
                _fingerprintService.SetLedLight("red", true);
                _fingerprintService.Beep(true);
                _ = Task.Delay(500).ContinueWith(_ =>
                {
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                });

                var fullName = $"{matchResult.User.UserFirstName} {matchResult.User.UserLastName}";
                var roleEmoji = matchResult.UserRoleName == "Admin" ? "🛡️" : "👨‍🏫";
                var dashboardText = matchResult.UserRoleName == "Admin" 
                    ? "Please use the Admin Dashboard to manage the system." 
                    : "Please use the Faculty Dashboard to manage your classes.";
                
                return Json(new
                {
                    success = false,
                    messageType = "role_restriction",
                    userRole = matchResult.UserRoleName,
                    userName = fullName,
                    message = $"{roleEmoji} {matchResult.UserRoleName} Fingerprint Detected\n\n" +
                             $"Hello, {fullName}\n\n" +
                             $"This is a {matchResult.UserRoleName} fingerprint.\n" +
                             $"⛔ Only students are allowed to time in and time out.\n\n" +
                             dashboardText
                });
            }

            // STEP 3C: Valid student found
            var student = matchResult.Student!;
            _logger.LogInformation($"Student identified: {student.StudNumber}");

            // STEP 4: Check current schedule - Find active class
            var currentTime = DateTime.Now;
            var currentDay = currentTime.ToString("dddd");
            var currentTimeOnly = currentTime.TimeOfDay;

            _logger.LogInformation($"Checking schedule for {currentDay} at {currentTimeOnly}...");

            var activeClassInfo = await GetActiveClassForStudentAsync(student.StudId, currentDay, currentTimeOnly);

            if (activeClassInfo == null)
            {
                _logger.LogWarning($"No class scheduled for student {student.StudNumber} on {currentDay} at {currentTimeOnly}");
                
                // Error feedback: red light + beep
                _fingerprintService.SetLedLight("red", true);
                _fingerprintService.Beep(true);
                _ = Task.Delay(500).ContinueWith(_ =>
                {
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                });
                
                // ✅ FIX: Include student's full name in "no class scheduled" error message
                var studentFullName = $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim();
                
                return Json(new
                {
                    success = false,
                    messageType = "no_schedule",
                    studentName = studentFullName, // ✅ NEW: Add student name for frontend display
                    message = $"📅 No class scheduled at this time.\n\n" +
                              $"Hello, {studentFullName}\n\n" +
                              $"You have no subject/course assigned for {currentDay} at {currentTime:h:mm tt}."
                });
            }

            _logger.LogInformation($"✅ Active class found: {activeClassInfo.CourseCode} - {activeClassInfo.CourseName} (Class ID: {activeClassInfo.ClassId})");

            // ✅ STEP 5: CHECK FOR EXISTING ATTENDANCE FOR THIS CLASS TODAY
            var existingAttendance = await _dailyAttendanceRepo.GetByStudentClassAndDateAsync(
                student.StudId, 
                activeClassInfo.ClassId, 
                currentTime.Date
            );

            if (existingAttendance != null)
            {
                // ✅ CASE 1: Already completed (both time in and time out recorded)
                if (existingAttendance.Status == "completed" && existingAttendance.TimeOut != null)
                {
                    _logger.LogWarning($"⚠️ BLOCKED: Student {student.StudNumber} already completed attendance for {activeClassInfo.CourseCode}");
                    
                    // Error feedback: red light + beep
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    _ = Task.Delay(1000).ContinueWith(_ =>
                    {
                        _fingerprintService.SetLedLight("red", false);
                        _fingerprintService.Beep(false);
                    });

                    return Json(new
                    {
                        success = false,
                        messageType = "already_completed",
                        studentName = $"{student.User?.UserFirstName} {student.User?.UserLastName}",
                        studentNumber = student.StudNumber,
                        courseName = $"{activeClassInfo.CourseCode} - {activeClassInfo.CourseName}",
                        facultyName = activeClassInfo.FacultyName,
                        timeIn = existingAttendance.TimeIn?.ToString("hh:mm tt"),
                        timeOut = existingAttendance.TimeOut?.ToString("hh:mm tt"),
                        message = $"⚠️ Attendance Already Completed\n\n" +
                                 $"Hello, {student.User?.UserFirstName} {student.User?.UserLastName}\n\n" +
                                 $"You have already completed your attendance for:\n" +
                                 $"📚 {activeClassInfo.CourseCode} - {activeClassInfo.CourseName}\n" +
                                 $"👨‍🏫 Prof. {activeClassInfo.FacultyName}\n\n" +
                                 $"✅ Time In: {existingAttendance.TimeIn:hh:mm tt}\n" +
                                 $"✅ Time Out: {existingAttendance.TimeOut:hh:mm tt}\n\n" +
                                 $"You cannot time in again for this class today."
                    });
                }
                
                // ✅ CASE 2: Active time in exists (needs time out)
                if (existingAttendance.Status == "active" && existingAttendance.TimeOut == null)
                {
                    _logger.LogInformation($"Processing TIME OUT for {activeClassInfo.CourseCode}");
                    
                    existingAttendance.TimeOut = currentTime;
                    existingAttendance.Status = "completed";
                    existingAttendance.UpdatedAt = DateTime.Now;
                    
                    var duration = currentTime - existingAttendance.TimeIn.Value;
                    var durationText = duration.TotalHours >= 1 
                        ? $"{(int)duration.TotalHours} hours and {duration.Minutes} minutes"
                        : $"{duration.Minutes} minutes and {duration.Seconds} seconds";
                    
                    existingAttendance.Remarks = $"Class duration: {durationText}";
                    
                    await _dailyAttendanceRepo.UpdateAsync(existingAttendance);
                    
                    // Success feedback: green light + beep
                    _fingerprintService.SetLedLight("green", true);
                    _fingerprintService.Beep(true);
                    _ = Task.Delay(300).ContinueWith(_ =>
                    {
                        _fingerprintService.SetLedLight("green", false);
                        _fingerprintService.Beep(false);
                    });

                    return Json(new
                    {
                        success = true,
                        action = "time_out",
                        timestamp = currentTime,
                        message = $"Goodbye, {student.User?.UserFirstName}! Time Out recorded. You attended {activeClassInfo.CourseCode} for {durationText}.",
                        student = new
                        {
                            studId = student.StudId,
                            studNumber = student.StudNumber,
                            fullName = $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim(),
                            programName = student.Program?.ProgramName,
                            sectionName = student.Section?.SectionName,
                            yearLevel = student.StudYearLevel
                        },
                        currentClass = new
                        {
                            courseCode = activeClassInfo.CourseCode,
                            courseName = activeClassInfo.CourseName,
                            facultyName = activeClassInfo.FacultyName,
                            roomCode = activeClassInfo.RoomCode,
                            schedule = activeClassInfo.Schedule
                        }
                    });
                }
            }

            // ✅ STEP 6: No existing record - Process TIME IN
            _logger.LogInformation($"Processing TIME IN for {activeClassInfo.CourseCode}");
            
            var processingResult = await _attendanceService.ProcessAttendanceAsync(student, activeClassInfo.ClassId);

            if (!processingResult.Success)
            {
                _logger.LogWarning($"Attendance processing failed: {processingResult.Error}");
                
                // Error feedback: red light + beep
                _fingerprintService.SetLedLight("red", true);
                _fingerprintService.Beep(true);
                _ = Task.Delay(500).ContinueWith(_ =>
                {
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                });
                
                return Json(new
                {
                    success = false,
                    messageType = "processing_error",
                    message = GetUserFriendlyErrorMessage(processingResult.ErrorType, processingResult.Error)
                });
            }

            var attendanceResponse = processingResult.AttendanceResponse;

            if (attendanceResponse == null)
            {
                return Json(new
                {
                    success = false,
                    messageType = "unknown_error",
                    message = "Unable to process attendance. Please try again."
                });
            }

            _logger.LogInformation($"✅ Attendance recorded for {student.StudNumber}: {attendanceResponse.Action}");

            // Success feedback: green light + beep
            _fingerprintService.SetLedLight("green", true);
            _fingerprintService.Beep(true);
            _ = Task.Delay(300).ContinueWith(_ =>
            {
                _fingerprintService.SetLedLight("green", false);
                _fingerprintService.Beep(false);
            });

            return Json(new
            {
                success = true,
                message = attendanceResponse.Message,
                action = attendanceResponse.Action,
                timestamp = attendanceResponse.Timestamp,
                student = new
                {
                    studId = student.StudId,
                    studNumber = student.StudNumber,
                    fullName = attendanceResponse.Student?.FullName ?? "Student",
                    programName = attendanceResponse.Student?.ProgramName,
                    sectionName = attendanceResponse.Student?.SectionName,
                    yearLevel = attendanceResponse.Student?.YearLevel
                },
                currentClass = new
                {
                    courseCode = activeClassInfo.CourseCode,
                    courseName = activeClassInfo.CourseName,
                    facultyName = activeClassInfo.FacultyName,
                    roomCode = activeClassInfo.RoomCode,
                    schedule = activeClassInfo.Schedule
                },
                incompleteRecordsWarning = attendanceResponse.IncompleteRecordsWarning
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during public fingerprint scan");
            return Json(new
            {
                success = false,
                messageType = "exception",
                message = "An error occurred during scanning. Please try again."
            });
        }
    }

    /// <summary>
    /// Match captured fingerprint against database with role checking
    /// Returns student if role is student, otherwise returns user info for error message
    /// </summary>
    private async Task<FingerprintMatchResult> MatchFingerprintWithRoleCheckAsync(byte[] capturedTemplate)
    {
        const int MATCH_THRESHOLD = 50;

        var allTemplates = await _fingerprintRepo.GetAllAsync();
        _logger.LogInformation($"Comparing against {allTemplates.Count()} registered fingerprints");

        foreach (var template in allTemplates)
        {
            try
            {
                int score = _fingerprintService.GetMatchScore(capturedTemplate, template.FtempData);

                if (score >= MATCH_THRESHOLD)
                {
                    _logger.LogInformation($"Match found - User ID: {template.UserId}, Score: {score}");

                    var user = await _fingerprintRepo.GetUserByTemplateAsync(template.UserId);
                    
                    if (user == null)
                    {
                        _logger.LogWarning($"User not found for template UserId: {template.UserId}");
                        continue;
                    }

                    // Get role name
                    string roleName = user.RoleId switch
                    {
                        1 => "Admin",
                        2 => "Faculty",
                        3 => "Student",
                        _ => "Unknown"
                    };

                    // If student role, get student record
                    if (user.RoleId == 3)
                    {
                        var student = await _studentRepo.GetByUserIdAsync(user.UserId);
                        if (student != null)
                        {
                            return new FingerprintMatchResult 
                            { 
                                Student = student, 
                                User = user,
                                UserRoleName = "Student"
                            };
                        }
                    }
                    
                    // Non-student role or student record not found
                    return new FingerprintMatchResult 
                    { 
                        Student = null, 
                        User = user,
                        UserRoleName = roleName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error matching template for User ID: {template.UserId}");
                continue;
            }
        }

        // No match found
        return new FingerprintMatchResult 
        { 
            Student = null, 
            User = null,
            UserRoleName = null
        };
    }

    /// <summary>
    /// Legacy method maintained for backward compatibility
    /// </summary>
    private async Task<StudentModel?> MatchFingerprintAsync(byte[] capturedTemplate)
    {
        var result = await MatchFingerprintWithRoleCheckAsync(capturedTemplate);
        return result.Student;
    }

    /// <summary>
    /// Result of fingerprint matching with role information
    /// </summary>
    private class FingerprintMatchResult
    {
        public StudentModel? Student { get; set; }
        public User? User { get; set; }
        public string? UserRoleName { get; set; }
    }

    /// <summary>
    /// Check if student has a scheduled class at the current time
    /// </summary>
    private async Task<bool> HasScheduledClassNowAsync(int studId, string dayOfWeek, TimeSpan currentTime)
    {
        try
        {
            // Get all student's enrolled classes
            var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
            var classIds = enrollments.Select(e => e.ClassId).ToList();

            if (!classIds.Any())
            {
                _logger.LogWarning($"Student {studId} has no enrollments");
                return false;
            }

            _logger.LogInformation($"Checking {classIds.Count} enrolled classes for schedule match");

            // Check if any class is scheduled for this day/time
            foreach (var classId in classIds)
            {
                var schedules = await _classScheduleRepo.GetByClassIdAsync(classId);
                
                foreach (var schedule in schedules)
                {
                    _logger.LogDebug($"Checking schedule: Day={schedule.SchedDayOfWeek}, Start={schedule.SchedStartTime}, End={schedule.SchedEndTime}");
                    
                    // Match day of week (case-insensitive, trimmed)
                    bool dayMatch = schedule.SchedDayOfWeek.Trim().Equals(dayOfWeek.Trim(), StringComparison.OrdinalIgnoreCase);
                    
                    if (dayMatch)
                    {
                        // Check if current time is within class time
                        if (currentTime >= schedule.SchedStartTime && currentTime <= schedule.SchedEndTime)
                        {
                            _logger.LogInformation($"✅ Found scheduled class: ClassId={classId}, Day={schedule.SchedDayOfWeek}, " +
                                                 $"Time={schedule.SchedStartTime}-{schedule.SchedEndTime}");
                            return true;
                        }
                        else
                        {
                            _logger.LogDebug($"Time mismatch: Current={currentTime} not in range {schedule.SchedStartTime}-{schedule.SchedEndTime}");
                        }
                    }
                }
            }

            _logger.LogWarning($"❌ No scheduled class found for student {studId} on {dayOfWeek} at {currentTime:hh\\:mm}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking schedule for student {studId}");
            return false;
        }
    }

    /// <summary>
    /// Get today's attendance statistics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var summary = await _attendanceService.GetTodaySummaryAsync();

            // Calculate success rate
            decimal successRate = summary.TotalStudents > 0
                ? Math.Round((decimal)summary.TimedIn / summary.TotalStudents * 100, 1)
                : 0;

            return Json(new
            {
                success = true,
                successRate = successRate,
                todayScans = summary.TimedIn + summary.TimedOut
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return Json(new
            {
                success = true,
                successRate = 0,
                todayScans = 0
            });
        }
    }

    /// <summary>
    /// Convert technical error to user-friendly message
    /// </summary>
    private string GetUserFriendlyErrorMessage(string? errorType, string? error)
    {
        return errorType switch
        {
            "ScannerNotAvailableException" => "⚠️ Scanner not available. Please check device connection.",
            "FingerprintNotRecognizedException" => "🔍 Fingerprint not recognized. This fingerprint is not registered in the system.",
            "NoScheduleException" => "📅 No classes scheduled. You are not enrolled in any classes for today.",
            "TimeoutException" => "⏱️ Scan timeout. Please place your finger on the scanner and try again.",
            _ => error ?? "❌ An error occurred. Please try again."
        };
    }

    /// <summary>
    /// ✅ NEW: Get active class for student at current time with all details
    /// Returns class information if student has a scheduled class right now
    /// HANDLES MIDNIGHT-CROSSING SCHEDULES (e.g., 4:00 PM - 12:00 AM)
    /// </summary>
    private async Task<ActiveClassInfo?> GetActiveClassForStudentAsync(int studId, string dayOfWeek, TimeSpan currentTime)
    {
        try
        {
            // ✅ ENHANCED: Use InvariantCulture to ensure consistent day-of-week comparison
            var currentDayFormatted = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(dayOfWeek.ToLower());
            
            _logger.LogInformation($"[SCHEDULE DEBUG] ===== START SCHEDULE CHECK =====");
            _logger.LogInformation($"[SCHEDULE DEBUG] Student ID: {studId}");
            _logger.LogInformation($"[SCHEDULE DEBUG] Current Day: '{currentDayFormatted}' (original: '{dayOfWeek}')");
            _logger.LogInformation($"[SCHEDULE DEBUG] Current Time: {currentTime:hh\\:mm\\:ss} ({currentTime.TotalSeconds} seconds)");
            
            var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
            var classIds = enrollments.Select(e => e.ClassId).ToList();

            if (!classIds.Any())
            {
                _logger.LogWarning($"[SCHEDULE DEBUG] Student {studId} has no enrollments");
                return null;
            }

            _logger.LogInformation($"[SCHEDULE DEBUG] Found {classIds.Count} enrolled class(es)");

            foreach (var classId in classIds)
            {
                var schedules = await _classScheduleRepo.GetByClassIdAsync(classId);
                
                _logger.LogInformation($"[SCHEDULE DEBUG] Class ID {classId}: {schedules.Count()} schedule(s) found");
                
                foreach (var schedule in schedules)
                {
                    _logger.LogInformation($"[SCHEDULE DEBUG] ----- Schedule Entry -----");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Day: '{schedule.SchedDayOfWeek}' (trimmed: '{schedule.SchedDayOfWeek.Trim()}')");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Start: {schedule.SchedStartTime:hh\\:mm\\:ss} ({schedule.SchedStartTime.TotalSeconds}s)");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   End: {schedule.SchedEndTime:hh\\:mm\\:ss} ({schedule.SchedEndTime.TotalSeconds}s)");
                    
                    // ✅ ENHANCED: Case-insensitive, culture-invariant day comparison
                    bool dayMatch = string.Equals(
                        schedule.SchedDayOfWeek.Trim(), 
                        currentDayFormatted, 
                        StringComparison.OrdinalIgnoreCase
                    );
                    
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Day Match: {dayMatch} ('{schedule.SchedDayOfWeek.Trim()}' vs '{currentDayFormatted}')");
                    
                    if (!dayMatch)
                    {
                        _logger.LogDebug($"[SCHEDULE DEBUG]   SKIPPED: Day mismatch");
                        continue;
                    }
                    
                    bool timeMatch = false;
                    
                    // ✅ ENHANCED: Handle midnight-crossing schedules AND 23:00 (11 PM) schedules
                    // 23:00:00 is LESS than 24:00:00 (midnight), so it's a normal schedule
                    if (schedule.SchedEndTime < schedule.SchedStartTime || schedule.SchedEndTime == TimeSpan.Zero)
                    {
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Schedule Type: MIDNIGHT-CROSSING");
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Checking: {currentTime:hh\\:mm\\:ss} >= {schedule.SchedStartTime:hh\\:mm\\:ss} OR <= {schedule.SchedEndTime:hh\\:mm\\:ss}");
                        
                        timeMatch = currentTime >= schedule.SchedStartTime || currentTime <= schedule.SchedEndTime;
                        
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Part A ({currentTime:hh\\:mm\\:ss} >= {schedule.SchedStartTime:hh\\:mm\\:ss}): {currentTime >= schedule.SchedStartTime}");
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Part B ({currentTime:hh\\:mm\\:ss} <= {schedule.SchedEndTime:hh\\:mm\\:ss}): {currentTime <= schedule.SchedEndTime}");
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Combined (A OR B): {timeMatch}");
                    }
                    else
                    {
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Schedule Type: NORMAL");
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Checking: {currentTime:hh\\:mm\\:ss} >= {schedule.SchedStartTime:hh\\:mm\\:ss} AND <= {schedule.SchedEndTime:hh\\:mm\\:ss}");
                        
                        // ✅ CRITICAL FIX: For 22:00 - 23:00, current time 22:09 should match
                        bool afterStart = currentTime >= schedule.SchedStartTime;
                        bool beforeEnd = currentTime <= schedule.SchedEndTime;
                        
                        _logger.LogInformation($"[SCHEDULE DEBUG]   After Start: {afterStart} ({currentTime.TotalSeconds} >= {schedule.SchedStartTime.TotalSeconds})");
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Before End: {beforeEnd} ({currentTime.TotalSeconds} <= {schedule.SchedEndTime.TotalSeconds})");
                        
                        timeMatch = afterStart && beforeEnd;
                        
                        _logger.LogInformation($"[SCHEDULE DEBUG]   Combined (After AND Before): {timeMatch}");
                    }
                    
                    if (dayMatch && timeMatch)
                    {
                        _logger.LogInformation($"[SCHEDULE DEBUG] ✅✅✅ MATCH FOUND! Class {classId} ✅✅✅");
                        
                        // Get full class details
                        var classDetail = await _classRepo.GetByIdAsync(classId);
                        if (classDetail == null)
                        {
                            _logger.LogWarning($"[SCHEDULE DEBUG] Class details not found for Class ID {classId}");
                            continue;
                        }
                        
                        var course = classDetail.CourseId.HasValue 
                            ? await _courseRepo.GetByIdAsync(classDetail.CourseId.Value) 
                            : null;
                        
                        string facultyName = "TBA";
                        if (classDetail.FacId.HasValue)
                        {
                            var faculty = await _facultyRepo.GetByIdAsync(classDetail.FacId.Value);
                            if (faculty != null)
                            {
                                var facultyUser = await _userRepo.GetByIdAsync(faculty.UserId);
                                if (facultyUser != null)
                                {
                                    facultyName = $"{facultyUser.UserFirstName} {facultyUser.UserLastName}";
                                }
                            }
                        }
                        
                        var room = classDetail.RoomId.HasValue 
                            ? await _roomRepo.GetByIdAsync(classDetail.RoomId.Value) 
                            : null;
                        
                        var activeClass = new ActiveClassInfo
                        {
                            ClassId = classId,
                            CourseCode = course?.CourseCode ?? "N/A",
                            CourseName = course?.CourseName ?? "N/A",
                            FacultyName = facultyName,
                            RoomCode = room?.RoomCode ?? "TBA",
                            Schedule = $"{schedule.SchedDayOfWeek} {schedule.SchedStartTime:hh\\:mm tt}-{schedule.SchedEndTime:hh\\:mm tt}"
                        };
                        
                        _logger.LogInformation($"[SCHEDULE DEBUG] Returning: {activeClass.CourseCode} - {activeClass.CourseName} ({activeClass.FacultyName})");
                        _logger.LogInformation($"[SCHEDULE DEBUG] ===== END SCHEDULE CHECK (SUCCESS) =====");
                        return activeClass;
                    }
                    else
                    {
                        _logger.LogInformation($"[SCHEDULE DEBUG]   NO MATCH: dayMatch={dayMatch}, timeMatch={timeMatch}");
                    }
                }
            }
            
            _logger.LogWarning($"[SCHEDULE DEBUG] ❌ NO MATCHING CLASS FOUND for {currentDayFormatted} at {currentTime:hh\\:mm\\:ss}");
            _logger.LogInformation($"[SCHEDULE DEBUG] ===== END SCHEDULE CHECK (NO MATCH) =====");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[SCHEDULE DEBUG] EXCEPTION in GetActiveClassForStudentAsync");
            return null;
        }
    }

    /// <summary>
    /// ✅ NEW: Helper class for active class information
    /// </summary>
    private class ActiveClassInfo
    {
        public int ClassId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
