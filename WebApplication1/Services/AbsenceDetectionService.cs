using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Background service for detecting and marking absent students
    /// Runs at end of day to mark students who never timed in
    /// ✅ ENHANCED: Now also detects students who forgot to time out before class ended
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
        /// ✅ NEW METHOD: Check for students who forgot to time out after class ended
        /// Marks students as absent if they timed in but didn't time out before class end time
        /// This handles the scenario where students bypass attendance by forgetting timeout
        /// </summary>
        public async Task ProcessForgotTimeoutAbsencesAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation($"=== Processing Forgot Timeout Absences for {date:yyyy-MM-dd} ===");
                
                var dayOfWeek = date.DayOfWeek.ToString();
                _logger.LogInformation($"Day of week: {dayOfWeek}");

                // Get all enrolled classes
                var allEnrollments = await _enrollmentRepo.GetAllAsync();
                var classIds = allEnrollments.Select(e => e.ClassId).Distinct().ToList();
                
                _logger.LogInformation($"Found {classIds.Count} unique classes to check");

                int forgotTimeoutCount = 0;
                int validTimeoutCount = 0;

                foreach (var classId in classIds)
                {
                    try
                    {
                        var schedules = await _classScheduleRepo.GetByClassIdAsync(classId);
                        var todaySchedule = schedules.FirstOrDefault(s => 
                            s.SchedDayOfWeek.Trim().Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase));
                        
                        if (todaySchedule == null)
                        {
                            _logger.LogDebug($"Class {classId} has no schedule for {dayOfWeek}");
                            continue; // No class scheduled today
                        }
                        
                        var classEndTime = todaySchedule.SchedEndTime;
                        var currentTime = DateTime.Now.TimeOfDay;
                        
                        // Only process if class has ended
                        bool classHasEnded = date.Date < DateTime.Today || 
                                           (date.Date == DateTime.Today && currentTime > classEndTime);
                        
                        if (!classHasEnded)
                        {
                            _logger.LogDebug($"Class {classId} hasn't ended yet (ends at {classEndTime:hh\\:mm tt})");
                            continue; // Class still in progress or future
                        }
                        
                        _logger.LogInformation($"Processing Class {classId} - Ended at {classEndTime:hh\\:mm tt}");
                        
                        var processed = await ProcessClassForgotTimeoutAsync(classId, date, classEndTime);
                        forgotTimeoutCount += processed.forgotCount;
                        validTimeoutCount += processed.validCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing forgot timeout for Class ID: {classId}");
                        // Continue processing other classes
                    }
                }

                _logger.LogInformation($"=== Forgot Timeout Detection Complete ===");
                _logger.LogInformation($"Marked as Absent (Forgot Timeout): {forgotTimeoutCount} students");
                _logger.LogInformation($"Valid Timeouts: {validTimeoutCount} students");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing forgot timeout absences for {date:yyyy-MM-dd}");
                throw;
            }
        }

        /// <summary>
        /// ✅ NEW METHOD: Process forgot timeout for a single class
        /// Returns count of students marked absent and students with valid timeout
        /// </summary>
        private async Task<(int forgotCount, int validCount)> ProcessClassForgotTimeoutAsync(
            int classId, 
            DateTime date, 
            TimeSpan classEndTime)
        {
            int forgotCount = 0;
            int validCount = 0;
            
            var enrollments = await _enrollmentRepo.GetByClassIdAsync(classId);
            _logger.LogInformation($"Class {classId} has {enrollments.Count()} enrolled students");
            
            foreach (var enrollment in enrollments)
            {
                try
                {
                    var dailyRecord = await _dailyAttendanceRepo.GetByStudentAndDateAsync(enrollment.StudId, date);
                    
                    // Check if student timed in but forgot to time out
                    if (dailyRecord != null && dailyRecord.TimeIn != null)
                    {
                        if (dailyRecord.TimeOut == null)
                        {
                            // Student forgot to time out - mark as absent
                            _logger.LogWarning($"Student {enrollment.StudId} forgot to time out for Class {classId}");
                            
                            var existingRecord = await _attendanceRepo.GetByClassStudentDateAsync(
                                classId, 
                                enrollment.StudId, 
                                date);
                            
                            if (existingRecord == null)
                            {
                                // Create new absence record
                                _logger.LogInformation($"Creating absence record for Student {enrollment.StudId}");
                                
                                var absenceRecord = new Attendance
                                {
                                    ClassId = classId,
                                    StudId = enrollment.StudId,
                                    AttendDate = date,
                                    AttendScanTime = date, // Use date as placeholder
                                    AttendType = ScanType.In,
                                    AttendStatus = AttendanceStatus.Absent,
                                    AttendRemark = $"Absent - Forgot to time out before class ended at {classEndTime:hh\\:mm tt}. Timed in at {dailyRecord.TimeIn:hh\\:mm tt}.",
                                    AttendCreatedAt = DateTime.Now,
                                    DailyAttendId = dailyRecord.DailyAttendId,
                                    IsPresent = false
                                };
                                
                                await _attendanceRepo.AddAsync(absenceRecord);
                                forgotCount++;
                            }
                            else if (existingRecord.AttendStatus == AttendanceStatus.Present || 
                                   existingRecord.AttendStatus == AttendanceStatus.Late)
                            {
                                // Update existing record to absent
                                _logger.LogInformation($"Updating Student {enrollment.StudId} status from {existingRecord.AttendStatus} to Absent");
                                
                                existingRecord.AttendStatus = AttendanceStatus.Absent;
                                existingRecord.AttendRemark = $"Absent - Forgot to time out before class ended at {classEndTime:hh\\:mm tt}. Timed in at {dailyRecord.TimeIn:hh\\:mm tt}.";
                                existingRecord.IsPresent = false;
                                
                                await _attendanceRepo.UpdateAsync(existingRecord);
                                forgotCount++;
                            }
                            else
                            {
                                _logger.LogDebug($"Student {enrollment.StudId} already marked as {existingRecord.AttendStatus}");
                            }
                        }
                        else
                        {
                            // Student properly timed out
                            _logger.LogDebug($"Student {enrollment.StudId} has valid timeout at {dailyRecord.TimeOut:hh\\:mm tt}");
                            validCount++;
                        }
                    }
                    else
                    {
                        // Student never timed in (handled by main absence detection)
                        _logger.LogDebug($"Student {enrollment.StudId} never timed in (handled by main absence detection)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Student {enrollment.StudId} for Class {classId}");
                    // Continue processing other students
                }
            }
            
            return (forgotCount, validCount);
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
        /// ✅ ENHANCED: Now also processes forgot timeout absences
        /// </summary>
        public async Task ProcessAbsencesForTodayAsync()
        {
            // Process students who never timed in
            await ProcessAbsencesForDateAsync(DateTime.Today);
            
            // ✅ NEW: Process students who forgot to time out
            await ProcessForgotTimeoutAbsencesAsync(DateTime.Today);
        }
    }
}
