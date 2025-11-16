using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Service for detecting and marking late arrivals based on class schedules
    /// Triggered after a student's Time In is recorded
    /// STEP 10: Late Time-In Detection Logic
    /// </summary>
    public class LateDetectionService
    {
        private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo;
        private readonly IClassScheduleRepository _classScheduleRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ILogger<LateDetectionService> _logger;

        public LateDetectionService(
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IClassScheduleRepository classScheduleRepo,
            IAttendanceRepository attendanceRepo,
            ILogger<LateDetectionService> logger)
        {
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _classScheduleRepo = classScheduleRepo;
            _attendanceRepo = attendanceRepo;
            _logger = logger;
        }

        /// <summary>
        /// STEP 10: Check student's Time In against class schedules and mark late/present
        /// Called immediately after Time In is recorded
        /// ✅ FIXED: Now only processes classes that are currently active (within schedule time window)
        /// </summary>
        public async Task ProcessLateDetectionAsync(int dailyAttendId)
        {
            try
            {
                _logger.LogInformation($"=== Processing Late Detection for Daily Attend ID: {dailyAttendId} ===");

                // Get the daily attendance record
                var dailyRecord = await _dailyAttendanceRepo.GetByIdAsync(dailyAttendId);
                if (dailyRecord == null || dailyRecord.TimeIn == null)
                {
                    _logger.LogWarning($"Daily attendance record {dailyAttendId} not found or has no Time In");
                    return;
                }

                var studId = dailyRecord.StudId;
                var timeIn = dailyRecord.TimeIn.Value;
                var timeInTime = timeIn.TimeOfDay; // ✅ NEW: Extract time only for comparison
                var date = dailyRecord.AttendDate;

                _logger.LogInformation($"Student ID: {studId}, Time In: {timeIn:HH:mm:ss}, Date: {date:yyyy-MM-dd}");

                // Get student's schedule for today
                var todaySchedule = await _classScheduleRepo.GetStudentScheduleForDateAsync(studId, date);
                var scheduleList = todaySchedule.ToList();

                if (!scheduleList.Any())
                {
                    _logger.LogInformation($"No classes scheduled for Student {studId} on {date.DayOfWeek}");
                    return;
                }

                _logger.LogInformation($"Found {scheduleList.Count} class(es) scheduled for Student {studId} today");

                // ✅ CRITICAL FIX: Filter to only ACTIVE classes (where current time is within schedule window)
                var activeSchedules = scheduleList.Where(s => 
                    timeInTime >= s.StartTime && 
                    timeInTime <= s.EndTime
                ).ToList();

                _logger.LogInformation($"? SCHEDULE VALIDATION: {scheduleList.Count} total classes, {activeSchedules.Count} are currently ACTIVE at {timeInTime:hh\\:mm}");

                if (!activeSchedules.Any())
                {
                    _logger.LogWarning($"❌ No active classes at time {timeInTime:hh\\:mm}. Student timed in outside all class schedules.");
                    _logger.LogInformation("Available class schedules:");
                    foreach (var sched in scheduleList)
                    {
                        _logger.LogInformation($"  - {sched.CourseCode}: {sched.StartTime:hh\\:mm} - {sched.EndTime:hh\\:mm} (INACTIVE - outside time window)");
                    }
                    return;
                }

                _logger.LogInformation("Active classes at this time:");
                foreach (var sched in activeSchedules)
                {
                    _logger.LogInformation($"  ? {sched.CourseCode} ({sched.StartTime:hh\\:mm} - {sched.EndTime:hh\\:mm}) - ACTIVE");
                }

                // Process ONLY active classes
                foreach (var classSchedule in activeSchedules)
                {
                    await ProcessClassAttendanceAsync(
                        dailyRecord, 
                        classSchedule, 
                        timeIn);
                }

                _logger.LogInformation($"? Late detection processing complete for Daily Attend ID: {dailyAttendId}. Processed {activeSchedules.Count} active class(es).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing late detection for Daily Attend ID: {dailyAttendId}");
                throw;
            }
        }

        /// <summary>
        /// Process attendance for a single class
        /// Determines if student is Present or Late based on Time In
        /// </summary>
        private async Task ProcessClassAttendanceAsync(
            DailyAttendanceLog dailyRecord,
            StudentScheduleDto classSchedule,
            DateTime timeIn)
        {
            try
            {
                // Combine date with class start time for comparison
                var classStartDateTime = dailyRecord.AttendDate.Date + classSchedule.StartTime;

                _logger.LogInformation($"Checking class: {classSchedule.CourseCode} - Start Time: {classSchedule.StartTime}, Time In: {timeIn.TimeOfDay}");

                // Determine attendance status
                AttendanceStatus status;
                string remark;

                if (timeIn.TimeOfDay <= classSchedule.StartTime)
                {
                    // Student arrived on time or early
                    status = AttendanceStatus.Present;
                    remark = $"On time (Arrived: {timeIn:HH:mm:ss})";
                    _logger.LogInformation($"? PRESENT - Student arrived before/at start time");
                }
                else
                {
                    // Student arrived after class start time
                    status = AttendanceStatus.Late;
                    var minutesLate = (int)(timeIn.TimeOfDay - classSchedule.StartTime).TotalMinutes;
                    remark = $"Late by {minutesLate} minute(s) (Arrived: {timeIn:HH:mm:ss}, Class started: {classSchedule.StartTime})";
                    _logger.LogWarning($"?? LATE - Student arrived {minutesLate} minute(s) after start time");
                }

                // Check if attendance record already exists for this class/date
                var existingRecord = await _attendanceRepo.GetByClassStudentDateAsync(
                    classSchedule.ClassId,
                    dailyRecord.StudId,
                    dailyRecord.AttendDate);

                if (existingRecord != null)
                {
                    _logger.LogInformation($"Attendance record already exists (ID: {existingRecord.AttendId}), updating...");
                    
                    // Update existing record
                    existingRecord.AttendStatus = status;
                    existingRecord.AttendRemark = remark;
                    existingRecord.DailyAttendId = dailyRecord.DailyAttendId;
                    existingRecord.IsPresent = true;
                    existingRecord.AttendScanTime = timeIn;

                    await _attendanceRepo.UpdateAsync(existingRecord);
                }
                else
                {
                    _logger.LogInformation($"Creating new attendance record for class {classSchedule.ClassId}");
                    
                    // Create new attendance record
                    var newRecord = new Attendance
                    {
                        ClassId = classSchedule.ClassId,
                        StudId = dailyRecord.StudId,
                        AttendDate = dailyRecord.AttendDate,
                        AttendScanTime = timeIn,
                        AttendType = ScanType.In, // Kept for backward compatibility
                        AttendStatus = status,
                        AttendRemark = remark,
                        AttendCreatedAt = DateTime.Now,
                        DailyAttendId = dailyRecord.DailyAttendId,
                        IsPresent = true
                    };

                    await _attendanceRepo.AddAsync(newRecord);
                }

                _logger.LogInformation($"? Attendance recorded: {classSchedule.CourseCode} - Status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing attendance for class {classSchedule.ClassId}");
                // Continue processing other classes even if one fails
            }
        }
    }
}
