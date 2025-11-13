using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Service for detecting and handling missed time out scenarios
    /// Flags incomplete records and notifies administrators
    /// STEP 12: Missed Time Out Handler
    /// </summary>
    public class MissedTimeOutService
    {
        private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly ILogger<MissedTimeOutService> _logger;

        public MissedTimeOutService(
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IStudentRepository studentRepo,
            ILogger<MissedTimeOutService> logger)
        {
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _studentRepo = studentRepo;
            _logger = logger;
        }

        /// <summary>
        /// STEP 12: Check for incomplete previous records before processing new attendance
        /// Flags records where student forgot to time out
        /// </summary>
        public async Task<IncompleteRecordResult> CheckAndFlagIncompleteRecordsAsync(int studId, DateTime currentDate)
        {
            try
            {
                _logger.LogInformation($"Checking for incomplete records for Student ID: {studId}");

                // Get all incomplete records from previous days
                var incompleteRecords = await _dailyAttendanceRepo.GetIncompleteRecordsAsync();
                var studentIncomplete = incompleteRecords
                    .Where(r => r.StudId == studId && r.AttendDate < currentDate.Date)
                    .OrderByDescending(r => r.AttendDate)
                    .ToList();

                if (!studentIncomplete.Any())
                {
                    _logger.LogInformation($"No incomplete records found for Student ID: {studId}");
                    return new IncompleteRecordResult
                    {
                        HasIncompleteRecords = false,
                        IncompleteCount = 0
                    };
                }

                _logger.LogWarning($"Found {studentIncomplete.Count} incomplete record(s) for Student ID: {studId}");

                var flaggedRecords = new List<IncompleteRecordDto>();

                // Flag each incomplete record
                foreach (var record in studentIncomplete)
                {
                    var daysAgo = (currentDate.Date - record.AttendDate).Days;
                    
                    _logger.LogWarning($"Flagging incomplete record - Date: {record.AttendDate:yyyy-MM-dd}, Time In: {record.TimeIn:HH:mm:ss}, Days Ago: {daysAgo}");

                    // Update the record
                    record.ForgotTimeout = true;
                    record.Status = "incomplete";
                    record.Remarks = string.IsNullOrEmpty(record.Remarks)
                        ? $"Forgot to time out. Flagged on {currentDate:yyyy-MM-dd HH:mm:ss}"
                        : $"{record.Remarks} | Flagged on {currentDate:yyyy-MM-dd HH:mm:ss}";
                    record.UpdatedAt = DateTime.Now;

                    await _dailyAttendanceRepo.UpdateAsync(record);

                    // Get student info for notification
                    var student = await _studentRepo.GetByIdAsync(studId);
                    
                    flaggedRecords.Add(new IncompleteRecordDto
                    {
                        DailyAttendId = record.DailyAttendId,
                        StudId = studId,
                        StudNumber = student?.StudNumber ?? "UNKNOWN",
                        StudentName = student != null 
                            ? $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim()
                            : "Unknown Student",
                        AttendDate = record.AttendDate,
                        TimeIn = record.TimeIn!.Value,
                        DaysAgo = $"{daysAgo} day(s) ago"
                    });
                }

                await _dailyAttendanceRepo.SaveChangesAsync();

                _logger.LogInformation($"Successfully flagged {flaggedRecords.Count} incomplete record(s)");

                return new IncompleteRecordResult
                {
                    HasIncompleteRecords = true,
                    IncompleteCount = flaggedRecords.Count,
                    FlaggedRecords = flaggedRecords,
                    WarningMessage = GenerateWarningMessage(flaggedRecords)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking incomplete records for Student ID: {studId}");
                // Don't throw - this shouldn't block current attendance recording
                return new IncompleteRecordResult
                {
                    HasIncompleteRecords = false,
                    IncompleteCount = 0,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Generate user-friendly warning message for incomplete records
        /// </summary>
        private string GenerateWarningMessage(List<IncompleteRecordDto> records)
        {
            if (records.Count == 1)
            {
                var record = records.First();
                return $"Note: You forgot to time out on {record.AttendDate:MMM dd, yyyy}. This has been flagged for admin review.";
            }
            else
            {
                return $"Note: You have {records.Count} incomplete attendance records that have been flagged for admin review.";
            }
        }

        /// <summary>
        /// Get all incomplete records for admin review
        /// </summary>
        public async Task<IEnumerable<IncompleteRecordDto>> GetAllIncompleteRecordsAsync()
        {
            try
            {
                var incompleteRecords = await _dailyAttendanceRepo.GetIncompleteRecordsAsync();
                var result = new List<IncompleteRecordDto>();

                foreach (var record in incompleteRecords)
                {
                    var student = await _studentRepo.GetByIdAsync(record.StudId);
                    var daysAgo = (DateTime.Today - record.AttendDate).Days;

                    result.Add(new IncompleteRecordDto
                    {
                        DailyAttendId = record.DailyAttendId,
                        StudId = record.StudId,
                        StudNumber = student?.StudNumber ?? "UNKNOWN",
                        StudentName = student != null 
                            ? $"{student.User?.UserFirstName} {student.User?.UserLastName}".Trim()
                            : "Unknown Student",
                        AttendDate = record.AttendDate,
                        TimeIn = record.TimeIn!.Value,
                        DaysAgo = $"{daysAgo} day(s) ago"
                    });
                }

                return result.OrderByDescending(r => r.AttendDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incomplete records");
                throw;
            }
        }

        /// <summary>
        /// Manually resolve an incomplete record (admin action)
        /// </summary>
        public async Task<bool> ResolveIncompleteRecordAsync(int dailyAttendId, DateTime manualTimeOut, int adminUserId)
        {
            try
            {
                var record = await _dailyAttendanceRepo.GetByIdAsync(dailyAttendId);
                
                if (record == null)
                {
                    _logger.LogWarning($"Record {dailyAttendId} not found");
                    return false;
                }

                _logger.LogInformation($"Admin {adminUserId} manually resolving incomplete record {dailyAttendId}");

                record.TimeOut = manualTimeOut;
                record.Status = "completed";
                record.ForgotTimeout = false;
                record.Remarks = string.IsNullOrEmpty(record.Remarks)
                    ? $"Manually resolved by admin on {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    : $"{record.Remarks} | Resolved by admin on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                record.UpdatedAt = DateTime.Now;

                await _dailyAttendanceRepo.UpdateAsync(record);
                await _dailyAttendanceRepo.SaveChangesAsync();

                _logger.LogInformation($"Successfully resolved incomplete record {dailyAttendId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving incomplete record {dailyAttendId}");
                return false;
            }
        }
    }

    /// <summary>
    /// Result of incomplete record check
    /// </summary>
    public class IncompleteRecordResult
    {
        public bool HasIncompleteRecords { get; set; }
        public int IncompleteCount { get; set; }
        public List<IncompleteRecordDto>? FlaggedRecords { get; set; }
        public string? WarningMessage { get; set; }
        public string? Error { get; set; }
    }
}
