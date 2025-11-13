using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Background service for detecting and marking absent students
    /// Runs at end of day to mark students who never timed in
    /// STEP 11: Absence Detection Batch Process
    /// </summary>
    public class AbsenceDetectionService
    {
        private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo;
        private readonly IClassScheduleRepository _classScheduleRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ILogger<AbsenceDetectionService> _logger;

        public AbsenceDetectionService(
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IClassScheduleRepository classScheduleRepo,
            IEnrollmentRepository enrollmentRepo,
            IAttendanceRepository attendanceRepo,
            ILogger<AbsenceDetectionService> logger)
        {
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _classScheduleRepo = classScheduleRepo;
            _enrollmentRepo = enrollmentRepo;
            _attendanceRepo = attendanceRepo;
            _logger = logger;
        }

        /// <summary>
        /// STEP 11: Mark absent students for a specific date
        /// Should be run at end of day (e.g., 11:59 PM)
        /// </summary>
        public async Task ProcessAbsencesForDateAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation($"=== Starting Absence Detection for {date:yyyy-MM-dd} ===");

                var dayOfWeek = date.DayOfWeek.ToString();
                _logger.LogInformation($"Day of week: {dayOfWeek}");

                // Get all students with classes scheduled for this day
                var studentsWithClasses = await GetStudentsWithScheduledClassesAsync(dayOfWeek);
                var studentsList = studentsWithClasses.ToList();

                _logger.LogInformation($"Found {studentsList.Count} unique students with classes on {dayOfWeek}");

                if (!studentsList.Any())
                {
                    _logger.LogInformation("No students have classes scheduled for this day");
                    return;
                }

                int absentCount = 0;
                int presentCount = 0;

                // Check each student's attendance
                foreach (var studId in studentsList)
                {
                    try
                    {
                        var wasMarkedAbsent = await ProcessStudentAbsenceAsync(studId, date);
                        if (wasMarkedAbsent)
                        {
                            absentCount++;
                        }
                        else
                        {
                            presentCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing absence for Student ID: {studId}");
                        // Continue processing other students
                    }
                }

                _logger.LogInformation($"=== Absence Detection Complete ===");
                _logger.LogInformation($"Present: {presentCount} students");
                _logger.LogInformation($"Absent: {absentCount} students");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing absences for {date:yyyy-MM-dd}");
                throw;
            }
        }

        /// <summary>
        /// Get list of unique student IDs who have classes scheduled for a specific day
        /// </summary>
        private async Task<IEnumerable<int>> GetStudentsWithScheduledClassesAsync(string dayOfWeek)
        {
            // Get all enrolled students
            var allEnrollments = await _enrollmentRepo.GetAllAsync();
            var studentIds = allEnrollments.Select(e => e.StudId).Distinct();

            var studentsWithClassesToday = new List<int>();

            // Check each student's schedule
            foreach (var studId in studentIds)
            {
                var schedule = await _classScheduleRepo.GetStudentScheduleForDayAsync(studId, dayOfWeek);
                if (schedule.Any())
                {
                    studentsWithClassesToday.Add(studId);
                }
            }

            return studentsWithClassesToday;
        }

        /// <summary>
        /// Process absence for a single student on a specific date
        /// Returns true if student was marked absent
        /// </summary>
        private async Task<bool> ProcessStudentAbsenceAsync(int studId, DateTime date)
        {
            // Check if student has a Time In record for this date
            var dailyRecord = await _dailyAttendanceRepo.GetByStudentAndDateAsync(studId, date);

            if (dailyRecord != null && dailyRecord.TimeIn != null)
            {
                _logger.LogDebug($"Student {studId} has Time In record - PRESENT");
                return false; // Student was present
            }

            _logger.LogInformation($"Student {studId} has NO Time In record - marking ABSENT");

            // Get all classes student should have attended today
            var todaySchedule = await _classScheduleRepo.GetStudentScheduleForDateAsync(studId, date);
            var scheduleList = todaySchedule.ToList();

            if (!scheduleList.Any())
            {
                _logger.LogDebug($"Student {studId} has no classes scheduled for {date:yyyy-MM-dd}");
                return false;
            }

            _logger.LogInformation($"Marking Student {studId} absent for {scheduleList.Count} class(es)");

            // Mark absent for each scheduled class
            foreach (var classSchedule in scheduleList)
            {
                await MarkClassAbsentAsync(studId, classSchedule, date);
            }

            return true; // Student was marked absent
        }

        /// <summary>
        /// Mark student as absent for a specific class
        /// </summary>
        private async Task MarkClassAbsentAsync(int studId, StudentScheduleDto classSchedule, DateTime date)
        {
            try
            {
                // Check if record already exists
                var existingRecord = await _attendanceRepo.GetByClassStudentDateAsync(
                    classSchedule.ClassId,
                    studId,
                    date);

                if (existingRecord != null)
                {
                    // Update existing record to absent (in case it was marked pending)
                    if (existingRecord.AttendStatus != AttendanceStatus.Present && 
                        existingRecord.AttendStatus != AttendanceStatus.Late)
                    {
                        _logger.LogInformation($"Updating attendance record {existingRecord.AttendId} to ABSENT");
                        existingRecord.AttendStatus = AttendanceStatus.Absent;
                        existingRecord.AttendRemark = $"No Time In recorded for {date:yyyy-MM-dd}";
                        existingRecord.IsPresent = false;
                        existingRecord.DailyAttendId = null; // No daily record exists

                        await _attendanceRepo.UpdateAsync(existingRecord);
                    }
                    else
                    {
                        _logger.LogDebug($"Student {studId} already marked Present/Late for class {classSchedule.ClassId}");
                    }
                }
                else
                {
                    // Create new absence record
                    _logger.LogInformation($"Creating absence record: Student {studId}, Class: {classSchedule.CourseCode}");
                    
                    var absenceRecord = new Attendance
                    {
                        ClassId = classSchedule.ClassId,
                        StudId = studId,
                        AttendDate = date,
                        AttendScanTime = date, // Use date as placeholder
                        AttendType = ScanType.In, // Kept for compatibility
                        AttendStatus = AttendanceStatus.Absent,
                        AttendRemark = $"Absent - No Time In recorded for {date:yyyy-MM-dd}",
                        AttendCreatedAt = DateTime.Now,
                        DailyAttendId = null, // No daily record exists
                        IsPresent = false
                    };

                    await _attendanceRepo.AddAsync(absenceRecord);
                }

                _logger.LogInformation($"? Marked absent: {classSchedule.CourseCode} for Student {studId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking class {classSchedule.ClassId} absent for Student {studId}");
                throw;
            }
        }

        /// <summary>
        /// Manual trigger for processing absences (for testing or manual runs)
        /// </summary>
        public async Task ProcessAbsencesForTodayAsync()
        {
            await ProcessAbsencesForDateAsync(DateTime.Today);
        }
    }
}
