using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using StudentModel = Fingerprint_Attendance_System.Models.Student;

namespace Fingerprint_Attendance_System.Controllers.Home
{
    public class HomeController : Controller
    {
        private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IClassScheduleRepository _classScheduleRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo;
        private readonly IClassRepository _classRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly IFacultyRepository _facultyRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly IUserRepository _userRepo;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly AttendanceProcessingService _attendanceService;
        private readonly IScanLogRepository _scanLogRepo;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            FingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IStudentRepository studentRepo,
            IClassScheduleRepository classScheduleRepo,
            IEnrollmentRepository enrollmentRepo,
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IClassRepository classRepo,
            ICourseRepository courseRepo,
            IFacultyRepository facultyRepo,
            IRoomRepository roomRepo,
            IUserRepository userRepo,
            IAttendanceRepository attendanceRepository,
            AttendanceProcessingService attendanceService,
            IScanLogRepository scanLogRepo,
            ILogger<HomeController> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _studentRepo = studentRepo;
            _classScheduleRepo = classScheduleRepo;
            _enrollmentRepo = enrollmentRepo;
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _classRepo = classRepo;
            _courseRepo = courseRepo;
            _facultyRepo = facultyRepo;
            _roomRepo = roomRepo;
            _userRepo = userRepo;
            _attendanceRepository = attendanceRepository;
            _attendanceService = attendanceService;
            _scanLogRepo = scanLogRepo;
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
                    
                    // ✅ FIX ISSUE #2: Build complete student name including middle name if present
                    var studentFirstName = student.User?.UserFirstName ?? "";
                    var studentMiddleName = student.User?.UserMiddleName ?? "";
                    var studentLastName = student.User?.UserLastName ?? "";
                    
                    // Format full name: "FirstName MiddleName LastName" (only include middle if present)
                    var studentFullName = string.IsNullOrWhiteSpace(studentMiddleName)
                        ? $"{studentFirstName} {studentLastName}".Trim()
                        : $"{studentFirstName} {studentMiddleName} {studentLastName}".Trim();
                    
                    _logger.LogInformation($"✅ FIX ISSUE #2: Sending student name '{studentFullName}' to frontend");
                    
                    return Json(new
                    {
                        success = false,
                        messageType = "no_schedule",
                        studentName = studentFullName, // ✅ FIX ISSUE #2: Now includes first, middle (if present), and last name
                        message = $"📅 No class scheduled at this time.\n\n" +
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
                        
                        // ✅ NEW: Get class schedule to check if class has ended
                        var classSchedules = await _classScheduleRepo.GetByClassIdAsync(activeClassInfo.ClassId);
                        var todaySchedule = classSchedules.FirstOrDefault(s => 
                            s.SchedDayOfWeek.Trim().Equals(currentDay, StringComparison.OrdinalIgnoreCase));
                        
                        if (todaySchedule != null)
                        {
                            var classEndTime = todaySchedule.SchedEndTime;
                            
                            // ✅ NEW: Check if current time is after class end time
                            if (currentTimeOnly > classEndTime)
                            {
                                _logger.LogWarning($"⚠️ BLOCKED: Class has ended. Student {student.StudNumber} cannot time out for {activeClassInfo.CourseCode}");
                                
                                // Error feedback: red light + beep
                                _fingerprintService.SetLedLight("red", true);
                                _fingerprintService.Beep(true);
                                _ = Task.Delay(1000).ContinueWith(_ =>
                                {
                                    _fingerprintService.SetLedLight("red", false);
                                    _fingerprintService.Beep(false);
                                });
                                
                                var classEndTimeFormatted = DateTime.Today.Add(classEndTime).ToString("hh:mm tt");

                                return Json(new
                                {
                                    success = false,
                                    messageType = "class_ended",
                                    studentName = $"{student.User?.UserFirstName} {student.User?.UserLastName}",
                                    studentNumber = student.StudNumber,
                                    courseName = $"{activeClassInfo.CourseCode} - {activeClassInfo.CourseName}",
                                    facultyName = activeClassInfo.FacultyName,
                                    classEndTime = classEndTimeFormatted,
                                    timeIn = existingAttendance.TimeIn?.ToString("hh:mm tt"),
                                    message = $"⚠️ Class Has Ended - Cannot Time Out\n\n" +
                                             $"Hello, {student.User?.UserFirstName} {student.User?.UserLastName}\n\n" +
                                             $"The class has already ended:\n" +
                                             $"📚 {activeClassInfo.CourseCode} - {activeClassInfo.CourseName}\n" +
                                             $"👨‍🏫 Prof. {activeClassInfo.FacultyName}\n\n" +
                                             $"🕒 Class ended at: {classEndTimeFormatted}\n" +
                                             $"✅ Your Time In: {existingAttendance.TimeIn:hh:mm tt}\n\n" +
                                             $"⚠️ You forgot to time out before the class ended.\n" +
                                             $"Your attendance has been automatically marked as ABSENT.\n\n" +
                                             $"Please contact your faculty if you need to appeal this status."
                                });
                            }
                        }
                        
                        existingAttendance.TimeOut = currentTime;
                        existingAttendance.Status = "completed";
                        existingAttendance.UpdatedAt = DateTime.Now;
                        
                        var duration = currentTime - existingAttendance.TimeIn.Value;
                        var durationText = duration.TotalHours >= 1 
                            ? $"{(int)duration.TotalHours} hours and {duration.Minutes} minutes"
                            : $"{duration.Minutes} minutes and {duration.Seconds} seconds";
                        
                        existingAttendance.Remarks = $"Class duration: {durationText}";
                        
                        await _dailyAttendanceRepo.UpdateAsync(existingAttendance);
                        
                        // ✅ NEW: Dual-write time out to attendance table
                        try
                        {
                            await _attendanceRepository.SyncAttendanceFromDailyLogAsync(
                                existingAttendance.DailyAttendId,
                                activeClassInfo.ClassId,
                                student.StudId,
                                currentTime.Date,
                                currentTime,
                                ScanType.Out
                            );
                            
                            _logger.LogInformation($"✅ Synced time out to attendance table for faculty tracking");
                        }
                        catch (Exception syncEx)
                        {
                            _logger.LogWarning(syncEx, "Failed to sync time out to attendance table (non-critical)");
                        }
                        
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

                // ✅ NEW: Dual-write to attendance table for faculty tracking
                try
                {
                    var dailyAttendLog = await _dailyAttendanceRepo.GetByStudentClassAndDateAsync(
                        student.StudId, 
                        activeClassInfo.ClassId, 
                        currentTime.Date
                    );
                    
                    if (dailyAttendLog != null)
                    {
                        await _attendanceRepository.SyncAttendanceFromDailyLogAsync(
                            dailyAttendLog.DailyAttendId,
                            activeClassInfo.ClassId,
                            student.StudId,
                            currentTime.Date,
                            currentTime,
                            ScanType.In
                        );
                        
                        _logger.LogInformation($"✅ Synced attendance record to attendance table for faculty tracking");
                    }
                }
                catch (Exception syncEx)
                {
                    _logger.LogWarning(syncEx, "Failed to sync attendance to attendance table (non-critical)");
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
                                // ✅ FIX: Populate student.User navigation property immediately
                                // This ensures student name is available for all error messages
                                student.User = user;
                                
                                _logger.LogInformation($"Student retrieved: {student.StudNumber}, Name: {user.UserFirstName} {user.UserMiddleName} {user.UserLastName}");
                                
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
                var today = DateTime.Today;
                var allScans = await _scanLogRepo.GetAllAsync();
                
                // Filter today's scans
                var todayScans = allScans.Where(log => log.ScanTime.Date == today).ToList();
                
                // Calculate statistics
                int totalScans = todayScans.Count;
                int successfulScans = todayScans.Count(log => log.ScanSuccess == true);
                int failedScans = totalScans - successfulScans;
                
                decimal successRate = totalScans > 0 
                    ? Math.Round((decimal)successfulScans / totalScans * 100, 0) 
                    : 0;

                return Json(new
                {
                    success = true,
                    successRate = (int)successRate,
                    todayScans = totalScans
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return Json(new { success = false, successRate = 0, todayScans = 0 });
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
        /// ✅ CRITICAL BUG FIX: Get active class for student at current time with all details
        /// Returns class information if student has a scheduled class right now
        /// HANDLES MIDNIGHT-CROSSING SCHEDULES (e.g., 4:00 PM - 12:00 AM)
        /// ✅ ENHANCED: Saturday 1:50 PM bug fix - comprehensive day/time matching
        /// ✅ NEW: Added extensive logging for debugging schedule detection issues
        /// </summary>
        private async Task<ActiveClassInfo?> GetActiveClassForStudentAsync(int studId, string dayOfWeek, TimeSpan currentTime)
        {
            try
            {
                // ✅ CRITICAL FIX: Normalize day name to match database format
                // Database stores: "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
                var currentDayFormatted = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(dayOfWeek.Trim().ToLower());
                
                _logger.LogInformation($"[SCHEDULE DEBUG] ===== START SCHEDULE CHECK =====");
                _logger.LogInformation($"[SCHEDULE DEBUG] Student ID: {studId}");
                _logger.LogInformation($"[SCHEDULE DEBUG] Current Day: '{currentDayFormatted}' (original: '{dayOfWeek}')");
                _logger.LogInformation($"[SCHEDULE DEBUG] Current Time: {currentTime:hh\\:mm\\:ss} ({currentTime.TotalSeconds} seconds = {currentTime.TotalHours:F2} hours)");
                _logger.LogInformation($"[SCHEDULE DEBUG] DateTime.Now: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ({DateTime.Now.DayOfWeek})");
                
                var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
                
                // ✅ CRITICAL FIX: Ensure enrollments is not null
                if (enrollments == null)
                {
                    _logger.LogError($"[SCHEDULE DEBUG] ❌ GetByStudentIdAsync returned NULL for Student {studId}");
                    return null;
                }
                
                var classIds = enrollments.Select(e => e.ClassId).ToList();

                if (!classIds.Any())
                {
                    _logger.LogWarning($"[SCHEDULE DEBUG] ❌ Student {studId} has no enrollments");
                    return null;
                }

                _logger.LogInformation($"[SCHEDULE DEBUG] Found {classIds.Count} enrolled class(es): [{string.Join(", ", classIds)}]");

                // ✅ CRITICAL FIX: Iterate through ALL classes and ALL schedules without early return
                var matchedClass = await FindFirstMatchingScheduleAsync(classIds, currentDayFormatted, dayOfWeek, currentTime);
                
                if (matchedClass != null)
                {
                    _logger.LogInformation($"[SCHEDULE DEBUG] ✅✅✅ SUCCESS: Found matching class! ✅✅✅");
                    _logger.LogInformation($"[SCHEDULE DEBUG] Course: {matchedClass.CourseCode} - {matchedClass.CourseName}");
                    _logger.LogInformation($"[SCHEDULE DEBUG] Faculty: {matchedClass.FacultyName}");
                    _logger.LogInformation($"[SCHEDULE DEBUG] Room: {matchedClass.RoomCode}");
                    _logger.LogInformation($"[SCHEDULE DEBUG] Schedule: {matchedClass.Schedule}");
                    _logger.LogInformation($"[SCHEDULE DEBUG] ===== END SCHEDULE CHECK (SUCCESS) =====");
                    return matchedClass;
                }
                
                _logger.LogError($"[SCHEDULE DEBUG] ❌❌❌ FAILED: NO MATCHING CLASS FOUND ❌❌❌");
                _logger.LogError($"[SCHEDULE DEBUG] Searched for: {currentDayFormatted} at {currentTime:hh\\:mm\\:ss}");
                _logger.LogError($"[SCHEDULE DEBUG] This should have matched IT203 (Saturday 12:00-16:00) if enrolled");
                _logger.LogInformation($"[SCHEDULE DEBUG] ===== END SCHEDULE CHECK (NO MATCH) =====");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SCHEDULE DEBUG] ❌ EXCEPTION in GetActiveClassForStudentAsync: {ex.Message}");
                _logger.LogError($"[SCHEDULE DEBUG] Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// ✅ ENHANCED: Find first matching schedule across all classes
        /// Separated for better maintainability and testing
        /// ✅ NEW: Added extensive logging for Saturday 1:50 PM debugging
        /// </summary>
        private async Task<ActiveClassInfo?> FindFirstMatchingScheduleAsync(
            List<int> classIds, 
            string currentDayFormatted, 
            string originalDay, 
            TimeSpan currentTime)
        {
            int totalSchedulesChecked = 0;
            
            foreach (var classId in classIds)
            {
                _logger.LogInformation($"[SCHEDULE DEBUG] ----- Checking Class ID {classId} -----");
                
                var schedules = await _classScheduleRepo.GetByClassIdAsync(classId);
                
                // ✅ CRITICAL FIX: Ensure schedules is not null
                if (schedules == null)
                {
                    _logger.LogWarning($"[SCHEDULE DEBUG] ⚠️ GetByClassIdAsync returned NULL for Class ID {classId}");
                    continue;
                }
                
                var schedulesList = schedules.ToList();
                
                _logger.LogInformation($"[SCHEDULE DEBUG] Class ID {classId}: {schedulesList.Count} schedule(s) found");
                
                if (!schedulesList.Any())
                {
                    _logger.LogWarning($"[SCHEDULE DEBUG] ⚠️ No schedules found for Class ID {classId} - SKIPPING");
                    continue;
                }
                
                // ✅ CRITICAL FIX: Check ALL schedules for this class before moving to next class
                foreach (var schedule in schedulesList)
                {
                    totalSchedulesChecked++;
                    
                    // ✅ CRITICAL FIX: Validate schedule object
                    if (schedule == null)
                    {
                        _logger.LogWarning($"[SCHEDULE DEBUG] ⚠️ NULL schedule entry for Class ID {classId} - SKIPPING");
                        continue;
                    }
                    
                    _logger.LogInformation($"[SCHEDULE DEBUG] ----- Schedule Entry #{totalSchedulesChecked} -----");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   SchedId: {schedule.SchedId}");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Day: '{schedule.SchedDayOfWeek}' (length: {schedule.SchedDayOfWeek?.Length ?? 0}, trimmed: '{schedule.SchedDayOfWeek?.Trim() ?? "NULL"}')");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Start: {schedule.SchedStartTime:hh\\:mm\\:ss} ({schedule.SchedStartTime.TotalSeconds}s = {schedule.SchedStartTime.TotalHours:F2}h)");
                    _logger.LogInformation($"[SCHEDULE DEBUG]   End: {schedule.SchedEndTime:hh\\:mm\\:ss} ({schedule.SchedEndTime.TotalSeconds}s = {schedule.SchedEndTime.TotalHours:F2}h)");
                    
                    // ✅ CRITICAL FIX: Comprehensive day matching
                    var dbDay = (schedule.SchedDayOfWeek ?? "").Trim();
                    
                    if (string.IsNullOrWhiteSpace(dbDay))
                    {
                        _logger.LogWarning($"[SCHEDULE DEBUG]   ❌ SKIPPED: Day of week is NULL or empty in database");
                        continue;
                    }
                    
                    // ✅ ENHANCED: Multiple comparison strategies for maximum compatibility
                    bool dayMatch = IsDayMatch(dbDay, currentDayFormatted, originalDay);
                    
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Day Match: {dayMatch}");
                    _logger.LogInformation($"[SCHEDULE DEBUG]     DB Day: '{dbDay}' vs Current: '{currentDayFormatted}'");
                    
                    if (!dayMatch)
                    {
                        _logger.LogDebug($"[SCHEDULE DEBUG]   ⚠️ SKIPPED: Day mismatch");
                        continue;
                    }
                    
                    // ✅ CRITICAL FIX: Time matching with midnight-crossing support
                    bool timeMatch = IsTimeMatch(schedule.SchedStartTime, schedule.SchedEndTime, currentTime);
                    
                    _logger.LogInformation($"[SCHEDULE DEBUG]   Time Match: {timeMatch}");
                    
                    if (dayMatch && timeMatch)
                    {
                        _logger.LogInformation($"[SCHEDULE DEBUG] ✅✅✅ MATCH FOUND! ✅✅✅");
                        _logger.LogInformation($"[SCHEDULE DEBUG] Class: {classId}, SchedId: {schedule.SchedId}");
                        _logger.LogInformation($"[SCHEDULE DEBUG] Day: {dbDay}, Time: {schedule.SchedStartTime:hh\\:mm}-{schedule.SchedEndTime:hh\\:mm}");
                        
                        // Get full class details
                        var classInfo = await BuildActiveClassInfoAsync(classId, schedule);
                        
                        if (classInfo != null)
                        {
                            _logger.LogInformation($"[SCHEDULE DEBUG] ✅ Returning class info successfully");
                            return classInfo;
                        }
                        else
                        {
                            _logger.LogError($"[SCHEDULE DEBUG] ❌ BuildActiveClassInfoAsync returned NULL!");
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"[SCHEDULE DEBUG]   ⚠️ NO MATCH: dayMatch={dayMatch}, timeMatch={timeMatch}");
                    }
                }
            }
            
            _logger.LogWarning($"[SCHEDULE DEBUG] ❌ Checked {totalSchedulesChecked} total schedule(s) across {classIds.Count} class(es) - NO MATCH FOUND");
            return null;
        }

        /// <summary>
        /// ✅ ENHANCED: Comprehensive day matching logic
        /// Handles various day name formats and case variations
        /// ✅ NEW: Added logging for Saturday 1:50 PM debugging
        /// </summary>
        private bool IsDayMatch(string dbDay, string currentDayFormatted, string originalDay)
        {
            _logger.LogDebug($"[SCHEDULE DEBUG]     Testing day match:");
            _logger.LogDebug($"[SCHEDULE DEBUG]       DB: '{dbDay}' (length: {dbDay.Length})");
            _logger.LogDebug($"[SCHEDULE DEBUG]       Formatted: '{currentDayFormatted}' (length: {currentDayFormatted.Length})");
            _logger.LogDebug($"[SCHEDULE DEBUG]       Original: '{originalDay}' (length: {originalDay.Length})");
            
            // Strategy 1: Direct case-insensitive comparison
            if (string.Equals(dbDay, currentDayFormatted, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug($"[SCHEDULE DEBUG]     ✅ Strategy 1 MATCHED: Direct comparison");
                return true;
            }
            
            // Strategy 2: Compare with original day name
            if (string.Equals(dbDay, originalDay, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug($"[SCHEDULE DEBUG]     ✅ Strategy 2 MATCHED: Original day comparison");
                return true;
            }
            
            // Strategy 3: Lowercase invariant comparison
            if (string.Equals(dbDay.ToLowerInvariant(), currentDayFormatted.ToLowerInvariant(), StringComparison.Ordinal))
            {
                _logger.LogDebug($"[SCHEDULE DEBUG]     ✅ Strategy 3 MATCHED: Lowercase invariant");
                return true;
            }
            
            // Strategy 4: Uppercase invariant comparison
            if (string.Equals(dbDay.ToUpperInvariant(), currentDayFormatted.ToUpperInvariant(), StringComparison.Ordinal))
            {
                _logger.LogDebug($"[SCHEDULE DEBUG]     ✅ Strategy 4 MATCHED: Uppercase invariant");
                return true;
            }
            
            // Strategy 5: Trimmed comparison (extra whitespace handling)
            if (string.Equals(dbDay.Trim().ToLowerInvariant(), currentDayFormatted.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            {
                _logger.LogDebug($"[SCHEDULE DEBUG]     ✅ Strategy 5 MATCHED: Trimmed comparison");
                return true;
            }
            
            _logger.LogDebug($"[SCHEDULE DEBUG]     ❌ No matching strategy found");
            return false;
        }

        /// <summary>
        /// ✅ ENHANCED: Time matching logic with midnight-crossing support
        /// Validates time values and handles edge cases
        /// ✅ NEW: Added extensive logging for Saturday 1:50 PM debugging
        /// </summary>
        private bool IsTimeMatch(TimeSpan schedStart, TimeSpan schedEnd, TimeSpan currentTime)
        {
            _logger.LogDebug($"[SCHEDULE DEBUG]     Testing time match:");
            _logger.LogDebug($"[SCHEDULE DEBUG]       Start: {schedStart:hh\\:mm\\:ss} ({schedStart.TotalSeconds}s)");
            _logger.LogDebug($"[SCHEDULE DEBUG]       End: {schedEnd:hh\\:mm\\:ss} ({schedEnd.TotalSeconds}s)");
            _logger.LogDebug($"[SCHEDULE DEBUG]       Current: {currentTime:hh\\:mm\\:ss} ({currentTime.TotalSeconds}s)");
            
            // ✅ CRITICAL FIX: Validate time values
            if (schedStart < TimeSpan.Zero || schedEnd < TimeSpan.Zero)
            {
                _logger.LogWarning($"[SCHEDULE DEBUG]     ❌ SKIPPED: Invalid negative time values");
                return false;
            }
            
            bool timeMatch = false;
            
            // ✅ CRITICAL FIX: Handle midnight-crossing schedules
            if (schedEnd < schedStart || schedEnd == TimeSpan.Zero)
            {
                _logger.LogInformation($"[SCHEDULE DEBUG]     Schedule Type: MIDNIGHT-CROSSING");
                _logger.LogInformation($"[SCHEDULE DEBUG]     Logic: currentTime >= start OR currentTime <= end");
                
                bool partA = currentTime >= schedStart;
                bool partB = currentTime <= schedEnd;
                timeMatch = partA || partB;
                
                _logger.LogInformation($"[SCHEDULE DEBUG]       Part A (>= start): {partA} ({currentTime.TotalSeconds}s >= {schedStart.TotalSeconds}s)");
                _logger.LogInformation($"[SCHEDULE DEBUG]       Part B (<= end): {partB} ({currentTime.TotalSeconds}s <= {schedEnd.TotalSeconds}s)");
                _logger.LogInformation($"[SCHEDULE DEBUG]       Combined (A OR B): {timeMatch}");
            }
            else
            {
                _logger.LogDebug($"[SCHEDULE DEBUG]     Schedule Type: NORMAL (within same day)");
                _logger.LogDebug($"[SCHEDULE DEBUG]     Logic: currentTime >= start AND currentTime <= end");
                
                bool afterStart = currentTime >= schedStart;
                bool beforeEnd = currentTime <= schedEnd;
                
                _logger.LogDebug($"[SCHEDULE DEBUG]       After Start: {afterStart} ({currentTime.TotalSeconds}s >= {schedStart.TotalSeconds}s)");
                _logger.LogDebug($"[SCHEDULE DEBUG]       Before End: {beforeEnd} ({currentTime.TotalSeconds}s <= {schedEnd.TotalSeconds}s)");
                
                timeMatch = afterStart && beforeEnd;
                
                _logger.LogDebug($"[SCHEDULE DEBUG]       Combined (After AND Before): {timeMatch}");
            }
            
            return timeMatch;
        }

        /// <summary>
        /// ✅ NEW HELPER METHOD: Build ActiveClassInfo with all related data
        /// Separated for better error handling and maintainability
        /// ✅ CRITICAL FIX: Proper TimeSpan formatting to prevent DateTime parsing errors
        /// </summary>
        private async Task<ActiveClassInfo?> BuildActiveClassInfoAsync(int classId, ClassSchedule schedule)
        {
            try
            {
                _logger.LogInformation($"[SCHEDULE DEBUG] Building ActiveClassInfo for Class ID {classId}");
                
                // Get class details
                var classDetail = await _classRepo.GetByIdAsync(classId);
                if (classDetail == null)
                {
                    _logger.LogWarning($"[SCHEDULE DEBUG] ❌ Class details not found for Class ID {classId}");
                    return null;
                }
                
                _logger.LogInformation($"[SCHEDULE DEBUG] ✅ Class details loaded: CourseId={classDetail.CourseId}, FacId={classDetail.FacId}, RoomId={classDetail.RoomId}");
                
                // Get course details
                var course = classDetail.CourseId.HasValue 
                    ? await _courseRepo.GetByIdAsync(classDetail.CourseId.Value) 
                    : null;
                
                _logger.LogInformation($"[SCHEDULE DEBUG] Course: {course?.CourseCode ?? "NULL"} - {course?.CourseName ?? "NULL"}");
                
                // Get faculty details
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
                
                _logger.LogInformation($"[SCHEDULE DEBUG] Faculty: {facultyName}");
                
                // Get room details
                var room = classDetail.RoomId.HasValue 
                    ? await _roomRepo.GetByIdAsync(classDetail.RoomId.Value) 
                    : null;
                
                _logger.LogInformation($"[SCHEDULE DEBUG] Room: {room?.RoomCode ?? "NULL"}");
                
                // ✅ CRITICAL FIX: Format TimeSpan correctly (use ToString(@"hh\:mm") instead of :hh\\:mm tt)
                // TimeSpan doesn't support AM/PM formatting - that's for DateTime only
                var startTimeFormatted = schedule.SchedStartTime.ToString(@"hh\:mm");
                var endTimeFormatted = schedule.SchedEndTime.ToString(@"hh\:mm");
                var scheduleText = $"{schedule.SchedDayOfWeek} {startTimeFormatted}-{endTimeFormatted}";
                
                _logger.LogInformation($"[SCHEDULE DEBUG] Schedule formatted: {scheduleText}");
                
                var result = new ActiveClassInfo
                {
                    ClassId = classId,
                    CourseCode = course?.CourseCode ?? "N/A",
                    CourseName = course?.CourseName ?? "N/A",
                    FacultyName = facultyName,
                    RoomCode = room?.RoomCode ?? "TBA",
                    Schedule = scheduleText
                };
                
                _logger.LogInformation($"[SCHEDULE DEBUG] ✅ Successfully built ActiveClassInfo");
                _logger.LogInformation($"[SCHEDULE DEBUG]   CourseCode: {result.CourseCode}");
                _logger.LogInformation($"[SCHEDULE DEBUG]   CourseName: {result.CourseName}");
                _logger.LogInformation($"[SCHEDULE DEBUG]   FacultyName: {result.FacultyName}");
                _logger.LogInformation($"[SCHEDULE DEBUG]   RoomCode: {result.RoomCode}");
                _logger.LogInformation($"[SCHEDULE DEBUG]   Schedule: {result.Schedule}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SCHEDULE DEBUG] ❌❌❌ CRITICAL ERROR building ActiveClassInfo for Class ID {classId}");
                _logger.LogError($"[SCHEDULE DEBUG] Exception Type: {ex.GetType().Name}");
                _logger.LogError($"[SCHEDULE DEBUG] Exception Message: {ex.Message}");
                _logger.LogError($"[SCHEDULE DEBUG] Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// ✅ NEW: Helper class for active class information
        /// Contains all details about the currently active class
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
}
