using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Faculty
{
    [RoleAuthorize(USER_ROLES.FACULTY)]
    public class FacultyClassController : Controller
    {
        private readonly ILogger<FacultyClassController> _logger;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IClassRepository _classRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IClassScheduleRepository _classScheduleRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IAcademicTermRepository _academicTermRepository;

        public FacultyClassController(
            ILogger<FacultyClassController> logger,
            IFacultyRepository facultyRepository,
            IClassRepository classRepository,
            IEnrollmentRepository enrollmentRepository,
            IClassScheduleRepository classScheduleRepository,
            ICourseRepository courseRepository,
            IRoomRepository roomRepository,
            IAcademicTermRepository academicTermRepository)
        {
            _logger = logger;
            _facultyRepository = facultyRepository;
            _classRepository = classRepository;
            _enrollmentRepository = enrollmentRepository;
            _classScheduleRepository = classScheduleRepository;
            _courseRepository = courseRepository;
            _roomRepository = roomRepository;
            _academicTermRepository = academicTermRepository;
        }

        // GET: FacultyClass/Index
        public IActionResult Index()
        {
            return View("~/Views/Faculty/Index.cshtml");
        }

        // GET: FacultyClass/GetMyClasses
        [HttpGet]
        public async Task<IActionResult> GetMyClasses()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get faculty record for this user
                var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
                if (faculty == null)
                {
                    return Json(new { success = false, message = "Faculty record not found" });
                }

                // Get all classes for this faculty
                var classes = await _classRepository.GetClassesByFacultyIdAsync(faculty.FacId);
                
                // Get all related data
                var allCourses = await _courseRepository.GetAllAsync();
                var allRooms = await _roomRepository.GetAllAsync();
                var allTerms = await _academicTermRepository.GetAllAsync();

                var courseDict = allCourses.ToDictionary(c => c.CourseId);
                var roomDict = allRooms.ToDictionary(r => r.RoomId);
                var termDict = allTerms.ToDictionary(t => t.TermId);

                // Get schedules and enrollment count for each class
                var classesData = new List<object>();
                foreach (var cls in classes)
                {
                    var schedules = await _classScheduleRepository.GetByClassIdAsync(cls.ClassId);
                    var enrollments = await _enrollmentRepository.GetByClassIdAsync(cls.ClassId);

                    // Get course, room, and term info
                    var course = cls.CourseId.HasValue && courseDict.ContainsKey(cls.CourseId.Value) 
                        ? courseDict[cls.CourseId.Value] 
                        : null;
                    var room = cls.RoomId.HasValue && roomDict.ContainsKey(cls.RoomId.Value) 
                        ? roomDict[cls.RoomId.Value] 
                        : null;
                    var term = cls.TermId.HasValue && termDict.ContainsKey(cls.TermId.Value) 
                        ? termDict[cls.TermId.Value] 
                        : null;

                    // Format schedule string
                    var scheduleString = "";
                    if (schedules.Any())
                    {
                        var firstSchedule = schedules.First();
                        scheduleString = $"{firstSchedule.SchedDayOfWeek} {firstSchedule.SchedStartTime:hh\\:mm} - {firstSchedule.SchedEndTime:hh\\:mm}";
                    }

                    classesData.Add(new
                    {
                        classId = cls.ClassId,
                        courseCode = course?.CourseCode ?? "N/A",
                        courseName = course?.CourseName ?? "N/A",
                        roomName = room?.RoomCode ?? "N/A",
                        schedule = scheduleString,
                        studentCount = enrollments.Count(),
                        termName = term?.TermName ?? "N/A"
                    });
                }

                return Json(new { success = true, data = classesData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching faculty classes");
                return Json(new { success = false, message = "Failed to fetch classes" });
            }
        }
    }
}
