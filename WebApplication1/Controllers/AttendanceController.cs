using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using StudentModel = Fingerprint_Attendance_System.Models.Student;

namespace Fingerprint_Attendance_System.Controllers
{
    /// <summary>
    /// API Controller for fingerprint-based attendance recording.
    /// Uses FingerprintMatchingService for shared matching logic.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly FingerprintMatchingService _matchingService;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly DailyAttendanceService _attendanceService;
        private readonly IAttendanceNotificationRepository _notificationRepo;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            FingerprintMatchingService matchingService,
            IEnrollmentRepository enrollmentRepo,
            DailyAttendanceService attendanceService,
            IAttendanceNotificationRepository notificationRepo,
            ILogger<AttendanceController> logger)
        {
            _matchingService = matchingService;
            _enrollmentRepo = enrollmentRepo;
            _attendanceService = attendanceService;
            _notificationRepo = notificationRepo;
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
                var isReady = _matchingService.IsDeviceReady;

                return Ok(new
                {
                    success = true,
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
        /// </summary>
        [HttpPost("RecordAttendance")]
        public async Task<IActionResult> RecordAttendance()
        {
            try
            {
                _logger.LogInformation("=== Starting Attendance Recording Process ===");

                // STEP 1: Ensure device is ready
                if (!_matchingService.EnsureDeviceReady())
                {
                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = "Scanner initialization failed. Please contact administrator.",
                        Action = "error"
                    });
                }

                // STEP 2: Capture fingerprint
                _logger.LogInformation("Capturing fingerprint...");
                var (imageData, width, height, template, errorCode, errorMessage) = 
                    _matchingService.CaptureFingerprintWithImage();

                if (template == null || template.Length == 0)
                {
                    _logger.LogWarning("Failed to capture fingerprint: {ErrorMessage}", errorMessage);
                    
                    // ✅ FIX: Do NOT log "no finger detected" events to prevent scan_logs flooding
                    // Only log meaningful events (successful scans, fingerprint not recognized, etc.)
                    await _matchingService.ProvideLedFeedbackAsync(false);

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

                // STEP 3: Match fingerprint and validate role (using shared service)
                var matchResult = await _matchingService.MatchAndValidateFingerprintAsync(template, requireStudentRole: true);

                if (!matchResult.Success)
                {
                    await _matchingService.LogScanAttemptAsync(matchResult.UserId, false, matchResult.Message);
                    await _matchingService.ProvideLedFeedbackAsync(false, matchResult.RoleViolation);

                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = matchResult.Message,
                        Action = "access_denied"
                    });
                }

                var student = matchResult.Student!;
                _logger.LogInformation("Student identified: {StudNumber} - {Name}", 
                    student.StudNumber, $"{student.User?.UserFirstName} {student.User?.UserLastName}");

                // STEP 4: Get enrolled classes for the student
                var enrolledClasses = await GetStudentEnrolledClassesAsync(student.StudId);
                if (!enrolledClasses.Any())
                {
                    _logger.LogWarning("Student {StudNumber} has no enrolled classes", student.StudNumber);
                    
                    await _matchingService.LogScanAttemptAsync(student.UserId, false, "No enrolled classes found");
                    await _matchingService.ProvideLedFeedbackAsync(false);

                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = "You are not enrolled in any classes. Please contact administration.",
                        Action = "no_enrollment",
                        Student = CreateStudentInfoDto(student)
                    });
                }

                // STEP 5: Process attendance using service
                var result = await _attendanceService.ProcessAttendanceScanAsync(student);

                // STEP 6: Handle result
                if (result.Success)
                {
                    await CreateAttendanceNotificationAsync(student, result.Action!, result.Timestamp);
                    await _matchingService.LogScanAttemptAsync(student.UserId, true, $"Attendance {result.Action} successful");
                    await _matchingService.ProvideLedFeedbackAsync(true);
                    result.Student = CreateStudentInfoDto(student, enrolledClasses);
                }
                else if (result.Action == "already_completed")
                {
                    await _matchingService.ProvideLedFeedbackAsync(false);
                    result.Student = CreateStudentInfoDto(student, enrolledClasses);
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
        /// Get student's enrolled classes with course details
        /// </summary>
        private async Task<List<EnrolledClassInfo>> GetStudentEnrolledClassesAsync(int studId)
        {
            var enrollments = await _enrollmentRepo.GetStudentEnrollmentsWithDetailsAsync(studId);
            
            return enrollments.Select(e => new EnrolledClassInfo
            {
                ClassId = e.ClassId,
                CourseCode = e.CourseCode ?? "N/A",
                CourseName = e.CourseName ?? "N/A",
                FacultyName = e.FacultyName,
                RoomCode = e.RoomCode,
                Schedule = e.Schedule
            }).ToList();
        }

        /// <summary>
        /// Create student info DTO with enrolled classes
        /// </summary>
        private StudentInfoDto CreateStudentInfoDto(StudentModel student, List<EnrolledClassInfo>? enrolledClasses = null)
        {
            return new StudentInfoDto
            {
                StudId = student.StudId,
                StudNumber = student.StudNumber,
                FullName = $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim(),
                ProgramName = student.Program?.ProgramName,
                SectionName = student.Section?.SectionName,
                EnrolledClasses = enrolledClasses
            };
        }

        /// <summary>
        /// Create attendance notification for the student
        /// </summary>
        private async Task CreateAttendanceNotificationAsync(StudentModel student, string action, DateTime? timestamp)
        {
            try
            {
                string title = action == "time_in" 
                    ? "⏰ Time In Recorded" 
                    : "👋 Time Out Recorded";

                string message = action == "time_in"
                    ? $"You have successfully logged your Time In at {timestamp:hh:mm tt} on {timestamp:MMM dd, yyyy}."
                    : $"You have successfully logged your Time Out at {timestamp:hh:mm tt} on {timestamp:MMM dd, yyyy}.";

                var notification = new AttendanceNotification
                {
                    UserId = student.UserId,
                    NotificationType = action == "time_in" ? "time_in_recorded" : "time_out_recorded",
                    Title = title,
                    Message = message,
                    RelatedEntityType = "attendance",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                await _notificationRepo.AddAsync(notification);
                _logger.LogInformation("Notification created for student {StudNumber}: {Title}", student.StudNumber, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for student {StudNumber}", student.StudNumber);
            }
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
        /// Get today's statistics for display
        /// </summary>
        [HttpGet("GetTodayStats")]
        public async Task<IActionResult> GetTodayStats()
        {
            try
            {
                var summary = await _attendanceService.GetTodaySummaryAsync();
                
                int totalScans = summary.TotalStudents;
                int successfulScans = summary.TimedIn + summary.TimedOut;
                decimal successRate = totalScans > 0 
                    ? Math.Round((decimal)successfulScans / totalScans * 100, 0) 
                    : 100;

                return Ok(new
                {
                    successRate = $"{successRate}",
                    todaysScans = summary.TotalStudents
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today stats");
                return Ok(new { successRate = "0", todaysScans = 0 });
            }
        }
    }
}
