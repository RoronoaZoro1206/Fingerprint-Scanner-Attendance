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
        private readonly IClassScheduleRepository _scheduleRepo; // ✅ NEW: Schedule validation
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
            IClassScheduleRepository scheduleRepo, // ✅ NEW: Inject schedule repository
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
            _scheduleRepo = scheduleRepo; // ✅ NEW: Store schedule repository
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

                // ✅ NEW STEP 5: Validate Schedule (Only for Time In)
                result.ScheduleStatus = await ValidateScheduleAsync(result.Student.StudId);
                if (!result.ScheduleStatus.HasScheduledClass)
                {
                    throw new NoScheduleException(result.Student.StudId, result.ScheduleStatus.Message);
                }

                // STEP 6: Check Incomplete Records
                result.IncompleteCheck = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    result.Student.StudId,
                    DateTime.Today);

                // STEP 7: Process Attendance (Time In/Out)
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
                    DateTime.Today);

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
                    DateTime.Today);

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
        /// ✅ FIXED: Validate student has a scheduled class at current time
        /// This ensures students can only time in when they have an actual class
        /// CRITICAL FIX: Proper time boundary handling for edge cases like 12:03 PM
        /// </summary>
        private async Task<ScheduleStatusDto> ValidateScheduleAsync(int studId)
        {
            try
            {
                var currentTime = DateTime.Now;
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
                                    DayOfWeek = schedule.SchedDayOfWeek,
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
