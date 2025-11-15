using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Home;

public class HomeController : Controller
{
    private readonly FingerprintService _fingerprintService;
    private readonly IFingerprintTemplateRepository _fingerprintRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IClassScheduleRepository _classScheduleRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly AttendanceProcessingService _attendanceService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        FingerprintService fingerprintService,
        IFingerprintTemplateRepository fingerprintRepo,
        IStudentRepository studentRepo,
        IClassScheduleRepository classScheduleRepo,
        IEnrollmentRepository enrollmentRepo,
        AttendanceProcessingService attendanceService,
        ILogger<HomeController> logger)
    {
        _fingerprintService = fingerprintService;
        _fingerprintRepo = fingerprintRepo;
        _studentRepo = studentRepo;
        _classScheduleRepo = classScheduleRepo;
        _enrollmentRepo = enrollmentRepo;
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
                    message = string.IsNullOrEmpty(errorMessage) 
                        ? "⚠️ Failed to capture fingerprint. Please try again." 
                        : $"⚠️ {errorMessage}"
                });
            }

            // STEP 3: Match fingerprint against database
            _logger.LogInformation("Matching fingerprint against database...");
            var student = await MatchFingerprintAsync(template);

            if (student == null)
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
                    message = "🔍 Fingerprint not recognized. This fingerprint is not registered in the system."
                });
            }

            _logger.LogInformation($"Student identified: {student.StudNumber}");

            // STEP 4: Check current schedule - INTELLIGENT VALIDATION
            var currentTime = DateTime.Now;
            var currentDay = currentTime.ToString("dddd"); // e.g., "Monday"
            var currentTimeOnly = currentTime.TimeOfDay;

            _logger.LogInformation($"Checking schedule for {currentDay} at {currentTimeOnly}...");

            var hasScheduledClass = await HasScheduledClassNowAsync(student.StudId, currentDay, currentTimeOnly);

            if (!hasScheduledClass)
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
                
                return Json(new
                {
                    success = false,
                    message = $"📅 No class scheduled at this time.<br>" +
                              $"You have no subject/course assigned for {currentDay} at {currentTime:h:mm tt}."
                });
            }

            // STEP 5: Process attendance (Time In/Out) - Schedule validated!
            _logger.LogInformation("Schedule validated. Processing attendance...");
            var processingResult = await _attendanceService.ProcessAttendanceAsync();

            if (!processingResult.Success)
            {
                _logger.LogWarning($"Attendance processing failed: {processingResult.Error}");
                
                // Error feedback
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
                    message = GetUserFriendlyErrorMessage(processingResult.ErrorType, processingResult.Error)
                });
            }

            var attendanceResponse = processingResult.AttendanceResponse;

            if (attendanceResponse == null)
            {
                return Json(new
                {
                    success = false,
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

            // STEP 6: Return success response
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
                    sectionName = attendanceResponse.Student?.SectionName
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
                message = "An error occurred during scanning. Please try again."
            });
        }
    }

    /// <summary>
    /// Match captured fingerprint against database
    /// </summary>
    private async Task<Student?> MatchFingerprintAsync(byte[] capturedTemplate)
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
                    if (user?.RoleId == 3) // Student role
                    {
                        var student = await _studentRepo.GetByUserIdAsync(user.UserId);
                        if (student != null)
                        {
                            return student;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error matching template for User ID: {template.UserId}");
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if student has a scheduled class at the current time
    /// INTELLIGENT SCHEDULE VALIDATION
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

            // Check if any class is scheduled for this day/time
            foreach (var classId in classIds)
            {
                var schedules = await _classScheduleRepo.GetByClassIdAsync(classId);
                
                foreach (var schedule in schedules)
                {
                    // Match day of week
                    if (schedule.SchedDayOfWeek.Trim().Equals(dayOfWeek.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if current time is within class time
                        if (currentTime >= schedule.SchedStartTime && currentTime <= schedule.SchedEndTime)
                        {
                            _logger.LogInformation($"✅ Found scheduled class: ClassId={classId}, Day={schedule.SchedDayOfWeek}, " +
                                                 $"Time={schedule.SchedStartTime}-{schedule.SchedEndTime}");
                            return true;
                        }
                    }
                }
            }

            _logger.LogInformation($"No scheduled class found for student {studId} on {dayOfWeek} at {currentTime}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking schedule for student {studId}");
            // On error, allow attendance (fail-safe approach)
            return true;
        }
    }

    /// <summary>
    /// Get today's attendance statistics
    /// GET: /Home/GetStatistics
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
