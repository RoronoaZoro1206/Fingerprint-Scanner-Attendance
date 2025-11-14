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
    private readonly AttendanceProcessingService _attendanceService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        FingerprintService fingerprintService,
        IFingerprintTemplateRepository fingerprintRepo,
        IStudentRepository studentRepo,
        AttendanceProcessingService attendanceService,
        ILogger<HomeController> logger)
    {
        _fingerprintService = fingerprintService;
        _fingerprintRepo = fingerprintRepo;
        _studentRepo = studentRepo;
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
    /// Process public fingerprint scan for attendance
    /// POST: /Home/ScanFingerprint
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ScanFingerprint()
    {
        try
        {
            _logger.LogInformation("=== Public Attendance Scan Started ===");

            // STEP 1: Process attendance using AttendanceProcessingService
            var processingResult = await _attendanceService.ProcessAttendanceAsync();

            if (!processingResult.Success)
            {
                _logger.LogWarning($"Attendance processing failed: {processingResult.Error}");

                return Json(new
                {
                    success = false,
                    message = GetUserFriendlyErrorMessage(processingResult.ErrorType, processingResult.Error)
                });
            }

            // STEP 2: Build response with student info
            var student = processingResult.Student;
            var attendanceResponse = processingResult.AttendanceResponse;

            if (student == null || attendanceResponse == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to process attendance. Please try again."
                });
            }

            _logger.LogInformation($"? Public attendance recorded for {student.StudNumber}");

            // STEP 3: Return success response
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
            "ScannerNotAvailableException" => "?? Scanner not available. Please check device connection.",
            "FingerprintNotRecognizedException" => "?? Fingerprint not recognized. This fingerprint is not registered in the system.",
            "NoScheduleException" => "?? No classes scheduled. You are not enrolled in any classes for today.",
            "TimeoutException" => "?? Scan timeout. Please place your finger on the scanner and try again.",
            _ => error ?? "? An error occurred. Please try again."
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}