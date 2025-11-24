using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Data; // ✅ ADDED: For DatabaseHelper
using Microsoft.AspNetCore.Mvc;
using System.Data; // ✅ ADDED: For DataRow

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    [RoleAuthorize(USER_ROLES.FACULTY)]
    public class FacultyAttendanceController : Controller
    {
        private readonly ILogger<FacultyAttendanceController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICourseRepository _courseRepository; // ✅ ADDED: For course details
        private readonly UniversalAuditService _auditService; // ✅ ADDED: Universal audit service
        private readonly DatabaseHelper _db; // ✅ ADDED: For schedule validation
        private readonly AbsenceDetectionService _absenceDetectionService; // ✅ NEW: For forgot timeout detection
        private readonly IDailyAttendanceLogRepository _dailyAttendanceRepo; // ✅ NEW: For daily attendance log operations

        public FacultyAttendanceController(
            ILogger<FacultyAttendanceController> logger,
            IFacultyRepository facultyRepository,
            IAttendanceRepository attendanceRepository,
            IEnrollmentRepository enrollmentRepository,
            IClassRepository classRepository,
            ICourseRepository courseRepository, // ✅ ADDED: Inject course repository
            UniversalAuditService auditService, // ✅ ADDED: Inject universal audit service
            DatabaseHelper db, // ✅ ADDED: Inject DatabaseHelper
            AbsenceDetectionService absenceDetectionService, // ✅ NEW: Inject absence detection service
            IDailyAttendanceLogRepository dailyAttendanceRepo) // ✅ NEW: Inject daily attendance log repository
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _attendanceRepository = attendanceRepository;
            _enrollmentRepository = enrollmentRepository;
            _classRepository = classRepository;
            _courseRepository = courseRepository; // ✅ ADDED: Store course repository
            _auditService = auditService; // ✅ ADDED: Store audit service
            _db = db; // ✅ ADDED: Store DatabaseHelper
            _absenceDetectionService = absenceDetectionService; // ✅ NEW: Store absence detection service
            _dailyAttendanceRepo = dailyAttendanceRepo; // ✅ NEW: Store daily attendance log repository
        }

        // GET: FacultyAttendance/Index
        public IActionResult Index()
        {
            return View("~/Views/Faculty/FacultyAttendance.cshtml");
        }

        // GET: FacultyAttendance/GetClassAttendance
        [HttpGet]
        public async Task<IActionResult> GetClassAttendance(int classId, string? date = null)
        {
            try
            {
                _logger.LogInformation($"✅ GetClassAttendance called - ClassId: {classId}, Date: {date}");
                
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("❌ User not authenticated - no UserId in session");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                _logger.LogInformation($"✅ User authenticated - UserId: {userId.Value}");

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    _logger.LogWarning($"❌ Faculty record not found for userId: {userId.Value}");
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                _logger.LogInformation($"✅ Faculty found - FacId: {faculty.FacId}");

                // ✅ TIMEZONE FIX: Use Philippine time (UTC+8) instead of UTC
                // Get current date in Philippine timezone
                var philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); // Philippines uses UTC+8, same as Singapore
                var philippineNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, philippineTimeZone);
                var philippineToday = philippineNow.Date;

                // Parse target date - default to Philippine local time, not UTC
                DateTime targetDate;
                if (string.IsNullOrEmpty(date))
                {
                    targetDate = philippineToday; // ✅ Use Philippine today
                }
                else if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    targetDate = parsedDate.Date;
                }
                else
                {
                    targetDate = philippineToday; // ✅ Fallback to Philippine today
                }

                _logger.LogInformation($"✅ Target date: {targetDate:yyyy-MM-dd} (Philippine Time: {philippineNow:yyyy-MM-dd HH:mm:ss})");

                // ✅ CRITICAL FIX: Validate if class is scheduled for this day of the week
                var classEntity = await _classRepository.GetByIdAsync(classId);
                if (classEntity == null || classEntity.FacId != faculty.FacId)
                {
                    _logger.LogWarning($"❌ Class not found or access denied for classId: {classId}");
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                // Get class schedule to check if class meets on this day
                var classSchedules = await _db.ExecuteQueryAsync(
                    "SELECT sched_day_of_week, sched_start_time, sched_end_time FROM class_schedules WHERE class_id = @classId",
                    new Dictionary<string, object> { { "@classId", classId } }
                );

                bool hasScheduleForThisDay = false;
                foreach (DataRow scheduleRow in classSchedules.Rows)
                {
                    var scheduledDay = scheduleRow["sched_day_of_week"]?.ToString()?.Trim();
                    var targetDay = targetDate.ToString("dddd").Trim();
                    
                    if (string.Equals(scheduledDay, targetDay, StringComparison.OrdinalIgnoreCase))
                    {
                        hasScheduleForThisDay = true;
                        break;
                    }
                }

                if (!hasScheduleForThisDay)
                {
                    _logger.LogWarning($"❌ No class scheduled for {targetDate:dddd, yyyy-MM-dd}");
                    return Json(new 
                    { 
                        success = false, 
                        message = $"No class scheduled for {targetDate:dddd, MMMM dd, yyyy}. This class only meets on scheduled days." 
                    });
                }

                // ❌ REMOVED: DO NOT run absence detection here - it overwrites faculty manual edits!
                // The service should ONLY run:
                // 1. Automatically at 11:59 PM via AttendanceBackgroundService
                // 2. Manually via explicit faculty action
                // 
                // Removing this prevents race condition where service overwrites faculty edits
                // when the page is loaded/refreshed

                // Single optimized query with all JOINs and faculty verification
                var attendanceData = await _attendanceRepository.GetClassAttendanceWithDetailsAsync(
                    classId, targetDate, faculty.FacId);

                if (attendanceData == null)
                {
                    _logger.LogWarning($"❌ No attendance data found for classId: {classId}, facId: {faculty.FacId}");
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                _logger.LogInformation($"✅ Attendance data retrieved - CourseCode: {attendanceData.CourseCode}, Students: {attendanceData.Students?.Count ?? 0}");

                var response = new
                {
                    success = true,
                    data = new
                    {
                        courseCode = attendanceData.CourseCode,
                        courseName = attendanceData.CourseName,
                        date = attendanceData.Date,
                        students = attendanceData.Students.Select(s => new
                        {
                            studId = s.StudId,
                            studentNumber = s.StudentNumber,
                            name = s.Name,
                            profileImage = s.ProfileImage,
                            timeIn = s.TimeIn,
                            timeOut = s.TimeOut,
                            status = s.Status,
                            attendId = s.AttendId
                        }).ToList()
                    }
                };

                _logger.LogInformation($"✅ Returning response with {response.data.students.Count} students");

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching class attendance: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        // PUT: FacultyAttendance/UpdateAttendance
        [HttpPut]
        public async Task<IActionResult> UpdateAttendance([FromBody] UpdateAttendanceRequest request)
        {
            try
            {
                _logger.LogInformation($"🔍 UpdateAttendance called - ClassId: {request.ClassId}, StudId: {request.StudId}, Status: {request.Status}, Date: {request.Date}");
                
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("❌ User not authenticated");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    _logger.LogWarning($"❌ Faculty record not found for userId: {userId.Value}");
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classEntity = await _classRepository.GetByIdAsync(request.ClassId);
                if (classEntity == null || classEntity.FacId != faculty.FacId)
                {
                    _logger.LogWarning($"❌ Class not found or access denied - ClassId: {request.ClassId}, FacId: {faculty.FacId}");
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                // ✅ CRITICAL FIX: Parse the target date properly and use UTC if parsing fails
                var philippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                var philippineToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, philippineTimeZone).Date;
                
                DateTime targetDate;
                if (string.IsNullOrEmpty(request.Date))
                {
                    targetDate = philippineToday;
                }
                else if (DateTime.TryParse(request.Date, out DateTime parsedDate))
                {
                    targetDate = parsedDate.Date;
                }
                else
                {
                    _logger.LogWarning($"❌ Invalid date format: {request.Date}");
                    return Json(new { success = false, message = "Invalid date format" });
                }

                _logger.LogInformation($"✅ Target date: {targetDate:yyyy-MM-dd}");

                // ✅ CRITICAL FIX: Add faculty override marker to remarks
                // This prevents AbsenceDetectionService from overwriting manual edits
                var facultyOverrideRemark = string.IsNullOrEmpty(request.Remarks)
                    ? $"Updated to {request.Status} by faculty"
                    : $"{request.Remarks} (Updated to {request.Status} by faculty)";

                // ✅ CRITICAL FIX: Use database query to get records directly instead of loading all into memory
                var attendanceQuery = @"
                    SELECT attend_id, class_id, stud_id, attend_date, attend_scan_time, attend_type, 
                           attend_status, attend_remark, attend_created_at
                    FROM attendance
                    WHERE class_id = @classId 
                      AND stud_id = @studId 
                      AND attend_date = @date
                    ORDER BY attend_scan_time;";
                
                var parameters = new Dictionary<string, object>
                {
                    { "@classId", request.ClassId },
                    { "@studId", request.StudId },
                    { "@date", targetDate }
                };

                var dt = await _db.ExecuteQueryAsync(attendanceQuery, parameters);
                
                _logger.LogInformation($"🔍 Found {dt.Rows.Count} attendance record(s) for Student {request.StudId} on {targetDate:yyyy-MM-dd}");

                bool result = false;
                int updatedCount = 0;
                var now = DateTime.Now;

                if (dt.Rows.Count == 0)
                {
                    // ✅ Create new attendance record if none exist
                    _logger.LogInformation($"📝 Creating NEW attendance record - Status: {request.Status}");
                    
                    var newAttendance = new Attendance
                    {
                        ClassId = request.ClassId,
                        StudId = request.StudId,
                        AttendDate = targetDate,
                        AttendScanTime = now,
                        AttendType = ScanType.In,
                        AttendStatus = Enum.Parse<AttendanceStatus>(request.Status, ignoreCase: true),
                        AttendRemark = facultyOverrideRemark, // ✅ Use faculty override remark
                        AttendCreatedAt = now
                    };

                    result = await _attendanceRepository.AddAsync(newAttendance);
                    if (result)
                    {
                        updatedCount = 1;
                        _logger.LogInformation($"✅ NEW attendance record created successfully");
                    }
                    else
                    {
                        _logger.LogError($"❌ Failed to create new attendance record");
                    }
                }
                else
                {
                    // ✅ CRITICAL FIX: Update ALL existing attendance records with direct SQL UPDATE
                    // This ensures the database is actually updated
                    var updateQuery = @"
                        UPDATE attendance 
                        SET attend_status = @status::attendance_status,
                            attend_remark = @remark
                        WHERE class_id = @classId 
                          AND stud_id = @studId 
                          AND attend_date = @date;";
                    
                    var updateParams = new Dictionary<string, object>
                    {
                        { "@classId", request.ClassId },
                        { "@studId", request.StudId },
                        { "@date", targetDate },
                        { "@status", request.Status.ToLower() },
                        { "@remark", facultyOverrideRemark } // ✅ Use faculty override remark
                    };

                    updatedCount = await _db.ExecuteNonQueryAsync(updateQuery, updateParams);
                    result = updatedCount > 0;
                    
                    _logger.LogInformation($"✅ Updated {updatedCount} attendance record(s) to status: {request.Status} with faculty override protection");
                }

                if (result)
                {
                    // ✅ Log audit for individual attendance update
                    if (classEntity?.CourseId != null)
                    {
                        var course = await _courseRepository.GetByIdAsync(classEntity.CourseId.Value);
                        if (course != null)
                        {
                            await _auditService.LogActionAsync(
                                "Attendance Status Changed",
                                $"Faculty changed attendance for student {request.StudId} in {course.CourseCode} " +
                                $"on {targetDate:yyyy-MM-dd} to {request.Status} (Override Protected)"
                            );
                            _logger.LogInformation($"✅ Audit log created for status change");
                        }
                    }

                    var message = updatedCount > 1 
                        ? $"Attendance updated successfully ({updatedCount} records)" 
                        : "Attendance updated successfully";
                    
                    _logger.LogInformation($"✅✅✅ SUCCESS - {message}");
                    return Json(new { success = true, message = message });
                }

                _logger.LogError($"❌ Failed to update attendance - no records affected");
                return Json(new { success = false, message = "Failed to update attendance" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating attendance: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // DELETE: FacultyAttendance/DeleteAttendance
        [HttpDelete]
        public async Task<IActionResult> DeleteAttendance([FromBody] DeleteAttendanceRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classEntity = await _classRepository.GetByIdAsync(request.ClassId);
                if (classEntity == null || classEntity.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                // Get attendance for the specified date
                var targetDate = string.IsNullOrEmpty(request.Date) ? DateTime.Today : DateTime.Parse(request.Date);
                
                // ✅ STEP 1: Delete from attendance table
                var allAttendance = await _attendanceRepository.GetAllAsync();
                var attendanceRecords = allAttendance
                    .Where(a => a.ClassId == request.ClassId && a.StudId == request.StudId && a.AttendDate.Date == targetDate)
                    .ToList();

                if (!attendanceRecords.Any())
                {
                    return Json(new { success = false, message = "Attendance record not found" });
                }

                // Delete all attendance records for this student on this date
                int deletedCount = 0;
                foreach (var record in attendanceRecords)
                {
                    var result = await _attendanceRepository.DeleteAsync(record.AttendId);
                    if (result)
                    {
                        deletedCount++;
                    }
                }

                // ✅ STEP 2: Also delete from daily_attendance_log table
                // This ensures students can re-scan during active class time
                var dailyAttendLog = await _dailyAttendanceRepo.GetByStudentClassAndDateAsync(
                    request.StudId, 
                    request.ClassId, 
                    targetDate
                );
                
                if (dailyAttendLog != null)
                {
                    try
                    {
                        await _dailyAttendanceRepo.DeleteAsync(dailyAttendLog.DailyAttendId);
                        _logger.LogInformation($"✅ Deleted daily attendance log for Student {request.StudId}, Class {request.ClassId}, Date {targetDate:yyyy-MM-dd}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to delete from daily_attendance_log, but continuing with operation");
                        // Don't fail the entire operation if daily log deletion fails
                    }
                }

                if (deletedCount > 0)
                {
                    // ✅ ADDED: Log audit for attendance deletion
                    if (classEntity?.CourseId != null)
                    {
                        var course = await _courseRepository.GetByIdAsync(classEntity.CourseId.Value);
                        if (course != null)
                        {
                            await _auditService.LogActionAsync(
                                "Attendance Record Deleted",
                                $"Faculty deleted attendance for student {request.StudId} in {course.CourseCode} " +
                                $"on {targetDate:yyyy-MM-dd} ({deletedCount} attendance record(s) + daily log)"
                            );
                        }
                    }

                    return Json(new { success = true, message = "Attendance deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete attendance" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance");
                return Json(new { success = false, message = "Failed to delete attendance" });
            }
        }

        // POST: FacultyAttendance/MarkAllPresent
        [HttpPost]
        public async Task<IActionResult> MarkAllPresent([FromBody] MarkAllPresentRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classEntity = await _classRepository.GetByIdAsync(request.ClassId);
                if (classEntity == null || classEntity.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                // Get enrolled students
                var enrollments = await _enrollmentRepository.GetByClassIdAsync(request.ClassId);
                var today = DateTime.Today;
                var now = DateTime.Now;

                // Get existing attendance for today
                var allAttendance = await _attendanceRepository.GetAllAsync();
                var todayAttendance = allAttendance
                    .Where(a => a.ClassId == request.ClassId && a.StudId.HasValue && a.AttendDate.Date == today)
                    .GroupBy(a => a.StudId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                int createdCount = 0;
                int updatedCount = 0;

                // Mark all students as present
                foreach (var enrollment in enrollments)
                {
                    if (todayAttendance.ContainsKey(enrollment.StudId))
                    {
                        // Update existing attendance records (both In and Out) to Present
                        var studentRecords = todayAttendance[enrollment.StudId];
                        foreach (var record in studentRecords)
                        {
                            record.AttendStatus = AttendanceStatus.Present;
                            record.AttendRemark = "Marked present by faculty";
                            
                            var updateResult = await _attendanceRepository.UpdateAsync(record);
                            if (updateResult)
                            {
                                updatedCount++;
                            }
                        }
                    }
                    else
                    {
                        // Create new attendance record if none exists
                        var attendance = new Attendance
                        {
                            ClassId = request.ClassId,
                            StudId = enrollment.StudId,
                            AttendDate = today,
                            AttendScanTime = now,
                            AttendType = ScanType.In,
                            AttendStatus = AttendanceStatus.Present,
                            AttendRemark = "Marked present by faculty",
                            AttendCreatedAt = now
                        };

                        var result = await _attendanceRepository.AddAsync(attendance);
                        if (result)
                        {
                            createdCount++;
                        }
                    }
                }

                var totalAffected = createdCount + updatedCount;
                var message = totalAffected > 0
                    ? $"Marked all students as present ({createdCount} created, {updatedCount} updated)"
                    : "No students to mark";

                // ✅ ADDED: Log audit for faculty marking attendance
                if (totalAffected > 0 && classEntity?.CourseId != null)
                {
                    var course = await _courseRepository.GetByIdAsync(classEntity.CourseId.Value);
                    if (course != null)
                    {
                        var date = DateTime.Today;
                        var studentCount = createdCount + updatedCount;
                        var statusCounts = $"{createdCount} created, {updatedCount} updated";

                        await _auditService.LogAttendanceActionAsync(
                            course.CourseCode,
                            date,
                            studentCount,
                            statusCounts
                        );
                    }
                }

                return Json(new
                {
                    success = true,
                    message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all present");
                return Json(new { success = false, message = "Failed to mark attendance" });
            }
        }

        // ✅ NEW: POST: FacultyAttendance/ProcessForgotTimeout
        // Allows faculty to manually check for students who forgot to time out
        [HttpPost]
        public async Task<IActionResult> ProcessForgotTimeout([FromBody] ProcessForgotTimeoutRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify faculty owns this class
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                var classEntity = await _classRepository.GetByIdAsync(request.ClassId);
                if (classEntity == null || classEntity.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                // Parse target date
                DateTime targetDate = string.IsNullOrEmpty(request.Date) 
                    ? DateTime.Today 
                    : DateTime.Parse(request.Date);

                _logger.LogInformation($"✅ Faculty triggering forgot timeout detection for Class {request.ClassId} on {targetDate:yyyy-MM-dd}");

                // Run the forgot timeout detection
                var result = await _absenceDetectionService.ProcessForgotTimeoutForClassAsync(request.ClassId, targetDate);

                if (result.forgotCount > 0)
                {
                    // Log audit
                    if (classEntity?.CourseId != null)
                    {
                        var course = await _courseRepository.GetByIdAsync(classEntity.CourseId.Value);
                        if (course != null)
                        {
                            await _auditService.LogActionAsync(
                                "Forgot Timeout Detection",
                                $"Faculty ran forgot timeout detection for {course.CourseCode} on {targetDate:yyyy-MM-dd}. " +
                                $"Marked {result.forgotCount} student(s) as absent."
                            );
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    message = result.message,
                    forgotCount = result.forgotCount,
                    validCount = result.validCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot timeout");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    public class MarkAllPresentRequest
    {
        public int ClassId { get; set; }
    }

    public class ProcessForgotTimeoutRequest
    {
        public int ClassId { get; set; }
        public string? Date { get; set; }
    }

    public class UpdateAttendanceRequest
    {
        public int ClassId { get; set; }
        public int StudId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string? Date { get; set; }
    }

    public class DeleteAttendanceRequest
    {
        public int ClassId { get; set; }
        public int StudId { get; set; }
        public string? Date { get; set; }
    }
}
