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
        private readonly IUserRepository _userRepo; // ✅ NEW: For loading user data
        private readonly IProgramRepository _programRepo; // ✅ NEW: For loading program data
        private readonly ISectionRepository _sectionRepo; // ✅ NEW: For loading section data
        private readonly IEnrollmentRepository _enrollmentRepo; // ✅ NEW: For class info
        private readonly IClassScheduleRepository _scheduleRepo; // ✅ NEW: For schedule validation
        private readonly IClassRepository _classRepo; // ✅ NEW: For class details
        private readonly ICourseRepository _courseRepo; // ✅ NEW: For course details
        private readonly IFacultyRepository _facultyRepo; // ✅ NEW: For faculty details
        private readonly IRoomRepository _roomRepo; // ✅ NEW: For room details
        private readonly LateDetectionService _lateDetectionService;
        private readonly MissedTimeOutService _missedTimeOutService;
        private readonly ILogger<DailyAttendanceService> _logger;
        private readonly PhilippineTimeService _pht;

        public DailyAttendanceService(
            IDailyAttendanceLogRepository dailyAttendanceRepo,
            IStudentRepository studentRepo,
            IUserRepository userRepo, // ✅ NEW
            IProgramRepository programRepo, // ✅ NEW
            ISectionRepository sectionRepo, // ✅ NEW
            IEnrollmentRepository enrollmentRepo, // ✅ NEW
            IClassScheduleRepository scheduleRepo, // ✅ NEW
            IClassRepository classRepo, // ✅ NEW
            ICourseRepository courseRepo, // ✅ NEW
            IFacultyRepository facultyRepo, // ✅ NEW
            IRoomRepository roomRepo, // ✅ NEW
            LateDetectionService lateDetectionService,
            MissedTimeOutService missedTimeOutService,
            ILogger<DailyAttendanceService> logger,
            PhilippineTimeService pht)
        {
            _dailyAttendanceRepo = dailyAttendanceRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo; // ✅ NEW
            _programRepo = programRepo; // ✅ NEW
            _sectionRepo = sectionRepo; // ✅ NEW
            _enrollmentRepo = enrollmentRepo; // ✅ NEW
            _scheduleRepo = scheduleRepo; // ✅ NEW
            _classRepo = classRepo; // ✅ NEW
            _courseRepo = courseRepo; // ✅ NEW
            _facultyRepo = facultyRepo; // ✅ NEW
            _roomRepo = roomRepo; // ✅ NEW
            _lateDetectionService = lateDetectionService;
            _missedTimeOutService = missedTimeOutService;
            _logger = logger;
            _pht = pht;
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
                
                var today = _pht.Today; // Use PHT date
                var now = _pht.Now; // Use PHT time

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
        /// MAIN LOGIC: Process attendance scan and determine action (Time In/Time Out/Already Completed)
        /// This is the core three-case toggle implementation
        /// ✅ NEW OVERLOAD: Accepts pre-validated classId from HomeController
        /// </summary>
        public async Task<AttendanceScanResponseDto> ProcessAttendanceScanAsync(Student student, int classId)
        {
            try
            {
                _logger.LogInformation($"=== Processing Attendance Scan for Student: {student.StudNumber}, Class: {classId} ===");
                
                var today = _pht.Today; // Use PHT date
                var now = _pht.Now; // Use PHT time

                // Check for incomplete previous day records
                var incompleteResult = await _missedTimeOutService.CheckAndFlagIncompleteRecordsAsync(
                    student.StudId, 
                    today);

                // Query today's attendance record FOR THIS SPECIFIC CLASS
                var todayRecord = await _dailyAttendanceRepo.GetByStudentClassAndDateAsync(student.StudId, classId, today);

                // Determine which case applies
                AttendanceScanResponseDto response;
                
                if (todayRecord == null)
                {
                    // CASE 1: No record exists → TIME IN
                    response = await ProcessTimeInAsync(student, classId, today, now);
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

                // Add warning message if incomplete records were found
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
        /// ✅ ENHANCED: Now loads complete student data and current class information
        /// </summary>
        private async Task<AttendanceScanResponseDto> ProcessTimeInAsync(
            Student student, 
            DateTime date, 
            DateTime timestamp)
        {
            _logger.LogInformation($"CASE 1: No record exists. Processing TIME IN for Student {student.StudNumber}");

            try
            {
                // ✅ NEW: Load complete student information
                var user = await _userRepo.GetByIdAsync(student.UserId);
                var program = student.ProgramId.HasValue 
                    ? await _programRepo.GetByIdAsync(student.ProgramId.Value) 
                    : null;
                var section = student.SectionId.HasValue 
                    ? await _sectionRepo.GetByIdAsync(student.SectionId.Value) 
                    : null;
                
                // Populate navigation properties
                student.User = user;
                student.Program = program;
                student.Section = section;
                
                _logger.LogInformation($"Loaded student data: {user?.UserFirstName} {user?.UserLastName}, Program: {program?.ProgramName}, Section: {section?.SectionName}");

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

                // ✅ NEW: Get current active class information
                var activeClass = await GetCurrentActiveClassAsync(student.StudId, timestamp);
                if (activeClass != null)
                {
                    _logger.LogInformation($"Active class: {activeClass.CourseCode} - {activeClass.CourseName}");
                }

                // ✅ Trigger late detection logic (Phase 4)
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

                // ✅ ENHANCED: Build response with complete data
                var response = new AttendanceScanResponseDto
                {
                    Success = true,
                    Message = $"Good morning, {user?.UserFirstName}! Time In recorded successfully.",
                    Action = "time_in",
                    Timestamp = timestamp,
                    Student = new StudentInfoDto
                    {
                        StudId = student.StudId,
                        StudNumber = student.StudNumber,
                        FullName = $"{user?.UserFirstName} {user?.UserLastName}".Trim(),
                        ProgramName = program?.ProgramName,
                        SectionName = section?.SectionName,
                        YearLevel = student.StudYearLevel,
                        EnrolledClasses = activeClass != null 
                            ? new List<EnrolledClassInfo> { activeClass } 
                            : new List<EnrolledClassInfo>()
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

        /// <summary>
        /// ✅ NEW: CASE 1 helper with classId parameter
        /// Creates attendance record linked to specific class
        /// </summary>
        private async Task<AttendanceScanResponseDto> ProcessTimeInAsync(
            Student student,
            int classId, 
            DateTime date, 
            DateTime timestamp)
        {
            _logger.LogInformation($"CASE 1: No record exists. Processing TIME IN for Student {student.StudNumber}, Class {classId}");

            try
            {
                // Load complete student information
                var user = await _userRepo.GetByIdAsync(student.UserId);
                var program = student.ProgramId.HasValue 
                    ? await _programRepo.GetByIdAsync(student.ProgramId.Value) 
                    : null;
                var section = student.SectionId.HasValue 
                    ? await _sectionRepo.GetByIdAsync(student.SectionId.Value) 
                    : null;
                
                // Populate navigation properties
                student.User = user;
                student.Program = program;
                student.Section = section;
                
                _logger.LogInformation($"Loaded student data: {user?.UserFirstName} {user?.UserLastName}, Program: {program?.ProgramName}, Section: {section?.SectionName}");

                // Create new daily attendance record WITH CLASS ID
                var newRecord = new DailyAttendanceLog
                {
                    StudId = student.StudId,
                    ClassId = classId, // ✅ LINK TO CLASS
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

                _logger.LogInformation($"✅ TIME IN recorded successfully at {timestamp:HH:mm:ss} for Student {student.StudNumber}, Class {classId}");

                // ✅ NEW: Get current active class information
                var activeClass = await GetCurrentActiveClassAsync(student.StudId, timestamp);
                if (activeClass != null)
                {
                    _logger.LogInformation($"Active class: {activeClass.CourseCode} - {activeClass.CourseName}");
                }

                // ✅ Trigger late detection logic (Phase 4)
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

                // ✅ ENHANCED: Build response with complete data
                var response = new AttendanceScanResponseDto
                {
                    Success = true,
                    Message = $"Good morning, {user?.UserFirstName}! Time In recorded successfully.",
                    Action = "time_in",
                    Timestamp = timestamp,
                    Student = new StudentInfoDto
                    {
                        StudId = student.StudId,
                        StudNumber = student.StudNumber,
                        FullName = $"{user?.UserFirstName} {user?.UserLastName}".Trim(),
                        ProgramName = program?.ProgramName,
                        SectionName = section?.SectionName,
                        YearLevel = student.StudYearLevel,
                        EnrolledClasses = activeClass != null 
                            ? new List<EnrolledClassInfo> { activeClass } 
                            : new List<EnrolledClassInfo>()
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

        /// <summary>
        /// CASE 2: Time In exists but Time Out is null
        /// Action: Update existing record with Time Out timestamp
        /// ✅ ENHANCED: Now loads complete student data and current class information
        /// </summary>
        private async Task<AttendanceScanResponseDto> ProcessTimeOutAsync(
            Student student, 
            DailyAttendanceLog existingRecord, 
            DateTime timestamp)
        {
            _logger.LogInformation($"CASE 2: Time In exists ({existingRecord.TimeIn:HH:mm:ss}), no Time Out. Processing TIME OUT for Student {student.StudNumber}");

            try
            {
                // ✅ NEW: Load complete student information
                var user = await _userRepo.GetByIdAsync(student.UserId);
                var program = student.ProgramId.HasValue 
                    ? await _programRepo.GetByIdAsync(student.ProgramId.Value) 
                    : null;
                var section = student.SectionId.HasValue 
                    ? await _sectionRepo.GetByIdAsync(student.SectionId.Value) 
                    : null;
                
                // Populate navigation properties
                student.User = user;
                student.Program = program;
                student.Section = section;
                
                _logger.LogInformation($"Loaded student data: {user?.UserFirstName} {user?.UserLastName}, Program: {program?.ProgramName}");

                // Calculate duration
                var duration = timestamp - existingRecord.TimeIn!.Value;
                var hours = (int)duration.TotalHours;
                var minutes = duration.Minutes;
                var seconds = duration.Seconds;

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

                _logger.LogInformation($"✅ TIME OUT recorded successfully at {timestamp:HH:mm:ss} for Student {student.StudNumber}. Duration: {hours}h {minutes}m {seconds}s");

                // ✅ NEW: Get current active class information (if any)
                var activeClass = await GetCurrentActiveClassAsync(student.StudId, timestamp);

                // ✅ ENHANCED: Build response with complete data
                string durationText = hours > 0 
                    ? $"{hours} hours and {minutes} minutes" 
                    : $"{minutes} minutes and {seconds} seconds";

                return new AttendanceScanResponseDto
                {
                    Success = true,
                    Message = $"Goodbye, {user?.UserFirstName}! Time Out recorded. You were on campus for {durationText} today.",
                    Action = "time_out",
                    Timestamp = timestamp,
                    Student = new StudentInfoDto
                    {
                        StudId = student.StudId,
                        StudNumber = student.StudNumber,
                        FullName = $"{user?.UserFirstName} {user?.UserLastName}".Trim(),
                        ProgramName = program?.ProgramName,
                        SectionName = section?.SectionName,
                        YearLevel = student.StudYearLevel,
                        EnrolledClasses = activeClass != null 
                            ? new List<EnrolledClassInfo> { activeClass } 
                            : new List<EnrolledClassInfo>()
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
                var today = _pht.Today; // Use PHT date
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

        /// <summary>
        /// ✅ NEW: Get current active class for student at specific timestamp
        /// Returns class information including course, faculty, room, and schedule details
        /// CRITICAL FIX: Proper time boundary handling for edge cases
        /// </summary>
        private async Task<EnrolledClassInfo?> GetCurrentActiveClassAsync(int studId, DateTime timestamp)
        {
            try
            {
                var currentDay = timestamp.DayOfWeek.ToString(); // "Monday", "Saturday", etc.
                var currentTime = timestamp.TimeOfDay;
                
                _logger.LogInformation($"Getting active class for student {studId} on {currentDay} at {currentTime:hh\\:mm}");
                
                // Get all enrolled classes
                var enrollments = await _enrollmentRepo.GetByStudentIdAsync(studId);
                
                foreach (var enrollment in enrollments)
                {
                    var schedules = await _scheduleRepo.GetByClassIdAsync(enrollment.ClassId);
                    
                    foreach (var schedule in schedules)
                    {
                        // ✅ CRITICAL FIX: Case-insensitive, trimmed day matching
                        bool dayMatch = schedule.SchedDayOfWeek?.Trim().Equals(currentDay.Trim(), StringComparison.OrdinalIgnoreCase) ?? false;
                        
                        // ✅ CRITICAL FIX: Proper time boundary checking
                        // A class is active if: current time >= start time AND current time <= end time
                        bool timeMatch = currentTime >= schedule.SchedStartTime && currentTime <= schedule.SchedEndTime;
                        
                        if (dayMatch && timeMatch)
                        {
                            _logger.LogInformation($"Found active class: ClassId={enrollment.ClassId}");
                            
                            // Get class details
                            var classDetail = await _classRepo.GetByIdAsync(enrollment.ClassId);
                            if (classDetail == null) continue;
                            
                            // Get course details
                            var course = classDetail.CourseId.HasValue 
                                ? await _courseRepo.GetByIdAsync(classDetail.CourseId.Value) 
                                : null;
                            
                            // Get faculty details
                            string facultyName = "TBA";
                            if (classDetail.FacId.HasValue)
                            {
                                var faculty = await _facultyRepo.GetByIdAsync(classDetail.FacId.Value);
                                if (faculty != null)
                                {
                                    var facultyUser = await _userRepo.GetByIdAsync(faculty.UserId);
                                    if (facultyUser != null)
                                    {
                                        facultyName = $"{facultyUser.UserFirstName} {facultyUser.UserLastName}";
                                    }
                                }
                            }
                            
                            // Get room details
                            var room = classDetail.RoomId.HasValue 
                                ? await _roomRepo.GetByIdAsync(classDetail.RoomId.Value) 
                                : null;
                            
                            return new EnrolledClassInfo
                            {
                                ClassId = classDetail.ClassId,
                                CourseCode = course?.CourseCode ?? "N/A",
                                CourseName = course?.CourseName ?? "N/A",
                                FacultyName = facultyName,
                                RoomCode = room?.RoomCode ?? "TBA",
                                Schedule = $"{schedule.SchedDayOfWeek} {schedule.SchedStartTime:hh\\:mm tt}-{schedule.SchedEndTime:hh\\:mm tt}"
                            };
                        }
                    }
                }
                
                _logger.LogWarning($"No active class found for student {studId} on {currentDay} at {currentTime:hh\\:mm}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active class for student {studId}");
                return null;
            }
        }

        /// <summary>
        /// ✅ NEW: Get class information by class ID
        /// Helper method to retrieve complete class details
        /// </summary>
        private async Task<EnrolledClassInfo?> GetClassInfoByIdAsync(int classId)
        {
            try
            {
                var classDetail = await _classRepo.GetByIdAsync(classId);
                if (classDetail == null) return null;
                
                var course = classDetail.CourseId.HasValue 
                    ? await _courseRepo.GetByIdAsync(classDetail.CourseId.Value) 
                    : null;
                
                string facultyName = "TBA";
                if (classDetail.FacId.HasValue)
                {
                    var faculty = await _facultyRepo.GetByIdAsync(classDetail.FacId.Value);
                    if (faculty != null)
                    {
                        var facultyUser = await _userRepo.GetByIdAsync(faculty.UserId);
                        if (facultyUser != null)
                        {
                            facultyName = $"{facultyUser.UserFirstName} {facultyUser.UserLastName}";
                        }
                    }
                }
                
                var room = classDetail.RoomId.HasValue 
                    ? await _roomRepo.GetByIdAsync(classDetail.RoomId.Value) 
                    : null;
                
                // Get schedule for this class
                var schedules = await _scheduleRepo.GetByClassIdAsync(classId);
                var schedule = schedules.FirstOrDefault();
                
                var scheduleText = schedule != null
                    ? $"{schedule.SchedDayOfWeek} {schedule.SchedStartTime:hh\\:mm tt}-{schedule.SchedEndTime:hh\\:mm tt}"
                    : "TBA";
                
                return new EnrolledClassInfo
                {
                    ClassId = classDetail.ClassId,
                    CourseCode = course?.CourseCode ?? "N/A",
                    CourseName = course?.CourseName ?? "N/A",
                    FacultyName = facultyName,
                    RoomCode = room?.RoomCode ?? "TBA",
                    Schedule = scheduleText
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting class info for ClassId: {classId}");
                return null;
            }
        }
    }
}
