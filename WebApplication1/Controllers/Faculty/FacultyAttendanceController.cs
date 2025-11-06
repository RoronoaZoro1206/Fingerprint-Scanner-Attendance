using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services; // ✅ ADDED: For UniversalAuditService
using Microsoft.AspNetCore.Mvc;

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

        public FacultyAttendanceController(
            ILogger<FacultyAttendanceController> logger,
            IFacultyRepository facultyRepository,
            IAttendanceRepository attendanceRepository,
            IEnrollmentRepository enrollmentRepository,
            IClassRepository classRepository,
            ICourseRepository courseRepository, // ✅ ADDED: Inject course repository
            UniversalAuditService auditService) // ✅ ADDED: Inject universal audit service
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _attendanceRepository = attendanceRepository;
            _enrollmentRepository = enrollmentRepository;
            _classRepository = classRepository;
            _courseRepository = courseRepository; // ✅ ADDED: Store course repository
            _auditService = auditService; // ✅ ADDED: Store audit service
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

                // Parse target date
                DateTime targetDate = string.IsNullOrEmpty(date) 
                    ? DateTime.Today 
                    : DateTime.TryParse(date, out DateTime parsedDate) ? parsedDate : DateTime.Today;

                // Single optimized query with all JOINs and faculty verification
                var attendanceData = await _attendanceRepository.GetClassAttendanceWithDetailsAsync(
                    classId, targetDate, faculty.FacId);

                if (attendanceData == null)
                {
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                return Json(new
                {
                    success = true,
                    data = attendanceData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching class attendance");
                return Json(new { success = false, message = "Failed to fetch attendance" });
            }
        }

        // PUT: FacultyAttendance/UpdateAttendance
        [HttpPut]
        public async Task<IActionResult> UpdateAttendance([FromBody] UpdateAttendanceRequest request)
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
                var now = DateTime.Now;
                var allAttendance = await _attendanceRepository.GetAllAsync();
                var attendanceRecords = allAttendance
                    .Where(a => a.ClassId == request.ClassId && a.StudId == request.StudId && a.AttendDate.Date == targetDate)
                    .ToList();

                bool result = false;
                int updatedCount = 0;

                if (!attendanceRecords.Any())
                {
                    // Create new attendance record if none exist
                    var newAttendance = new Attendance
                    {
                        ClassId = request.ClassId,
                        StudId = request.StudId,
                        AttendDate = targetDate,
                        AttendScanTime = now,
                        AttendType = ScanType.In,
                        AttendStatus = Enum.Parse<AttendanceStatus>(request.Status),
                        AttendRemark = request.Remarks,
                        AttendCreatedAt = now
                    };

                    result = await _attendanceRepository.AddAsync(newAttendance);
                    if (result) updatedCount = 1;
                }
                else
                {
                    // Update all existing attendance records (both In and Out) with the same status and remark
                    foreach (var attendance in attendanceRecords)
                    {
                        attendance.AttendStatus = Enum.Parse<AttendanceStatus>(request.Status);
                        attendance.AttendRemark = request.Remarks;

                        var updateResult = await _attendanceRepository.UpdateAsync(attendance);
                        if (updateResult)
                        {
                            updatedCount++;
                        }
                    }
                    result = updatedCount > 0;
                }

                if (result)
                {
                    // ✅ ADDED: Log audit for individual attendance update
                    if (classEntity?.CourseId != null)
                    {
                        var course = await _courseRepository.GetByIdAsync(classEntity.CourseId.Value);
                        if (course != null)
                        {
                            await _auditService.LogActionAsync(
                                "Attendance Status Changed",
                                $"Faculty changed attendance for student {request.StudId} in {course.CourseCode} " +
                                $"on {targetDate:yyyy-MM-dd} to {request.Status}"
                            );
                        }
                    }

                    var message = updatedCount > 1 
                        ? $"Attendance updated successfully ({updatedCount} records)" 
                        : "Attendance updated successfully";
                    return Json(new { success = true, message = message });
                }

                return Json(new { success = false, message = "Failed to update attendance" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance");
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
                var allAttendance = await _attendanceRepository.GetAllAsync();
                var attendanceRecords = allAttendance
                    .Where(a => a.ClassId == request.ClassId && a.StudId == request.StudId && a.AttendDate.Date == targetDate)
                    .ToList();

                if (!attendanceRecords.Any())
                {
                    return Json(new { success = false, message = "Attendance record not found" });
                }

                // Delete all attendance records for this student today
                int deletedCount = 0;
                foreach (var record in attendanceRecords)
                {
                    var result = await _attendanceRepository.DeleteAsync(record.AttendId);
                    if (result)
                    {
                        deletedCount++;
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
                                $"on {targetDate:yyyy-MM-dd} ({deletedCount} record(s))"
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
    }

    public class MarkAllPresentRequest
    {
        public int ClassId { get; set; }
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
