using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Configuration settings for attendance and fingerprint matching
    /// Loaded from appsettings.json to avoid hardcoded magic numbers
    /// </summary>
    public class AttendanceSettings
    {
        public const string SectionName = "AttendanceSettings";
        
        /// <summary>
        /// Minimum score required for fingerprint match (0-100)
        /// Default: 50
        /// </summary>
        public int FingerprintMatchThreshold { get; set; } = 50;
        
        /// <summary>
        /// Grace period in minutes after class start time before marking as "Late"
        /// Default: 15 minutes
        /// </summary>
        public int LateGracePeriodMinutes { get; set; } = 15;
        
        /// <summary>
        /// LED feedback duration in milliseconds for successful scan
        /// Default: 300ms
        /// </summary>
        public int SuccessLedDurationMs { get; set; } = 300;
        
        /// <summary>
        /// LED feedback duration in milliseconds for failed scan
        /// Default: 500ms
        /// </summary>
        public int ErrorLedDurationMs { get; set; } = 500;
        
        /// <summary>
        /// LED feedback duration in milliseconds for role violation
        /// Default: 1000ms
        /// </summary>
        public int RoleViolationLedDurationMs { get; set; } = 1000;
        
        /// <summary>
        /// Delay in milliseconds when resetting the scanner device
        /// Default: 500ms
        /// </summary>
        public int DeviceResetDelayMs { get; set; } = 500;
        
        /// <summary>
        /// Maximum number of fingerprint capture retry attempts
        /// Default: 3
        /// </summary>
        public int MaxCaptureRetries { get; set; } = 3;
        
        /// <summary>
        /// Delay in milliseconds between capture retry attempts
        /// Default: 300ms
        /// </summary>
        public int CaptureRetryDelayMs { get; set; } = 300;
        
        /// <summary>
        /// Size of the fingerprint template buffer in bytes
        /// Default: 2048
        /// </summary>
        public int TemplateBufferSize { get; set; } = 2048;
        
        /// <summary>
        /// Minimum valid template size in bytes (templates smaller than this are rejected)
        /// Default: 100
        /// </summary>
        public int MinTemplateSize { get; set; } = 100;
        
        /// <summary>
        /// Timeout in seconds for fingerprint capture operations
        /// Default: 10
        /// </summary>
        public int CaptureTimeoutSeconds { get; set; } = 10;
    }

    /// <summary>
    /// Result of fingerprint matching with role validation
    /// Shared between AttendanceController and AttendanceScanController
    /// </summary>
    public class FingerprintMatchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public Student? Student { get; set; }
        public User? User { get; set; }
        public bool RoleViolation { get; set; } = false;
        public int MatchScore { get; set; }
    }

    /// <summary>
    /// Shared service for fingerprint matching and validation logic
    /// Eliminates duplicate code between AttendanceController and AttendanceScanController
    /// </summary>
    public class FingerprintMatchingService : Interfaces.IFingerprintMatchingService
    {
        private readonly Interfaces.IFingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IUserRepository _userRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IScanLogRepository _scanLogRepo;
        private readonly AttendanceSettings _settings;
        private readonly ILogger<FingerprintMatchingService> _logger;

        public FingerprintMatchingService(
            Interfaces.IFingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IUserRepository userRepo,
            IStudentRepository studentRepo,
            IScanLogRepository scanLogRepo,
            IOptions<AttendanceSettings> settings,
            ILogger<FingerprintMatchingService> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _scanLogRepo = scanLogRepo;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Get configured attendance settings
        /// </summary>
        public AttendanceSettings Settings => _settings;

        /// <summary>
        /// Capture fingerprint template from scanner
        /// Returns null if no finger detected or capture failed
        /// </summary>
        public byte[]? CaptureFingerprint()
        {
            return _fingerprintService.CaptureTemplate();
        }

        /// <summary>
        /// Capture fingerprint with image data (for detailed capture)
        /// </summary>
        public (byte[]? imageData, int width, int height, byte[]? template, int errorCode, string errorMessage) CaptureFingerprintWithImage()
        {
            return _fingerprintService.CaptureFingerprintWithImage();
        }

        /// <summary>
        /// Check if scanner device is connected and ready
        /// </summary>
        public bool IsDeviceReady => _fingerprintService.IsDeviceConnected;

        /// <summary>
        /// Initialize and turn on the fingerprint device
        /// </summary>
        public bool EnsureDeviceReady()
        {
            if (_fingerprintService.IsDeviceConnected)
                return true;

            if (!_fingerprintService.Initialize())
            {
                _logger.LogError("Failed to initialize fingerprint SDK");
                return false;
            }

            if (!_fingerprintService.TurnOnDevice())
            {
                _logger.LogError("Failed to turn on fingerprint device");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Match captured fingerprint against database and validate user role
        /// This is the SINGLE source of truth for fingerprint matching logic
        /// </summary>
        /// <param name="capturedTemplate">The captured fingerprint template bytes</param>
        /// <param name="requireStudentRole">Whether to validate that the user is a student</param>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        public async Task<FingerprintMatchResult> MatchAndValidateFingerprintAsync(
            byte[] capturedTemplate, 
            bool requireStudentRole = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var allTemplates = await _fingerprintRepo.GetAllAsync();
            var templateList = allTemplates.ToList();
            
            _logger.LogInformation("Comparing against {Count} registered fingerprints (threshold: {Threshold})", 
                templateList.Count, _settings.FingerprintMatchThreshold);

            if (!templateList.Any())
            {
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "No registered fingerprints found in the system. Please register first."
                };
            }

            FingerprintTemplate? matchedTemplate = null;
            int bestScore = 0;

            foreach (var template in templateList)
            {
                try
                {
                    int score = _fingerprintService.GetMatchScore(capturedTemplate, template.FtempData);
                    _logger.LogDebug("Match score for user {UserId}: {Score}", template.UserId, score);

                    if (score > bestScore && score >= _settings.FingerprintMatchThreshold)
                    {
                        bestScore = score;
                        matchedTemplate = template;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error matching template for user {UserId}", template.UserId);
                }
            }

            if (matchedTemplate == null)
            {
                _logger.LogWarning("Fingerprint not recognized. Best score: {BestScore} (threshold: {Threshold})", 
                    bestScore, _settings.FingerprintMatchThreshold);
                
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "❌ Fingerprint Not Recognized: Please register your fingerprint with the administration.",
                    MatchScore = bestScore
                };
            }

            _logger.LogInformation("✅ Fingerprint matched! User ID: {UserId}, Score: {Score}", 
                matchedTemplate.UserId, bestScore);

            // Get user details
            var user = await _userRepo.GetByIdAsync(matchedTemplate.UserId);
            if (user == null)
            {
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "User account not found. Please contact administration.",
                    UserId = matchedTemplate.UserId,
                    MatchScore = bestScore
                };
            }

            // Check if account is active
            if (!user.IsActive)
            {
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "Your account has been deactivated. Please contact administration.",
                    UserId = user.UserId,
                    User = user,
                    MatchScore = bestScore
                };
            }

            // Role validation (if required for attendance)
            if (requireStudentRole)
            {
                var roleValidation = ValidateStudentRole(user);
                if (!roleValidation.Success)
                {
                    roleValidation.MatchScore = bestScore;
                    return roleValidation;
                }

                // Get student record
                var student = await _studentRepo.GetByUserIdAsync(user.UserId);
                if (student == null)
                {
                    return new FingerprintMatchResult
                    {
                        Success = false,
                        Message = "Student record not found. Please contact administration.",
                        UserId = user.UserId,
                        User = user,
                        MatchScore = bestScore
                    };
                }

                // Populate navigation property
                student.User = user;

                return new FingerprintMatchResult
                {
                    Success = true,
                    Message = "Student identified successfully",
                    UserId = user.UserId,
                    User = user,
                    Student = student,
                    MatchScore = bestScore
                };
            }

            // Return user without student validation (for login scenarios)
            return new FingerprintMatchResult
            {
                Success = true,
                Message = "User identified successfully",
                UserId = user.UserId,
                User = user,
                MatchScore = bestScore
            };
        }

        /// <summary>
        /// Validate that user has student role for attendance
        /// </summary>
        private FingerprintMatchResult ValidateStudentRole(User user)
        {
            if (user.RoleType == USER_ROLES.ADMIN)
            {
                _logger.LogWarning("Admin user {Email} attempted to use attendance scanner", user.UserEmail);
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "❌ Access Denied: Administrators cannot log attendance through the fingerprint scanner.",
                    UserId = user.UserId,
                    User = user,
                    RoleViolation = true
                };
            }

            if (user.RoleType == USER_ROLES.FACULTY)
            {
                _logger.LogWarning("Faculty user {Email} attempted to use attendance scanner", user.UserEmail);
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "❌ Access Denied: Faculty members cannot log attendance through the fingerprint scanner.",
                    UserId = user.UserId,
                    User = user,
                    RoleViolation = true
                };
            }

            if (user.RoleType != USER_ROLES.STUDENT)
            {
                _logger.LogWarning("Non-student user {Email} attempted to use attendance scanner", user.UserEmail);
                return new FingerprintMatchResult
                {
                    Success = false,
                    Message = "❌ Access Denied: Only students can log attendance through the fingerprint scanner.",
                    UserId = user.UserId,
                    User = user,
                    RoleViolation = true
                };
            }

            return new FingerprintMatchResult { Success = true };
        }

        /// <summary>
        /// Log scan attempt to database
        /// </summary>
        /// <param name="userId">User ID (null if unknown)</param>
        /// <param name="success">Whether scan was successful</param>
        /// <param name="message">Log message</param>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        public async Task LogScanAttemptAsync(int? userId, bool success, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
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
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Scan log operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log scan attempt");
            }
        }

        /// <summary>
        /// Provide LED feedback for scan result
        /// </summary>
        /// <param name="success">Whether scan was successful</param>
        /// <param name="roleViolation">Whether there was a role violation</param>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        public async Task ProvideLedFeedbackAsync(bool success, bool roleViolation = false, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                string color = success ? "green" : "red";
                int duration = success 
                    ? _settings.SuccessLedDurationMs 
                    : (roleViolation ? _settings.RoleViolationLedDurationMs : _settings.ErrorLedDurationMs);

                _fingerprintService.SetLedLight(color, true);
                _fingerprintService.Beep(true);
                await Task.Delay(duration, cancellationToken);
                _fingerprintService.SetLedLight(color, false);
                _fingerprintService.Beep(false);
            }
            catch (OperationCanceledException)
            {
                // Ensure LEDs are turned off on cancellation
                _fingerprintService.SetLedLight("green", false);
                _fingerprintService.SetLedLight("red", false);
                _fingerprintService.Beep(false);
                _logger.LogWarning("LED feedback operation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to provide LED feedback");
            }
        }

        /// <summary>
        /// Determine attendance status based on scan time and class start time
        /// ✅ UPDATED: Always returns Present - Late status is only applied manually by faculty
        /// </summary>
        public AttendanceStatus DetermineAttendanceStatus(TimeSpan currentTime, TimeSpan classStartTime)
        {
            // ✅ Always return Present - Late status should only be set manually by faculty
            return AttendanceStatus.Present;
        }
    }
}
