using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Exceptions;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Main orchestration service for attendance processing
    /// Coordinates all attendance-related operations
    /// PHASE 6: STEP 14 - API/Service Layer
    /// </summary>
    public class AttendanceProcessingService
    {
        private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly DailyAttendanceService _dailyAttendanceService;
        private readonly MissedTimeOutService _missedTimeOutService;
        private readonly LateDetectionService _lateDetectionService;
        private readonly AbsenceDetectionService _absenceService;
        private readonly ILogger<AttendanceProcessingService> _logger;

        public AttendanceProcessingService(
            FingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IStudentRepository studentRepo,
            IEnrollmentRepository enrollmentRepo,
            DailyAttendanceService dailyAttendanceService,
            MissedTimeOutService missedTimeOutService,
            LateDetectionService lateDetectionService,
            AbsenceDetectionService absenceService,
            ILogger<AttendanceProcessingService> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _studentRepo = studentRepo;
            _enrollmentRepo = enrollmentRepo;
            _dailyAttendanceService = dailyAttendanceService;
            _missedTimeOutService = missedTimeOutService;
            _lateDetectionService = lateDetectionService;
            _absenceService = absenceService;
            _logger = logger;
        }

        /// <summary>
        /// MAIN ORCHESTRATION METHOD
        /// Complete attendance recording pipeline with all validations
        /// </summary>
        public async Task<AttendanceProcessingResult> ProcessAttendanceAsync()
        {
            var result = new AttendanceProcessingResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("=== Attendance Processing Pipeline Started ===");

                // STEP 1: Validate Scanner
                result.ScannerStatus = await ValidateScannerAsync();
                if (!result.ScannerStatus.IsAvailable)
                {
                    throw new ScannerNotAvailableException(result.ScannerStatus.Message);
                }

                // STEP 2: Capture Fingerprint
                result.CaptureResult = await CaptureFingerprintAsync();
                if (!result.CaptureResult.Success)
                {
                    throw new Exception(result.CaptureResult.ErrorMessage);
                }

                // STEP 3: Match Fingerprint
                result.Student = await MatchFingerprintAsync(result.CaptureResult.Template!);
                if (result.Student == null)
                {
                    throw new FingerprintNotRecognizedException();
                }

                // STEP 4: Validate Enrollment
                result.EnrollmentStatus = await ValidateEnrollmentAsync(result.Student.StudId);
                if (!result.EnrollmentStatus.IsEnrolled)
                {
                    throw new NoScheduleException(result.Student.StudId);
                }

                // STEP 5: Check Incomplete Records
                result.IncompleteCheck = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    result.Student.StudId,
                    DateTime.Today);

                // STEP 6: Process Attendance (Time In/Out)
                result.AttendanceResponse = await _dailyAttendanceService.ProcessAttendanceScanAsync(result.Student);

                // STEP 7: Add incomplete warning if needed
                if (result.IncompleteCheck.HasIncompleteRecords)
                {
                    result.AttendanceResponse.IncompleteRecordsWarning = result.IncompleteCheck.WarningMessage;
                }

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogInformation($"? Attendance processing completed in {result.ProcessingTimeMs}ms");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorType = ex.GetType().Name;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogError(ex, $"? Attendance processing failed: {ex.Message}");

                return result;
            }
        }

        /// <summary>
        /// Validate scanner availability and connectivity
        /// </summary>
        private async Task<ScannerStatusDto> ValidateScannerAsync()
        {
            await Task.CompletedTask; // Async placeholder

            var status = _fingerprintService.GetStatus();
            var isConnected = _fingerprintService.IsDeviceConnected;

            if (!isConnected)
            {
                // Attempt auto-recovery
                _logger.LogWarning("Scanner not connected, attempting auto-recovery...");
                
                if (_fingerprintService.Initialize() && _fingerprintService.TurnOnDevice())
                {
                    return new ScannerStatusDto
                    {
                        IsAvailable = true,
                        Status = "Device Connected",
                        Message = "Scanner initialized successfully"
                    };
                }

                return new ScannerStatusDto
                {
                    IsAvailable = false,
                    Status = status,
                    Message = "Scanner not available. Please check device connection."
                };
            }

            return new ScannerStatusDto
            {
                IsAvailable = true,
                Status = status,
                Message = "Scanner ready"
            };
        }

        /// <summary>
        /// Capture fingerprint with timeout protection
        /// </summary>
        private async Task<FingerprintCaptureResult> CaptureFingerprintAsync()
        {
            const int TIMEOUT_SECONDS = 10;

            var captureTask = Task.Run(() => _fingerprintService.CaptureFingerprintWithImage());
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(TIMEOUT_SECONDS));

            var completedTask = await Task.WhenAny(captureTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return new FingerprintCaptureResult
                {
                    Success = false,
                    ErrorMessage = $"Capture timeout after {TIMEOUT_SECONDS} seconds. Please try again."
                };
            }

            var (imageData, width, height, template, errorCode, errorMessage) = await captureTask;

            if (template == null || template.Length == 0)
            {
                return new FingerprintCaptureResult
                {
                    Success = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage
                };
            }

            return new FingerprintCaptureResult
            {
                Success = true,
                Template = template,
                ImageData = imageData,
                Width = width,
                Height = height
            };
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
        /// Validate student has active enrollments
        /// </summary>
        private async Task<EnrollmentStatusDto> ValidateEnrollmentAsync(int studId)
        {
            var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
            var enrollmentsList = enrollments.ToList();

            return new EnrollmentStatusDto
            {
                IsEnrolled = enrollmentsList.Any(),
                EnrollmentCount = enrollmentsList.Count
            };
        }

        /// <summary>
        /// Get scanner device status
        /// </summary>
        public ScannerStatusDto GetScannerStatus()
        {
            var status = _fingerprintService.GetStatus();
            var isConnected = _fingerprintService.IsDeviceConnected;

            return new ScannerStatusDto
            {
                IsAvailable = isConnected,
                Status = status,
                Message = isConnected ? "Scanner ready" : "Scanner not available"
            };
        }

        /// <summary>
        /// Get today's attendance summary
        /// </summary>
        public async Task<DailyAttendanceSummaryDto> GetTodaySummaryAsync()
        {
            return await _dailyAttendanceService.GetTodaySummaryAsync();
        }

        /// <summary>
        /// Manually trigger absence detection for a specific date
        /// </summary>
        public async Task ProcessAbsencesAsync(DateTime date)
        {
            await _absenceService.ProcessAbsencesForDateAsync(date);
        }
    }

    #region DTOs

    public class AttendanceProcessingResult
    {
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double ProcessingTimeMs { get; set; }
        public string? Error { get; set; }
        public string? ErrorType { get; set; }

        public ScannerStatusDto ScannerStatus { get; set; } = new();
        public FingerprintCaptureResult CaptureResult { get; set; } = new();
        public Student? Student { get; set; }
        public EnrollmentStatusDto EnrollmentStatus { get; set; } = new();
        public IncompleteRecordResult IncompleteCheck { get; set; } = new();
        public AttendanceScanResponseDto? AttendanceResponse { get; set; }
    }

    public class ScannerStatusDto
    {
        public bool IsAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class FingerprintCaptureResult
    {
        public bool Success { get; set; }
        public byte[]? Template { get; set; }
        public byte[]? ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class EnrollmentStatusDto
    {
        public bool IsEnrolled { get; set; }
        public int EnrollmentCount { get; set; }
    }

    #endregion
}
