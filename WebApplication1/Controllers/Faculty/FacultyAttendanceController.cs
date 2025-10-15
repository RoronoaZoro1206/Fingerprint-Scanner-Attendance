using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    public class FacultyAttendanceController : Controller
    {
        private readonly ILogger<FacultyAttendanceController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IClassRepository _classRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;

        public FacultyAttendanceController(
            ILogger<FacultyAttendanceController> logger,
            IFacultyRepository facultyRepository,
            IClassRepository classRepository,
            IEnrollmentRepository enrollmentRepository,
            IAttendanceRepository attendanceRepository,
            IStudentRepository studentRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _classRepository = classRepository;
            _enrollmentRepository = enrollmentRepository;
            _attendanceRepository = attendanceRepository;
            _studentRepository = studentRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
        }

        // GET: FacultyAttendance/Index
        public IActionResult Index()
        {
            return View("~/Views/Faculty/FacultyAttendance.cshtml");
        }

        // GET: FacultyAttendance/GetClassAttendance
        [HttpGet]
        public async Task<IActionResult> GetClassAttendance(int classId)
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

                var classEntity = await _classRepository.GetByIdAsync(classId);
                if (classEntity == null || classEntity.FacId != faculty.FacId)
                {
                    return Json(new { success = false, message = "Class not found or access denied" });
                }

                // Get course info
                var course = classEntity.CourseId.HasValue 
                    ? await _courseRepository.GetByIdAsync(classEntity.CourseId.Value) 
                    : null;

                // Get enrolled students
                var enrollments = await _enrollmentRepository.GetByClassIdAsync(classId);
                var allStudents = await _studentRepository.GetAllAsync();
                var allUsers = await _userRepository.GetAllAsync();
                var allAttendance = await _attendanceRepository.GetAllAsync();

                var studentDict = allStudents.ToDictionary(s => s.StudId);
                var userDict = allUsers.ToDictionary(u => u.UserId);

                // Get today's attendance
                var today = DateTime.Today;
                var todayAttendance = allAttendance
                    .Where(a => a.ClassId == classId && a.AttendDate.Date == today)
                    .GroupBy(a => a.StudId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(a => a.AttendScanTime).ToList());

                var studentsData = new List<object>();
                foreach (var enrollment in enrollments)
                {
                    if (!studentDict.ContainsKey(enrollment.StudId))
                        continue;

                    var student = studentDict[enrollment.StudId];
                    var user = userDict.ContainsKey(student.UserId) ? userDict[student.UserId] : null;

                    var attendanceRecords = todayAttendance.ContainsKey(student.StudId) 
                        ? todayAttendance[student.StudId] 
                        : new List<Attendance>();

                    // Get Time In (first scan of type In)
                    var timeInRecord = attendanceRecords.FirstOrDefault(a => a.AttendType == ScanType.In);
                    var timeIn = timeInRecord?.AttendScanTime.ToString("hh:mm:ss tt");

                    // Get Time Out (first scan of type Out)
                    var timeOutRecord = attendanceRecords.FirstOrDefault(a => a.AttendType == ScanType.Out);
                    var timeOut = timeOutRecord?.AttendScanTime.ToString("hh:mm:ss tt");

                    // Determine status based on attendance records
                    var status = "Absent";
                    if (attendanceRecords.Any())
                    {
                        var latestRecord = attendanceRecords.OrderByDescending(a => a.AttendScanTime).First();
                        status = latestRecord.AttendStatus.ToString();
                    }

                    studentsData.Add(new
                    {
                        studId = student.StudId,
                        studentNumber = student.StudNumber,
                        name = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : "Unknown",
                        timeIn = timeIn,
                        timeOut = timeOut,
                        status = status,
                        attendId = timeInRecord?.AttendId
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        courseCode = course?.CourseCode ?? "N/A",
                        courseName = course?.CourseName ?? "N/A",
                        date = DateTime.Now.ToString("M/d/yyyy"),
                        students = studentsData
                    }
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

                // Get today's attendance for this student
                var today = DateTime.Today;
                var allAttendance = await _attendanceRepository.GetAllAsync();
                var attendance = allAttendance
                    .Where(a => a.ClassId == request.ClassId && a.StudId == request.StudId && a.AttendDate.Date == today)
                    .OrderByDescending(a => a.AttendScanTime)
                    .FirstOrDefault();

                if (attendance == null)
                {
                    return Json(new { success = false, message = "Attendance record not found" });
                }

                // Update attendance
                attendance.AttendStatus = Enum.Parse<AttendanceStatus>(request.Status);
                attendance.AttendRemark = request.Remarks;

                var result = await _attendanceRepository.UpdateAsync(attendance);

                if (result)
                {
                    return Json(new { success = true, message = "Attendance updated successfully" });
                }

                return Json(new { success = false, message = "Failed to update attendance" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance");
                return Json(new { success = false, message = "Failed to update attendance" });
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

                // Get today's attendance for this student
                var today = DateTime.Today;
                var allAttendance = await _attendanceRepository.GetAllAsync();
                var attendanceRecords = allAttendance
                    .Where(a => a.ClassId == request.ClassId && a.StudId == request.StudId && a.AttendDate.Date == today)
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
                    .ToList();

                var existingStudentIds = todayAttendance.Select(a => a.StudId).ToHashSet();

                int markedCount = 0;

                // Mark all students as present
                foreach (var enrollment in enrollments)
                {
                    // Skip if already has attendance today
                    if (existingStudentIds.Contains(enrollment.StudId))
                        continue;

                    var attendance = new Attendance
                    {
                        ClassId = request.ClassId,
                        StudId = enrollment.StudId,
                        AttendDate = today,
                        AttendScanTime = now,
                        AttendType = ScanType.In,
                        AttendStatus = AttendanceStatus.Present,
                        AttendCreatedAt = now
                    };

                    var result = await _attendanceRepository.AddAsync(attendance);
                    if (result)
                    {
                        markedCount++;
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Marked {markedCount} student(s) as present"
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
    }

    public class DeleteAttendanceRequest
    {
        public int ClassId { get; set; }
        public int StudId { get; set; }
    }
}
