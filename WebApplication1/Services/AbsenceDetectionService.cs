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
        private readonly PhilippineTimeService _pht;

        public AbsenceDetectionService(
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IClassScheduleRepository classScheduleRepo,
            IEnrollmentRepository enrollmentRepo,
            IAttendanceRepository attendanceRepo,
            ILogger<AbsenceDetectionService> logger,
            PhilippineTimeService pht)
        {
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _classScheduleRepo = classScheduleRepo;
            _enrollmentRepo = enrollmentRepo;
            _attendanceRepo = attendanceRepo;
            _logger = logger;
            _pht = pht;
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
        /// ✅ CRITICAL FIX: Properly format TimeSpan in logging (lines 142, 145) - convert to DateTime first
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
                        var currentTime = _pht.TimeOfDay; // Use PHT for schedule comparison
                        
                        // Only process if class has ended (using PHT)
                        bool classHasEnded = date.Date < _pht.Today || 
                                           (date.Date == _pht.Today && currentTime > classEndTime);
                        
                        if (!classHasEnded)
                        {
                            // ✅ CRITICAL FIX: Convert TimeSpan to DateTime before formatting with AM/PM
                            var classEndTimeFormatted = _pht.Today.Add(classEndTime).ToString("hh:mm tt");
                            _logger.LogDebug($"Class {classId} hasn't ended yet (ends at {classEndTimeFormatted})");
                            continue; // Class still in progress or future
                        }
                        
                        // ✅ CRITICAL FIX: Convert TimeSpan to DateTime before formatting with AM/PM
                        var classEndTimeFormattedLog = _pht.Today.Add(classEndTime).ToString("hh:mm tt");
                        _logger.LogInformation($"Processing Class {classId} - Ended at {classEndTimeFormattedLog}");
                        
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
        /// ✅ FIX: Only update the EXISTING attendance record, don't create new ones
        /// ✅ FIX: Only mark absent if TimeOut is NULL (no timeout recorded)
        /// ✅ FIX: Skip records that have COMPLETE attendance (both TimeIn AND TimeOut)
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
                    // ✅ FIX: Get the ACTIVE (non-invalidated) daily attendance record
                    var dailyRecord = await _dailyAttendanceRepo.GetActiveByStudentClassAndDateAsync(enrollment.StudId, classId, date);
                    
                    // Skip if student never timed in at all
                    if (dailyRecord == null || dailyRecord.TimeIn == null)
                    {
                        _logger.LogDebug($"Student {enrollment.StudId} never timed in for class {classId} (handled by main absence detection)");
                        continue;
                    }
                    
                    // ✅ CRITICAL FIX: If student has BOTH TimeIn AND TimeOut, SKIP completely
                    // These are COMPLETE records - student attended properly
                    if (dailyRecord.TimeOut != null)
                    {
                        string timeInFormatted = dailyRecord.TimeIn.Value.ToString("hh:mm:ss tt");
                        string timeOutFormatted = dailyRecord.TimeOut.Value.ToString("hh:mm:ss tt");
                        _logger.LogInformation($"✅ Student {enrollment.StudId} has COMPLETE attendance - TimeIn: {timeInFormatted}, TimeOut: {timeOutFormatted}. SKIPPING.");
                        validCount++;
                        continue;
                    }
                    
                    // ✅ At this point: Student has TimeIn but NO TimeOut (NULL) - FORGOT TO TIME OUT
                    string classEndTimeFormatted = _pht.Today.Add(classEndTime).ToString("hh:mm tt");
                    string studentTimeInFormatted = dailyRecord.TimeIn.Value.ToString("hh:mm tt");
                    
                    _logger.LogWarning($"⚠️ Student {enrollment.StudId} forgot to time out for Class {classId}. TimeIn: {studentTimeInFormatted}, TimeOut: NULL, Class ended: {classEndTimeFormatted}");
                    
                    // ✅ FIX: Get the EXISTING attendance record - DON'T create a new one
                    var existingRecord = await _attendanceRepo.GetByClassStudentDateAsync(
                        classId, 
                        enrollment.StudId, 
                        date);
                    
                    if (existingRecord == null)
                    {
                        // No existing attendance record - this is unusual, log it but don't create duplicates
                        _logger.LogWarning($"⚠️ No existing attendance record found for Student {enrollment.StudId}, Class {classId}. Skipping to avoid duplicates.");
                        continue;
                    }
                    
                    // ✅ Check if faculty manually modified this record
                    bool isFacultyOverride = !string.IsNullOrEmpty(existingRecord.AttendRemark) &&
                        (existingRecord.AttendRemark.Contains("by faculty", StringComparison.OrdinalIgnoreCase) ||
                         existingRecord.AttendRemark.Contains("Corrected by faculty", StringComparison.OrdinalIgnoreCase) ||
                         existingRecord.AttendRemark.Contains("Manually set", StringComparison.OrdinalIgnoreCase) ||
                         existingRecord.AttendRemark.Contains("Updated to", StringComparison.OrdinalIgnoreCase));
                    
                    if (isFacultyOverride)
                    {
                        _logger.LogInformation($"✅ FACULTY OVERRIDE DETECTED - Preserving {existingRecord.AttendStatus} status for Student {enrollment.StudId}");
                        validCount++;
                        continue;
                    }
                    
                    // ✅ Never override 'Excused' status
                    if (existingRecord.AttendStatus == AttendanceStatus.Excused)
                    {
                        _logger.LogInformation($"✅ EXCUSED STATUS PRESERVED - Skipping for Student {enrollment.StudId}");
                        validCount++;
                        continue;
                    }
                    
                    // ✅ Only update if currently marked as Present or Late
                    if (existingRecord.AttendStatus == AttendanceStatus.Present || 
                        existingRecord.AttendStatus == AttendanceStatus.Late)
                    {
                        _logger.LogInformation($"Updating Student {enrollment.StudId} status from {existingRecord.AttendStatus} to Absent (forgot timeout)");
                        
                        existingRecord.AttendStatus = AttendanceStatus.Absent;
                        existingRecord.AttendRemark = $"Absent - Forgot to time out before class ended at {classEndTimeFormatted}. Timed in at {studentTimeInFormatted}.";
                        existingRecord.IsPresent = false;
                        
                        await _attendanceRepo.UpdateAsync(existingRecord);
                        forgotCount++;
                        
                        _logger.LogInformation($"✅ Updated existing attendance record {existingRecord.AttendId} to Absent");
                    }
                    else
                    {
                        _logger.LogDebug($"Student {enrollment.StudId} already marked as {existingRecord.AttendStatus}");
                        validCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing Student {enrollment.StudId} for Class {classId}");
                }
            }
            
            return (forgotCount, validCount);
        }

        /// <summary>
        /// Get list of unique student ID's who have classes scheduled for a specific day
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
        /// ✅ CRITICAL FIX: Check for faculty override before marking absent
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
                    // ✅ CRITICAL FIX: Check if faculty manually modified this record
                    bool isFacultyOverride = !string.IsNullOrEmpty(existingRecord.AttendRemark) &&
                        (existingRecord.AttendRemark.Contains("by faculty", StringComparison.OrdinalIgnoreCase) ||
                         existingRecord.AttendRemark.Contains("Corrected by faculty", StringComparison.OrdinalIgnoreCase) ||
                         existingRecord.AttendRemark.Contains("Manually set", StringComparison.OrdinalIgnoreCase) ||
                         existingRecord.AttendRemark.Contains("Updated to", StringComparison.OrdinalIgnoreCase));
                    
                    if (isFacultyOverride)
                    {
                        _logger.LogInformation($"✅ FACULTY OVERRIDE DETECTED - Preserving {existingRecord.AttendStatus} status for Student {studId}, Class {classSchedule.ClassId}. Remark: {existingRecord.AttendRemark}");
                        return; // Don't override faculty decision
                    }
                    
                    // ✅ CRITICAL FIX: Never override 'Excused' status (always faculty-set)
                    if (existingRecord.AttendStatus == AttendanceStatus.Excused)
                    {
                        _logger.LogInformation($"✅ EXCUSED STATUS PRESERVED - Skipping auto-absent for Student {studId}, Class {classSchedule.ClassId}");
                        return;
                    }
                    
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

                _logger.LogInformation($"✅ Marked absent: {classSchedule.CourseCode} for Student {studId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking class {classSchedule.ClassId} absent for Student {studId}");
                throw;
            }
        }

        /// <summary>
        /// ✅ NEW: Manual trigger for forgot timeout detection (can be called by faculty)
        /// Processes ONLY classes that have already ended
        /// </summary>
        public async Task<(int forgotCount, int validCount, string message)> ProcessForgotTimeoutForClassAsync(int classId, DateTime date)
        {
            try
            {
                _logger.LogInformation($"=== Manual Forgot Timeout Check for Class {classId} on {date:yyyy-MM-dd} ===");
                
                var dayOfWeek = date.DayOfWeek.ToString();
                
                var schedules = await _classScheduleRepo.GetByClassIdAsync(classId);
                var todaySchedule = schedules.FirstOrDefault(s => 
                    s.SchedDayOfWeek.Trim().Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase));
                
                if (todaySchedule == null)
                {
                    _logger.LogWarning($"Class {classId} has no schedule for {dayOfWeek}");
                    return (0, 0, $"No class scheduled for {dayOfWeek}");
                }
                
                var classEndTime = todaySchedule.SchedEndTime;
                var currentTime = _pht.TimeOfDay; // Use PHT for schedule comparison
                
                // Check if class has ended (using PHT)
                bool classHasEnded = date.Date < _pht.Today || 
                                   (date.Date == _pht.Today && currentTime > classEndTime);
                
                if (!classHasEnded)
                {
                    var classEndTimeFormatted = _pht.Today.Add(classEndTime).ToString("hh:mm tt");
                    var currentTimeFormatted = _pht.Today.Add(currentTime).ToString("hh:mm tt");
                    _logger.LogWarning($"Class {classId} hasn't ended yet. Current: {currentTimeFormatted}, Ends: {classEndTimeFormatted}");
                    return (0, 0, $"Class hasn't ended yet. Current time: {currentTimeFormatted}, Class ends: {classEndTimeFormatted}");
                }
                
                _logger.LogInformation($"Processing Class {classId} - Ended at {_pht.Today.Add(classEndTime):hh:mm tt}");
                
                var (forgotCount, validCount) = await ProcessClassForgotTimeoutAsync(classId, date, classEndTime);
                
                string message = forgotCount > 0 
                    ? $"Marked {forgotCount} student(s) as absent (forgot to time out)"
                    : "No students forgot to time out";
                
                _logger.LogInformation($"✅ Manual forgot timeout check complete: {message}");
                
                return (forgotCount, validCount, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing manual forgot timeout for Class {classId}");
                return (0, 0, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Manual trigger for processing absences (for testing or manual runs)
        /// ✅ ENHANCED: Now also processes forgot timeout absences
        /// </summary>
        public async Task ProcessAbsencesForTodayAsync()
        {
            // Process students who never timed in (using PHT date)
            await ProcessAbsencesForDateAsync(_pht.Today);
            
            // ✅ NEW: Process students who forgot to time out
            await ProcessForgotTimeoutAbsencesAsync(_pht.Today);
        }
    }
}
