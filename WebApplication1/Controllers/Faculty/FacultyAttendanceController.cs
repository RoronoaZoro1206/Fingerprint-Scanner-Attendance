using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    public class FacultyAttendanceController : Controller
    {
        private readonly ILogger<FacultyAttendanceController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IClassRepository _classRepository;

        public FacultyAttendanceController(
            ILogger<FacultyAttendanceController> logger,
            IFacultyRepository facultyRepository,
            IAttendanceRepository attendanceRepository,
            IEnrollmentRepository enrollmentRepository,
            IClassRepository classRepository)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _attendanceRepository = attendanceRepository;
            _enrollmentRepository = enrollmentRepository;
            _classRepository = classRepository;
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
                var targetDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);

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
                    .Where(a => a.ClassId == request.ClassId && a.AttendDate.Date == today)
                    .GroupBy(a => a.StudId)
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
