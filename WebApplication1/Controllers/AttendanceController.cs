using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using StudentModel = Fingerprint_Attendance_System.Models.Student;

namespace Fingerprint_Attendance_System.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly DailyAttendanceService _attendanceService;
        private readonly IScanLogRepository _scanLogRepo;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            FingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IStudentRepository studentRepo,
            DailyAttendanceService attendanceService,
            IScanLogRepository scanLogRepo,
            ILogger<AttendanceController> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _studentRepo = studentRepo;
            _attendanceService = attendanceService;
            _scanLogRepo = scanLogRepo;
            _logger = logger;
        }

        /// <summary>
        /// Check if scanner device is ready for attendance
        /// </summary>
        [HttpGet("CheckScannerStatus")]
        public IActionResult CheckScannerStatus()
        {
            try
            {
                var status = _fingerprintService.GetStatus();
                var isReady = _fingerprintService.IsDeviceConnected;

                return Ok(new
                {
                    success = true,
                    status = status,
                    isReady = isReady,
                    message = isReady 
                        ? "Scanner is ready for attendance recording" 
                        : "Scanner is not connected. Please check device."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking scanner status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking scanner status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Main endpoint: Record attendance via fingerprint scan
        /// Uses DailyAttendanceService for three-case toggle logic
        /// Implements Time In/Time Out toggle logic
        /// </summary>
        [HttpPost("RecordAttendance")]
        public async Task<IActionResult> RecordAttendance()
        {
            try
            {
                _logger.LogInformation("=== Starting Attendance Recording Process ===");

                // STEP 1: Ensure device is ready
                if (!_fingerprintService.IsDeviceConnected)
                {
                    _logger.LogWarning("Device not connected, attempting to initialize...");
                    
                    if (!_fingerprintService.Initialize())
                    {
                        return BadRequest(new AttendanceScanResponseDto
                        {
                            Success = false,
                            Message = "Scanner initialization failed. Please contact administrator.",
                            Action = "error"
                        });
                    }

                    if (!_fingerprintService.TurnOnDevice())
                    {
                        return BadRequest(new AttendanceScanResponseDto
                        {
                            Success = false,
                            Message = "Unable to connect to scanner. Please check device connection.",
                            Action = "error"
                        });
                    }
                }

                // STEP 2: Capture fingerprint
                _logger.LogInformation("Capturing fingerprint...");
                var (imageData, width, height, template, errorCode, errorMessage) = 
                    _fingerprintService.CaptureFingerprintWithImage();

                if (template == null || template.Length == 0)
                {
                    _logger.LogWarning($"Failed to capture fingerprint: {errorMessage}");
                    
                    // Log scan attempt
                    await LogScanAttempt(null, false, $"Capture failed: {errorMessage}");

                    // Control LED and beep for error
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(500);
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);

                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = errorMessage == "No finger detected on scanner" 
                            ? "Please place your finger on the scanner." 
                            : $"Failed to capture fingerprint: {errorMessage}",
                        Action = "error"
                    });
                }

                _logger.LogInformation("Fingerprint captured successfully, matching against database...");

                // STEP 3: Match against all registered fingerprints
                var matchedStudent = await FindMatchingStudent(template);

                if (matchedStudent is null)
                {
                    _logger.LogWarning("No matching fingerprint found");
                    
                    // Log failed match attempt
                    await LogScanAttempt(null, false, "Fingerprint not recognized");

                    // Error feedback
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(500);
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);

                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = "Fingerprint not recognized. Please register your fingerprint first.",
                        Action = "not_recognized"
                    });
                }

                _logger.LogInformation($"Student identified: {matchedStudent.StudNumber} - {matchedStudent.User?.UserFirstName} {matchedStudent.User?.UserLastName}");

                // STEP 4: Process attendance using service (implements Cases 1-3)
                var result = await _attendanceService.ProcessAttendanceScanAsync(matchedStudent);

                // Log successful scan
                await LogScanAttempt(matchedStudent.UserId, true, $"Attendance {result.Action} successful");

                // STEP 5: Provide appropriate feedback
                if (result.Success)
                {
                    // Success feedback (green light + short beep)
                    _fingerprintService.SetLedLight("green", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(300);
                    _fingerprintService.SetLedLight("green", false);
                    _fingerprintService.Beep(false);
                }
                else if (result.Action == "already_completed")
                {
                    // Already completed (red light + long beep)
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(800);
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                }

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during attendance recording");
                return StatusCode(500, new AttendanceScanResponseDto
                {
                    Success = false,
                    Message = "An error occurred while recording attendance. Please try again.",
                    Action = "error"
                });
            }
        }

        /// <summary>
        /// Helper: Find matching student by fingerprint template
        /// </summary>
        private async Task<StudentModel?> FindMatchingStudent(byte[] capturedTemplate)
        {
            const int MATCH_THRESHOLD = 50;

            var allTemplates = await _fingerprintRepo.GetAllAsync();
            _logger.LogInformation($"Comparing against {allTemplates.Count()} registered fingerprints");

            foreach (var template in allTemplates)
            {
                int score = _fingerprintService.GetMatchScore(capturedTemplate, template.FtempData);
                
                if (score >= MATCH_THRESHOLD)
                {
                    _logger.LogInformation($"Match found! User ID: {template.UserId}, Score: {score}");
                    
                    // Get the user
                    var user = await _fingerprintRepo.GetUserByTemplateAsync(template.UserId);
                    if (user?.RoleId == 3) // Role 3 = Student
                    {
                        var student = await _studentRepo.GetByUserIdAsync(user.UserId);
                        return student;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get today's attendance summary
        /// </summary>
        [HttpGet("TodaySummary")]
        public async Task<IActionResult> GetTodaySummary()
        {
            try
            {
                var summary = await _attendanceService.GetTodaySummaryAsync();
                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's summary");
                return StatusCode(500, new { success = false, error = "Failed to retrieve summary" });
            }
        }

        /// <summary>
        /// Helper: Log scan attempt to scan_logs table
        /// </summary>
        private async Task LogScanAttempt(int? userId, bool success, string message)
        {
            try
            {
                var scanLog = new ScanLog
                {
                    UserId = userId,
                    ScanTime = DateTime.Now,
                    ScanSuccess = success,
                    ScanLogMessage = message,
                    ScanCreatedAt = DateTime.Now
                };

                await _scanLogRepo.AddAsync(scanLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log scan attempt");
            }
        }
    }
}
