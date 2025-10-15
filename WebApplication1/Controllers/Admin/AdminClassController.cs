using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class AdminClassController : Controller
{
    private readonly ILogger<AdminClassController> _logger;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAcademicTermRepository _academicTermRepository;
    private readonly IUserRepository _userRepository;
    private readonly AdminAuditService _auditService;

    public AdminClassController(
        ILogger<AdminClassController> logger, 
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IRoomRepository roomRepository,
        IFacultyRepository facultyRepository,
        IAcademicTermRepository academicTermRepository,
        IUserRepository userRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _roomRepository = roomRepository;
        _facultyRepository = facultyRepository;
        _academicTermRepository = academicTermRepository;
        _userRepository = userRepository;
        _auditService = auditService;
    }

    // GET: Class/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Classes.cshtml");
    }

    // GET: Class/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var classes = await _classRepository.GetAllWithDetailsAsync();
            
            var classData = classes.Select(c => new
            {
                c.ClassId,
                c.CourseId,
                CourseName = c.Course?.CourseName ?? "N/A",
                c.RoomId,
                RoomName = c.Room?.RoomName ?? "N/A",
                c.FacId,
                FacultyName = c.Faculty?.FacName ?? "N/A",
                c.TermId,
                TermName = c.Term?.TermName ?? "N/A"
            });
            
            return Json(new { data = classData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching classes");
            return Json(new { error = "Failed to fetch classes" });
        }
    }

    // GET: Class/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var cls = await _classRepository.GetByIdAsync(id);
            if (cls == null)
            {
                return Json(new { success = false, message = "Class not found" });
            }
            return Json(new { success = true, data = cls });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching class by ID");
            return Json(new { success = false, message = "Failed to fetch class" });
        }
    }

    // GET: Class/GetCourses
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        try
        {
            var courses = await _courseRepository.GetAllAsync();
            return Json(new { success = true, data = courses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses");
            return Json(new { success = false, message = "Failed to fetch courses" });
        }
    }

    // GET: Class/GetRooms
    [HttpGet]
    public async Task<IActionResult> GetRooms()
    {
        try
        {
            var rooms = await _roomRepository.GetAllAsync();
            return Json(new { success = true, data = rooms });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rooms");
            return Json(new { success = false, message = "Failed to fetch rooms" });
        }
    }

    // GET: Class/GetFaculties
    [HttpGet]
    public async Task<IActionResult> GetFaculties()
    {
        try
        {
            var faculties = await _facultyRepository.GetAllAsync();
            var allUsers = await _userRepository.GetAllAsync();
            var userDict = allUsers.ToDictionary(u => u.UserId);

            var facultyList = faculties.Select(f => new
            {
                facId = f.FacId,
                facName = userDict.ContainsKey(f.UserId) 
                    ? $"{userDict[f.UserId].UserFirstName} {userDict[f.UserId].UserLastName}".Trim()
                    : "Unknown"
            });

            return Json(new { success = true, data = facultyList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching faculties");
            return Json(new { success = false, message = "Failed to fetch faculties" });
        }
    }

    // GET: Class/GetTerms
    [HttpGet]
    public async Task<IActionResult> GetTerms()
    {
        try
        {
            var terms = await _academicTermRepository.GetAllAsync();
            return Json(new { success = true, data = terms });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching academic terms");
            return Json(new { success = false, message = "Failed to fetch academic terms" });
        }
    }

    // POST: Class/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Class cls)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            cls.ClassCreatedAt = DateTime.Now;
            cls.ClassUpdatedAt = DateTime.Now;
            var result = await _classRepository.AddAsync(cls);

            if (result)
            {
                var course = cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
                await _auditService.LogActionAsync(
                    "Created Class", 
                    $"Created class for course: {course?.CourseName ?? "Unknown"}");
                return Json(new { success = true, message = "Class created successfully" });
            }
            return Json(new { success = false, message = "Failed to create class" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating class");
            return Json(new { success = false, message = "An error occurred while creating class" });
        }
    }

    // PUT: Class/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Class cls)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            cls.ClassUpdatedAt = DateTime.Now;
            var result = await _classRepository.UpdateAsync(cls);

            if (result)
            {
                var course = cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
                await _auditService.LogActionAsync(
                    "Updated Class", 
                    $"Updated class for course: {course?.CourseName ?? "Unknown"}");
                return Json(new { success = true, message = "Class updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update class" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating class");
            return Json(new { success = false, message = "An error occurred while updating class" });
        }
    }

    // DELETE: Class/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var cls = await _classRepository.GetByIdAsync(id);
            var course = cls != null && cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
            var result = await _classRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Class", 
                    $"Deleted class for course: {course?.CourseName ?? "Unknown"}");
                return Json(new { success = true, message = "Class deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete class" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting class");
            return Json(new { success = false, message = "An error occurred while deleting class" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
