using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Service for managing daily student attendance (Time In/Time Out)
    /// Implements the three-case toggle logic for attendance recording
    /// </summary>
    public class DailyAttendanceService
    {
        private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly LateDetectionService _lateDetectionService; // ✅ NEW: Phase 4
        private readonly MissedTimeOutService _missedTimeOutService; // ✅ NEW: Phase 5
        private readonly ILogger<DailyAttendanceService> _logger;

        public DailyAttendanceService(
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IStudentRepository studentRepo,
            LateDetectionService lateDetectionService, // ✅ NEW: Phase 4
            MissedTimeOutService missedTimeOutService, // ✅ NEW: Phase 5
            ILogger<DailyAttendanceService> logger)
        {
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _studentRepo = studentRepo;
            _lateDetectionService = lateDetectionService; // ✅ NEW: Phase 4
            _missedTimeOutService = missedTimeOutService; // ✅ NEW: Phase 5
            _logger = logger;
        }

        /// <summary>
        /// STEP 5: Query daily attendance record for a student on a specific date
        /// Returns null if no record exists for that date
        /// </summary>
        public async Task<DailyAttendanceLog?> GetDailyRecordAsync(int studId, DateTime date)
        {
            try
            {
                _logger.LogInformation($"Querying daily attendance for Student ID: {studId}, Date: {date:yyyy-MM-dd}");
                
                var record = await _dailyAttendanceRepo.GetByStudentAndDateAsync(studId, date.Date);
                
                if (record == null)
                {
                    _logger.LogInformation($"No attendance record found for Student ID: {studId} on {date:yyyy-MM-dd}");
                }
                else
                {
                    _logger.LogInformation($"Found attendance record - Time In: {record.TimeIn?.ToString("HH:mm:ss") ?? "NULL"}, Time Out: {record.TimeOut?.ToString("HH:mm:ss") ?? "NULL"}, Status: {record.Status}");
                }
                
                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error querying daily attendance for Student ID: {studId}");
                throw;
            }
        }

        /// <summary>
        /// Get all attendance records for a specific date (for reports)
        /// </summary>
        public async Task<IEnumerable<DailyAttendanceLog>> GetAllRecordsByDateAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation($"Retrieving all attendance records for {date:yyyy-MM-dd}");
                return await _dailyAttendanceRepo.GetByDateAsync(date.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving attendance records for {date:yyyy-MM-dd}");
                throw;
            }
        }

        /// <summary>
        /// MAIN LOGIC: Process attendance scan and determine action (Time In/Time Out/Already Completed)
        /// This is the core three-case toggle implementation
        /// </summary>
        public async Task<AttendanceScanResponseDto> ProcessAttendanceScanAsync(Student student)
        {
            try
            {
                _logger.LogInformation($"=== Processing Attendance Scan for Student: {student.StudNumber} ===");
                
                var today = DateTime.Today;
                var now = DateTime.Now;

                // ✅ NEW: Check for incomplete previous day records using MissedTimeOutService
                var incompleteResult = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    student.StudId, 
                    today);

                // Query today's attendance record
                var todayRecord = await GetDailyRecordAsync(student.StudId, today);

                // Determine which case applies
                AttendanceScanResponseDto response;
                
                if (todayRecord == null)
                {
                    // CASE 1: No record exists → TIME IN
                    response = await ProcessTimeInAsync(student, today, now);
                }
                else if (todayRecord.TimeIn != null && todayRecord.TimeOut == null)
                {
                    // CASE 2: Time In exists but no Time Out → TIME OUT
                    response = await ProcessTimeOutAsync(student, todayRecord, now);
                }
                else
                {
                    // CASE 3: Both Time In and Time Out exist → ALREADY COMPLETED
                    response = ProcessAlreadyCompleted(student, todayRecord);
                }

                // ✅ NEW: Add warning message if incomplete records were found
                if (incompleteResult.HasIncompleteRecords && !string.IsNullOrEmpty(incompleteResult.WarningMessage))
                {
                    response.Message = $"{response.Message}\n\n⚠️ {incompleteResult.WarningMessage}";
                    response.IncompleteRecordsWarning = incompleteResult.WarningMessage;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing attendance scan for Student ID: {student.StudId}");
                throw;
            }
        }

        /// <summary>
        /// Helper: Check for incomplete records from previous days
        /// Flags records where student forgot to time out
        /// DEPRECATED: Replaced by MissedTimeOutService in Phase 5
        /// </summary>
        private async Task CheckAndFlagIncompleteRecordsAsync(int studId)
        {
            // This method is now deprecated and replaced by MissedTimeOutService
            // Kept for backward compatibility but should not be called
            _logger.LogWarning("CheckAndFlagIncompleteRecordsAsync is deprecated. Use MissedTimeOutService instead.");
        }

        // ============================================================
        // STEP 6: CASE 1 - First Scan of Day (TIME IN)
        // ============================================================
        
        /// <summary>
        /// CASE 1: No attendance record exists for today
        /// Action: Create new record with Time In timestamp
        /// </summary>
        private async Task<AttendanceScanResponseDto> ProcessTimeInAsync(
            Student student, 
            DateTime date, 
            DateTime timestamp)
        {
            _logger.LogInformation($"CASE 1: No record exists. Processing TIME IN for Student {student.StudNumber}");

            try
            {
                // Create new daily attendance record
                var newRecord = new DailyAttendanceLog
                {
                    StudId = student.StudId,
                    AttendDate = date,
                    TimeIn = timestamp,
                    TimeOut = null,
                    Status = "active",
                    ForgotTimeout = false,
                    Remarks = null,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                };

                // Save to database
                await _dailyAttendanceRepo.AddAsync(newRecord);
                await _dailyAttendanceRepo.SaveChangesAsync();

                _logger.LogInformation($"✅ TIME IN recorded successfully at {timestamp:HH:mm:ss} for Student {student.StudNumber}");

                // ✅ NEW: Trigger late detection logic (Phase 4)
                try
                {
                    _logger.LogInformation($"🔍 Triggering late detection for Daily Attend ID: {newRecord.DailyAttendId}");
                    await _lateDetectionService.ProcessLateDetectionAsync(newRecord.DailyAttendId);
                    _logger.LogInformation($"✅ Late detection processing completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ Late detection failed, but Time In was recorded successfully");
                    // Don't fail the entire operation if late detection fails
                }

                // Build response
                var response = new AttendanceScanResponseDto
                {
                    Success = true,
                    Message = $"Good morning, {student.User?.UserFirstName}! Time In recorded successfully.",
                    Action = "time_in",
                    Timestamp = timestamp,
                    Student = new StudentInfoDto
                    {
                        StudId = student.StudId,
                        StudNumber = student.StudNumber,
                        FullName = $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim(),
                        ProgramName = student.Program?.ProgramName,
                        SectionName = student.Section?.SectionName
                    }
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process TIME IN for Student {student.StudNumber}");
                throw;
            }
        }

        // ============================================================
        // STEP 7: CASE 2 - Second Scan of Day (TIME OUT)
        // ============================================================
        
        /// <summary>
        /// CASE 2: Time In exists but Time Out is null
        /// Action: Update existing record with Time Out timestamp
        /// </summary>
        private async Task<AttendanceScanResponseDto> ProcessTimeOutAsync(
            Student student, 
            DailyAttendanceLog existingRecord, 
            DateTime timestamp)
        {
            _logger.LogInformation($"CASE 2: Time In exists ({existingRecord.TimeIn:HH:mm:ss}), no Time Out. Processing TIME OUT for Student {student.StudNumber}");

            try
            {
                // Calculate duration
                var duration = timestamp - existingRecord.TimeIn!.Value;
                var hours = (int)duration.TotalHours;
                var minutes = duration.Minutes;

                // Update the record
                existingRecord.TimeOut = timestamp;
                existingRecord.Status = "completed";
                existingRecord.UpdatedAt = timestamp;
                
                if (string.IsNullOrEmpty(existingRecord.Remarks))
                {
                    existingRecord.Remarks = $"Total time on campus: {hours}h {minutes}m";
                }
                else
                {
                    existingRecord.Remarks += $" | Total time: {hours}h {minutes}m";
                }

                // Save to database
                await _dailyAttendanceRepo.UpdateAsync(existingRecord);
                await _dailyAttendanceRepo.SaveChangesAsync();

                _logger.LogInformation($"✅ TIME OUT recorded successfully at {timestamp:HH:mm:ss} for Student {student.StudNumber}. Duration: {hours}h {minutes}m");

                // Build response
                return new AttendanceScanResponseDto
                {
                    Success = true,
                    Message = $"Goodbye, {student.User?.UserFirstName}! Time Out recorded. You were on campus for {hours} hours and {minutes} minutes today.",
                    Action = "time_out",
                    Timestamp = timestamp,
                    Student = new StudentInfoDto
                    {
                        StudId = student.StudId,
                        StudNumber = student.StudNumber,
                        FullName = $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim(),
                        ProgramName = student.Program?.ProgramName,
                        SectionName = student.Section?.SectionName
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process TIME OUT for Student {student.StudNumber}");
                throw;
            }
        }

        // ============================================================
        // STEP 8: CASE 3 - Already Completed (REJECT SCAN)
        // ============================================================
        
        /// <summary>
        /// CASE 3: Both Time In and Time Out already exist for today
        /// Action: Reject the scan with informative message
        /// </summary>
        private AttendanceScanResponseDto ProcessAlreadyCompleted(
            Student student, 
            DailyAttendanceLog existingRecord)
        {
            _logger.LogInformation($"CASE 3: Attendance already completed. Time In: {existingRecord.TimeIn:HH:mm:ss}, Time Out: {existingRecord.TimeOut:HH:mm:ss}");

            var timeInFormatted = existingRecord.TimeIn?.ToString("h:mm tt");
            var timeOutFormatted = existingRecord.TimeOut?.ToString("h:mm tt");

            return new AttendanceScanResponseDto
            {
                Success = false,
                Message = $"You have already completed attendance today, {student.User?.UserFirstName}. Time In: {timeInFormatted}, Time Out: {timeOutFormatted}. See you tomorrow!",
                Action = "already_completed",
                Timestamp = null,
                Student = new StudentInfoDto
                {
                    StudId = student.StudId,
                    StudNumber = student.StudNumber,
                    FullName = $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim(),
                    ProgramName = student.Program?.ProgramName,
                    SectionName = student.Section?.SectionName
                }
            };
        }

        /// <summary>
        /// Get summary statistics for today's attendance
        /// </summary>
        public async Task<DailyAttendanceSummaryDto> GetTodaySummaryAsync()
        {
            try
            {
                var today = DateTime.Today;
                var allRecords = await GetAllRecordsByDateAsync(today);
                var recordsList = allRecords.ToList();

                return new DailyAttendanceSummaryDto
                {
                    Date = today,
                    TotalStudents = recordsList.Count,
                    TimedIn = recordsList.Count(r => r.TimeIn != null),
                    TimedOut = recordsList.Count(r => r.TimeOut != null),
                    StillOnCampus = recordsList.Count(r => r.TimeIn != null && r.TimeOut == null),
                    IncompleteRecords = recordsList.Count(r => r.ForgotTimeout)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating today's attendance summary");
                throw;
            }
        }
    }
}
