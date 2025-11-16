using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Models.Enums;
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
        private readonly IUserRepository _userRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly DailyAttendanceService _attendanceService;
        private readonly IScanLogRepository _scanLogRepo;
        private readonly IAttendanceNotificationRepository _notificationRepo;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            FingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IStudentRepository studentRepo,
            IUserRepository userRepo,
            IEnrollmentRepository enrollmentRepo,
            DailyAttendanceService attendanceService,
            IScanLogRepository scanLogRepo,
            IAttendanceNotificationRepository notificationRepo,
            ILogger<AttendanceController> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _enrollmentRepo = enrollmentRepo;
            _attendanceService = attendanceService;
            _scanLogRepo = scanLogRepo;
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
        /// ✅ IMPLEMENTS:
        /// - Role-based access control (students only)
        /// - Display student information after scan
        /// - Display class/subject information
        /// - Show time-in/time-out timestamps
        /// - Proper validation messages for Admin/Faculty/Unknown fingerprints
        /// - Populate all database tables: daily_attendance_log, attendance, attendance_notifications
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

                // STEP 3: Match fingerprint and validate role
                var matchResult = await MatchAndValidateFingerprint(template);

                if (!matchResult.Success)
                {
                    // Log failed match attempt
                    await LogScanAttempt(matchResult.UserId, false, matchResult.Message);

                    // Error feedback
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(matchResult.RoleViolation ? 1000 : 500); // Longer beep for role violations
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);

                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = matchResult.Message,
                        Action = "access_denied"
                    });
                }

                var student = matchResult.Student!;
                _logger.LogInformation($"Student identified: {student.StudNumber} - {student.User?.UserFirstName} {student.User?.UserLastName}");

                // STEP 4: Get enrolled classes for the student
                var enrolledClasses = await GetStudentEnrolledClassesAsync(student.StudId);
                if (!enrolledClasses.Any())
                {
                    _logger.LogWarning($"Student {student.StudNumber} has no enrolled classes");
                    
                    await LogScanAttempt(student.UserId, false, "No enrolled classes found");
                    
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(500);
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);

                    return BadRequest(new AttendanceScanResponseDto
                    {
                        Success = false,
                        Message = "You are not enrolled in any classes. Please contact administration.",
                        Action = "no_enrollment",
                        Student = CreateStudentInfoDto(student)
                    });
                }

                // STEP 5: Process attendance using service (implements Cases 1-3)
                var result = await _attendanceService.ProcessAttendanceScanAsync(student);

                // STEP 6: Create notification for successful attendance
                if (result.Success)
                {
                    await CreateAttendanceNotificationAsync(student, result.Action!, result.Timestamp);
                    
                    // Log successful scan
                    await LogScanAttempt(student.UserId, true, $"Attendance {result.Action} successful");

                    // Success feedback (green light + short beep)
                    _fingerprintService.SetLedLight("green", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(300);
                    _fingerprintService.SetLedLight("green", false);
                    _fingerprintService.Beep(false);

                    // Enhance result with enrolled classes information
                    result.Student = CreateStudentInfoDto(student, enrolledClasses);
                }
                else if (result.Action == "already_completed")
                {
                    // Already completed (red light + long beep)
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    await Task.Delay(800);
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);

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
        /// ✅ NEW: Match fingerprint and validate user role
        /// Returns detailed information about the match result and role validation
        /// </summary>
        private async Task<FingerprintMatchResult> MatchAndValidateFingerprint(byte[] capturedTemplate)
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
                    var user = await _userRepo.GetByIdAsync(template.UserId);
                    if (user == null)
                    {
                        return new FingerprintMatchResult
                        {
                            Success = false,
                            Message = "User account not found. Please contact administration.",
                            UserId = template.UserId
                        };
                    }

                    // ✅ ROLE-BASED ACCESS CONTROL
                    // Check if user is a student
                    if (user.RoleType == USER_ROLES.ADMIN)
                    {
                        _logger.LogWarning($"Admin user {user.UserEmail} attempted to use attendance scanner");
                        return new FingerprintMatchResult
                        {
                            Success = false,
                            Message = "❌ Access Denied: Administrators cannot log attendance through the fingerprint scanner.",
                            UserId = user.UserId,
                            RoleViolation = true
                        };
                    }

                    if (user.RoleType == USER_ROLES.FACULTY)
                    {
                        _logger.LogWarning($"Faculty user {user.UserEmail} attempted to use attendance scanner");
                        return new FingerprintMatchResult
                        {
                            Success = false,
                            Message = "❌ Access Denied: Faculty members cannot log attendance through the fingerprint scanner.",
                            UserId = user.UserId,
                            RoleViolation = true
                        };
                    }

                    if (user.RoleType != USER_ROLES.STUDENT)
                    {
                        _logger.LogWarning($"Non-student user {user.UserEmail} attempted to use attendance scanner");
                        return new FingerprintMatchResult
                        {
                            Success = false,
                            Message = "❌ Access Denied: Only students can log attendance through the fingerprint scanner.",
                            UserId = user.UserId,
                            RoleViolation = true
                        };
                    }

                    // User is a student - get student record
                    var student = await _studentRepo.GetByUserIdAsync(user.UserId);
                    if (student == null)
                    {
                        return new FingerprintMatchResult
                        {
                            Success = false,
                            Message = "Student record not found. Please contact administration.",
                            UserId = user.UserId
                        };
                    }

                    // Load user navigation property for student
                    student.User = user;

                    return new FingerprintMatchResult
                    {
                        Success = true,
                        Student = student,
                        UserId = user.UserId,
                        Message = "Student identified successfully"
                    };
                }
            }

            // No match found
            return new FingerprintMatchResult
            {
                Success = false,
                Message = "❌ Fingerprint Not Recognized: Please register your fingerprint with the administration."
            };
        }

        /// <summary>
        /// ✅ NEW: Get student's enrolled classes with course details
        /// </summary>
        private async Task<List<EnrolledClassInfo>> GetStudentEnrolledClassesAsync(int studId)
        {
            // ✅ FIX: Use the correct method name from IEnrollmentRepository
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
        /// ✅ NEW: Create student info DTO with enrolled classes
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
        /// ✅ NEW: Create attendance notification for the student
        /// Populates the attendance_notifications table
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
                _logger.LogInformation($"Notification created for student {student.StudNumber}: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create notification for student {student.StudNumber}");
                // Don't fail the entire operation if notification creation fails
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
                
                // Calculate success rate
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

        #region Helper Classes

        /// <summary>
        /// Result of fingerprint matching with role validation
        /// </summary>
        private class FingerprintMatchResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int? UserId { get; set; }
            public StudentModel? Student { get; set; }
            public bool RoleViolation { get; set; } = false;
        }

        #endregion
    }
}
