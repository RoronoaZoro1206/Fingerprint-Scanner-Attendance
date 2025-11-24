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
        /// ✅ FIXED: Proper time boundary checking - only processes classes that are currently active
        /// ✅ ENHANCED: Now properly handles class_id from daily_attendance_log
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
                var timeInTime = timeIn.TimeOfDay; // ✅ Extract time only for comparison
                var date = dailyRecord.AttendDate;
                var classId = dailyRecord.ClassId; // ✅ Get the class ID from the record

                _logger.LogInformation($"Student ID: {studId}, Time In: {timeIn:HH:mm:ss}, Date: {date:yyyy-MM-dd}, Class ID: {classId?.ToString() ?? "NULL"}");

                // ✅ CRITICAL FIX: If ClassId is present, only process attendance for THAT SPECIFIC CLASS
                if (classId.HasValue)
                {
                    _logger.LogInformation($"Processing class-specific attendance for Class ID: {classId}");
                    
                    // Get schedule for this specific class
                    var schedules = await _classScheduleRepo.GetByClassIdAsync(classId.Value);
                    var scheduleList = schedules.ToList();
                    
                    if (!scheduleList.Any())
                    {
                        _logger.LogWarning($"No schedules found for Class ID: {classId}");
                        return;
                    }
                    
                    // Process ONLY the schedule that matches today's day of week
                    var todayDayOfWeek = date.DayOfWeek.ToString();
                    var todaySchedule = scheduleList.FirstOrDefault(s => 
                        s.SchedDayOfWeek.Trim().Equals(todayDayOfWeek, StringComparison.OrdinalIgnoreCase));
                    
                    if (todaySchedule == null)
                    {
                        _logger.LogWarning($"Class {classId} has no schedule for {todayDayOfWeek}");
                        return;
                    }
                    
                    // ✅ CRITICAL FIX: Proper time boundary checking
                    // A class is active if: time in >= start time AND time in <= end time
                    bool timeMatch = timeInTime >= todaySchedule.SchedStartTime && timeInTime <= todaySchedule.SchedEndTime;
                    
                    if (!timeMatch)
                    {
                        _logger.LogWarning($"Time In {timeInTime:hh\\:mm} is outside class schedule {todaySchedule.SchedStartTime:hh\\:mm} - {todaySchedule.SchedEndTime:hh\\:mm}");
                        return;
                    }
                    
                    // Build StudentScheduleDto for this class
                    // (You'll need to get course details from the class repository)
                    var classScheduleDto = new StudentScheduleDto
                    {
                        ClassId = classId.Value,
                        StartTime = todaySchedule.SchedStartTime,
                        EndTime = todaySchedule.SchedEndTime,
                        CourseCode = "N/A", // Will be filled by ProcessClassAttendanceAsync
                        DayOfWeek = todayDayOfWeek
                    };
                    
                    await ProcessClassAttendanceAsync(dailyRecord, classScheduleDto, timeIn);
                    
                    _logger.LogInformation($"✅ Late detection processing complete for Class ID: {classId}");
                    return;
                }

                // ✅ FALLBACK: If no ClassId (old records), process all scheduled classes for today
                _logger.LogInformation("No ClassId found - processing all scheduled classes for today (legacy mode)");
                
                // Get student's schedule for today
                var todayScheduleAll = await _classScheduleRepo.GetStudentScheduleForDateAsync(studId, date);
                var scheduleListAll = todayScheduleAll.ToList();

                if (!scheduleListAll.Any())
                {
                    _logger.LogInformation($"No classes scheduled for Student {studId} on {date.DayOfWeek}");
                    return;
                }

                _logger.LogInformation($"Found {scheduleListAll.Count} class(es) scheduled for Student {studId} today");

                // ✅ CRITICAL FIX: Filter to only ACTIVE classes (where current time is within schedule window)
                // A class is active if: time in >= start time AND time in <= end time
                var activeSchedules = scheduleListAll.Where(s => 
                    timeInTime >= s.StartTime && 
                    timeInTime <= s.EndTime
                ).ToList();

                _logger.LogInformation($"🔍 SCHEDULE VALIDATION: {scheduleListAll.Count} total classes, {activeSchedules.Count} are currently ACTIVE at {timeInTime:hh\\:mm}");

                if (!activeSchedules.Any())
                {
                    _logger.LogWarning($"❌ No active classes at time {timeInTime:hh\\:mm}. Student timed in outside all class schedules.");
                    _logger.LogInformation("Available class schedules:");
                    foreach (var sched in scheduleListAll)
                    {
                        _logger.LogInformation($"  - {sched.CourseCode}: {sched.StartTime:hh\\:mm} - {sched.EndTime:hh\\:mm} (INACTIVE - outside time window)");
                    }
                    return;
                }

                _logger.LogInformation("Active classes at this time:");
                foreach (var sched in activeSchedules)
                {
                    _logger.LogInformation($"  ✅ {sched.CourseCode} ({sched.StartTime:hh\\:mm} - {sched.EndTime:hh\\:mm}) - ACTIVE");
                }

                // Process ONLY active classes
                foreach (var classSchedule in activeSchedules)
                {
                    await ProcessClassAttendanceAsync(
                        dailyRecord, 
                        classSchedule, 
                        timeIn);
                }

                _logger.LogInformation($"✅ Late detection processing complete for Daily Attend ID: {dailyAttendId}. Processed {activeSchedules.Count} active class(es).");
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
