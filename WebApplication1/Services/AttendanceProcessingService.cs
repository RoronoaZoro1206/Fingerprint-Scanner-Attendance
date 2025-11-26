using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Exceptions;
using Fingerprint_Attendance_System.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Main orchestration service for attendance processing
    /// Coordinates all attendance-related operations
    /// PHASE 6: STEP 14 - API/Service Layer
    /// </summary>
    public class AttendanceProcessingService
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly IFingerprintMatchingService _matchingService;
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IClassScheduleRepository _scheduleRepo;
        private readonly DailyAttendanceService _dailyAttendanceService;
        private readonly MissedTimeOutService _missedTimeOutService;
        private readonly LateDetectionService _lateDetectionService;
        private readonly AbsenceDetectionService _absenceService;
        private readonly AttendanceSettings _settings; // ✅ NEW: Use configurable settings
        private readonly ILogger<AttendanceProcessingService> _logger;
        private readonly PhilippineTimeService _pht;

        public AttendanceProcessingService(
            IFingerprintService fingerprintService,
            IFingerprintMatchingService matchingService,
            IStudentRepository studentRepo,
            IEnrollmentRepository enrollmentRepo,
            IClassScheduleRepository scheduleRepo,
            DailyAttendanceService dailyAttendanceService,
            MissedTimeOutService missedTimeOutService,
            LateDetectionService lateDetectionService,
            AbsenceDetectionService absenceService,
            IOptions<AttendanceSettings> settings,
            ILogger<AttendanceProcessingService> logger,
            PhilippineTimeService pht)
        {
            _fingerprintService = fingerprintService;
            _matchingService = matchingService;
            _studentRepo = studentRepo;
            _enrollmentRepo = enrollmentRepo;
            _scheduleRepo = scheduleRepo;
            _dailyAttendanceService = dailyAttendanceService;
            _missedTimeOutService = missedTimeOutService;
            _lateDetectionService = lateDetectionService;
            _absenceService = absenceService;
            _settings = settings.Value; // ✅ NEW: Store settings
            _logger = logger;
            _pht = pht;
        }

        /// <summary>
        /// MAIN ORCHESTRATION METHOD
        /// Complete attendance recording pipeline with all validations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        public async Task<AttendanceProcessingResult> ProcessAttendanceAsync(CancellationToken cancellationToken = default)
        {
            var result = new AttendanceProcessingResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("=== Attendance Processing Pipeline Started ===");

                // STEP 1: Validate Scanner
                cancellationToken.ThrowIfCancellationRequested();
                result.ScannerStatus = await ValidateScannerAsync(cancellationToken);
                if (!result.ScannerStatus.IsAvailable)
                {
                    throw new ScannerNotAvailableException(result.ScannerStatus.Message);
                }

                // STEP 2: Capture Fingerprint
                cancellationToken.ThrowIfCancellationRequested();
                result.CaptureResult = await CaptureFingerprintAsync(cancellationToken);
                if (!result.CaptureResult.Success)
                {
                    throw new Exception(result.CaptureResult.ErrorMessage);
                }

                // STEP 3: Match Fingerprint
                cancellationToken.ThrowIfCancellationRequested();
                result.Student = await MatchFingerprintAsync(result.CaptureResult.Template!, cancellationToken);
                if (result.Student == null)
                {
                    throw new FingerprintNotRecognizedException();
                }

                // STEP 4: Validate Enrollment
                cancellationToken.ThrowIfCancellationRequested();
                result.EnrollmentStatus = await ValidateEnrollmentAsync(result.Student.StudId, cancellationToken);
                if (!result.EnrollmentStatus.IsEnrolled)
                {
                    throw new NoScheduleException(result.Student.StudId);
                }

                // ✅ NEW STEP 5: Validate Schedule (Only for Time In)
                cancellationToken.ThrowIfCancellationRequested();
                result.ScheduleStatus = await ValidateScheduleAsync(result.Student.StudId, cancellationToken);
                if (!result.ScheduleStatus.HasScheduledClass)
                {
                    throw new NoScheduleException(result.Student.StudId, result.ScheduleStatus.Message);
                }

                // STEP 6: Check Incomplete Records
                cancellationToken.ThrowIfCancellationRequested();
                result.IncompleteCheck = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    result.Student.StudId,
                    _pht.Today); // Use PHT date

                // STEP 7: Process Attendance (Time In/Out)
                cancellationToken.ThrowIfCancellationRequested();
                result.AttendanceResponse = await _dailyAttendanceService.ProcessAttendanceScanAsync(result.Student);

                // STEP 8: Add incomplete warning if needed
                if (result.IncompleteCheck.HasIncompleteRecords)
                {
                    result.AttendanceResponse.IncompleteRecordsWarning = result.IncompleteCheck.WarningMessage;
                }

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogInformation($"✅ Attendance processing completed in {result.ProcessingTimeMs}ms");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorType = ex.GetType().Name;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogError(ex, $"❌ Attendance processing failed: {ex.Message}");

                return result;
            }
        }

        /// <summary>
        /// MAIN ORCHESTRATION METHOD
        /// Complete attendance recording pipeline with all validations
        /// ✅ OVERLOAD: Accepts pre-matched student (used by HomeController)
        /// </summary>
        public async Task<AttendanceProcessingResult> ProcessAttendanceAsync(Student student)
        {
            var result = new AttendanceProcessingResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation($"=== Attendance Processing Pipeline Started for Student {student.StudId} ===");

                // SKIP STEPS 1-3: Scanner, Capture, and Match (already done by HomeController)
                result.Student = student;
                _logger.LogInformation($"Using pre-matched student: {student.StudNumber}");

                // STEP 4: Validate Enrollment
                result.EnrollmentStatus = await ValidateEnrollmentAsync(student.StudId);
                if (!result.EnrollmentStatus.IsEnrolled)
                {
                    throw new NoScheduleException(student.StudId);
                }

                // ✅ STEP 5: Validate Schedule (Already done by HomeController, but kept for internal consistency)
                result.ScheduleStatus = await ValidateScheduleAsync(student.StudId);
                if (!result.ScheduleStatus.HasScheduledClass)
                {
                    throw new NoScheduleException(student.StudId, result.ScheduleStatus.Message);
                }

                // STEP 6: Check Incomplete Records
                result.IncompleteCheck = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    student.StudId,
                    _pht.Today); // Use PHT date

                // STEP 7: Process Attendance (Time In/Out)
                result.AttendanceResponse = await _dailyAttendanceService.ProcessAttendanceScanAsync(student);

                // STEP 8: Add incomplete warning if needed
                if (result.IncompleteCheck.HasIncompleteRecords)
                {
                    result.AttendanceResponse.IncompleteRecordsWarning = result.IncompleteCheck.WarningMessage;
                }

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogInformation($"✅ Attendance processing completed in {result.ProcessingTimeMs}ms");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorType = ex.GetType().Name;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogError(ex, $"❌ Attendance processing failed: {ex.Message}");

                return result;
            }
        }

        /// <summary>
        /// MAIN ORCHESTRATION METHOD
        /// Complete attendance recording pipeline with all validations
        /// ✅ NEW OVERLOAD: Accepts pre-matched student AND class ID (used by HomeController)
        /// </summary>
        public async Task<AttendanceProcessingResult> ProcessAttendanceAsync(Student student, int classId)
        {
            var result = new AttendanceProcessingResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation($"=== Attendance Processing Pipeline Started for Student {student.StudId}, Class {classId} ===");

                // SKIP STEPS 1-3: Scanner, Capture, and Match (already done by HomeController)
                result.Student = student;
                _logger.LogInformation($"Using pre-matched student: {student.StudNumber}, ClassId: {classId}");

                // STEP 4: Validate Enrollment
                result.EnrollmentStatus = await ValidateEnrollmentAsync(student.StudId);
                if (!result.EnrollmentStatus.IsEnrolled)
                {
                    throw new NoScheduleException(student.StudId);
                }

                // STEP 5: Check Incomplete Records
                result.IncompleteCheck = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    student.StudId,
                    _pht.Today); // Use PHT date

                // STEP 6: Process Attendance (Time In/Out) with classId
                result.AttendanceResponse = await _dailyAttendanceService.ProcessAttendanceScanAsync(student, classId);

                // STEP 7: Add incomplete warning if needed
                if (result.IncompleteCheck.HasIncompleteRecords)
                {
                    result.AttendanceResponse.IncompleteRecordsWarning = result.IncompleteCheck.WarningMessage;
                }

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogInformation($"✅ Attendance processing completed in {result.ProcessingTimeMs}ms");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorType = ex.GetType().Name;
                result.EndTime = DateTime.Now;
                result.ProcessingTimeMs = (result.EndTime - result.StartTime).TotalMilliseconds;

                _logger.LogError(ex, $"❌ Attendance processing failed: {ex.Message}");

                return result;
            }
        }

        /// <summary>
        /// Validate scanner availability and connectivity
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task<ScannerStatusDto> ValidateScannerAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task<FingerprintCaptureResult> CaptureFingerprintAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // ✅ Use configurable timeout from settings
            int timeoutSeconds = _settings.CaptureTimeoutSeconds;

            var captureTask = Task.Run(() => _fingerprintService.CaptureFingerprintWithImage(), cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

            var completedTask = await Task.WhenAny(captureTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return new FingerprintCaptureResult
                {
                    Success = false,
                    ErrorMessage = $"Capture timeout after {timeoutSeconds} seconds. Please try again."
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
        /// ✅ REFACTORED: Uses FingerprintMatchingService to eliminate duplicate code
        /// </summary>
        /// <param name="capturedTemplate">Captured fingerprint template</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task<Student?> MatchFingerprintAsync(byte[] capturedTemplate, CancellationToken cancellationToken = default)
        {
            // ✅ Use shared FingerprintMatchingService instead of duplicate logic
            var matchResult = await _matchingService.MatchAndValidateFingerprintAsync(capturedTemplate, requireStudentRole: true, cancellationToken);
            
            if (matchResult.Success && matchResult.Student != null)
            {
                _logger.LogInformation($"Match found via shared service - Student: {matchResult.Student.StudNumber}, Score: {matchResult.MatchScore}");
                return matchResult.Student;
            }
            
            _logger.LogWarning($"No match found. Best score: {matchResult.MatchScore} (threshold: {_settings.FingerprintMatchThreshold})");
            return null;
        }

        /// <summary>
        /// Validate student has active enrollments
        /// </summary>
        /// <param name="studId">Student ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task<EnrollmentStatusDto> ValidateEnrollmentAsync(int studId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
            var enrollmentsList = enrollments.ToList();

            return new EnrollmentStatusDto
            {
                IsEnrolled = enrollmentsList.Any(),
                EnrollmentCount = enrollmentsList.Count
            };
        }

        /// <summary>
        /// ✅ FIXED: Validate student has a scheduled class at current time
        /// This ensures students can only time in when they have an actual class
        /// CRITICAL FIX: Proper time boundary handling for edge cases like 12:03 PM
        /// </summary>
        /// <param name="studId">Student ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task<ScheduleStatusDto> ValidateScheduleAsync(int studId, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentTime = _pht.Now; // Use PHT for schedule validation
                var currentDay = currentTime.ToString("dddd"); // e.g., "Monday", "Saturday"
                var currentTimeOnly = currentTime.TimeOfDay;

                _logger.LogInformation($"[VALIDATE SCHEDULE] ===== START VALIDATION =====");
                _logger.LogInformation($"[VALIDATE SCHEDULE] Student ID: {studId}");
                _logger.LogInformation($"[VALIDATE SCHEDULE] Current DateTime: {currentTime:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"[VALIDATE SCHEDULE] Day of Week: '{currentDay}'");
                _logger.LogInformation($"[VALIDATE SCHEDULE] Time Only: {currentTimeOnly:hh\\:mm\\:ss}");
                _logger.LogInformation($"[VALIDATE SCHEDULE] Time (total seconds): {currentTimeOnly.TotalSeconds}");

                // Get all student's enrolled classes
                var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
                
                if (enrollments == null)
                {
                    _logger.LogWarning($"[VALIDATE SCHEDULE] GetByStudentIdAsync returned NULL");
                    return new ScheduleStatusDto
                    {
                        HasScheduledClass = false,
                        Message = "Unable to retrieve enrollments. Please contact support."
                    };
                }
                
                var classIds = enrollments.Select(e => e.ClassId).ToList();

                if (!classIds.Any())
                {
                    _logger.LogWarning($"[VALIDATE SCHEDULE] Student has no enrollments");
                    _logger.LogInformation($"[VALIDATE SCHEDULE] ===== END VALIDATION (NO ENROLLMENTS) =====");
                    return new ScheduleStatusDto
                    {
                        HasScheduledClass = false,
                        Message = "You have no enrolled classes. Please enroll in classes before timing in."
                    };
                }

                _logger.LogInformation($"[VALIDATE SCHEDULE] Found {classIds.Count} enrolled class(es): [{string.Join(", ", classIds)}]");

                // ✅ CRITICAL FIX: Check if any class is scheduled for this day/time
                // Iterate through ALL classes and ALL schedules
                int totalSchedulesChecked = 0;
                int matchingDayCount = 0;
                int matchingTimeCount = 0;
                
                foreach (var classId in classIds)
                {
                    _logger.LogInformation($"[VALIDATE SCHEDULE] ----- Checking Class ID {classId} -----");
                    
                    var schedules = await _scheduleRepo.GetByClassIdAsync(classId);
                    
                    if (schedules == null)
                    {
                        _logger.LogWarning($"[VALIDATE SCHEDULE] GetByClassIdAsync returned NULL for Class ID {classId}");
                        continue;
                    }
                    
                    var schedulesList = schedules.ToList();
                    
                    _logger.LogInformation($"[VALIDATE SCHEDULE] Class ID {classId}: {schedulesList.Count} schedule(s) found");
                    
                    foreach (var schedule in schedulesList)
                    {
                        if (schedule == null)
                        {
                            _logger.LogWarning($"[VALIDATE SCHEDULE] NULL schedule entry in list - SKIPPING");
                            continue;
                        }
                        
                        totalSchedulesChecked++;
                        
                        _logger.LogInformation($"[VALIDATE SCHEDULE] --- Schedule #{totalSchedulesChecked} (SchedId: {schedule.SchedId}) ---");
                        _logger.LogInformation($"[VALIDATE SCHEDULE]   DB Day: '{schedule.SchedDayOfWeek}' (trimmed: '{schedule.SchedDayOfWeek?.Trim()}')");
                        _logger.LogInformation($"[VALIDATE SCHEDULE]   DB Start: {schedule.SchedStartTime:hh\\:mm\\:ss}");
                        _logger.LogInformation($"[VALIDATE SCHEDULE]   DB End: {schedule.SchedEndTime:hh\\:mm\\:ss}");
                        
                        // ✅ CRITICAL FIX: Match day of week (case-insensitive, trimmed)
                        bool dayMatch = schedule.SchedDayOfWeek?.Trim().Equals(currentDay.Trim(), StringComparison.OrdinalIgnoreCase) ?? false;
                        
                        _logger.LogInformation($"[VALIDATE SCHEDULE]   Day Match: {dayMatch}");
                        _logger.LogInformation($"[VALIDATE SCHEDULE]     Comparing: DB='{schedule.SchedDayOfWeek?.Trim()}' vs Current='{currentDay.Trim()}'");
                        
                        if (dayMatch)
                        {
                            matchingDayCount++;
                            
                            // ✅ CRITICAL FIX: Proper time boundary checking
                            // A class is active if: current time >= start time AND current time <= end time
                            // Example: 12:03 PM is WITHIN 12:00 PM - 4:00 PM (active)
                            // Example: 12:03 PM is NOT WITHIN 10:00 AM - 12:00 PM (class ended 3 minutes ago)
                            bool afterStart = currentTimeOnly >= schedule.SchedStartTime;
                            bool beforeEnd = currentTimeOnly <= schedule.SchedEndTime;
                            bool timeMatch = afterStart && beforeEnd;
                            
                            _logger.LogInformation($"[VALIDATE SCHEDULE]   Time Check:");
                            _logger.LogInformation($"[VALIDATE SCHEDULE]     After Start: {afterStart} ({currentTimeOnly.TotalSeconds}s >= {schedule.SchedStartTime.TotalSeconds}s)");
                            _logger.LogInformation($"[VALIDATE SCHEDULE]     Before End: {beforeEnd} ({currentTimeOnly.TotalSeconds}s <= {schedule.SchedEndTime.TotalSeconds}s)");
                            _logger.LogInformation($"[VALIDATE SCHEDULE]     Time Match: {timeMatch}");
                            
                            if (timeMatch)
                            {
                                matchingTimeCount++;
                                
                                _logger.LogInformation($"[VALIDATE SCHEDULE] ✅✅✅ SCHEDULE VALIDATED! ✅✅✅");
                                _logger.LogInformation($"[VALIDATE SCHEDULE] ClassId: {classId}, SchedId: {schedule.SchedId}");
                                _logger.LogInformation($"[VALIDATE SCHEDULE] {currentDay} {schedule.SchedStartTime:hh\\:mm tt}-{schedule.SchedEndTime:hh\\:mm tt}");
                                _logger.LogInformation($"[VALIDATE SCHEDULE] ===== END VALIDATION (SUCCESS) =====");
                                
                                return new ScheduleStatusDto
                                {
                                    HasScheduledClass = true,
                                    ClassId = classId,
                                    DayOfWeek = schedule.SchedDayOfWeek ?? string.Empty,
                                    StartTime = schedule.SchedStartTime,
                                    EndTime = schedule.SchedEndTime
                                };
                            }
                            else
                            {
                                _logger.LogInformation($"[VALIDATE SCHEDULE]   ❌ Day matched but time did NOT match");
                                _logger.LogInformation($"[VALIDATE SCHEDULE]   Reason: Class is {'<'}{(afterStart ? "AFTER" : "BEFORE")} start time OR {'>'}{(beforeEnd ? "BEFORE" : "AFTER")} end time");
                            }
                        }
                        else
                        {
                            _logger.LogDebug($"[VALIDATE SCHEDULE]   ❌ Day did NOT match - SKIPPING time check");
                        }
                    }
                }

                _logger.LogWarning($"[VALIDATE SCHEDULE] ❌ NO SCHEDULED CLASS FOUND");
                _logger.LogWarning($"[VALIDATE SCHEDULE] Summary:");
                _logger.LogWarning($"[VALIDATE SCHEDULE]   Total Schedules Checked: {totalSchedulesChecked}");
                _logger.LogWarning($"[VALIDATE SCHEDULE]   Matching Day: {matchingDayCount}");
                _logger.LogWarning($"[VALIDATE SCHEDULE]   Matching Day AND Time: {matchingTimeCount}");
                _logger.LogWarning($"[VALIDATE SCHEDULE]   Current: {currentDay} at {currentTimeOnly:hh\\:mm\\:ss}");
                _logger.LogInformation($"[VALIDATE SCHEDULE] ===== END VALIDATION (NO MATCH) =====");
                
                return new ScheduleStatusDto
                {
                    HasScheduledClass = false,
                    Message = $"No class scheduled at this time. You have no subject/course assigned for {currentDay} at {currentTime:h:mm tt}."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[VALIDATE SCHEDULE] EXCEPTION during validation for student {studId}");
                return new ScheduleStatusDto
                {
                    HasScheduledClass = false,
                    Message = "Unable to validate schedule. Please try again or contact support."
                };
            }
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
        public ScheduleStatusDto ScheduleStatus { get; set; } = new(); // ✅ NEW: Schedule validation result
        public IncompleteRecordResult IncompleteCheck { get; set; } = new();
        public AttendanceScanResponseDto? AttendanceResponse { get; set; }
    }

    public class ScannerStatusDto
    {
        public bool IsAvailable { get; set; }
        public String Status { get; set; } = string.Empty;
        public String Message { get; set; } = string.Empty;
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

    /// <summary>
    /// ✅ NEW: Schedule validation result
    /// </summary>
    public class ScheduleStatusDto
    {
        public bool HasScheduledClass { get; set; }
        public int? ClassId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}
