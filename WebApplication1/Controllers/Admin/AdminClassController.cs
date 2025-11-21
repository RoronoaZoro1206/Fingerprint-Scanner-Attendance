using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminClassController : Controller
{
    private readonly ILogger<AdminClassController> _logger;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAcademicTermRepository _academicTermRepository;
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
        _auditService = auditService;
    }

    // GET: Class/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Classes.cshtml");
    }

    // GET: Class/GetAll - DataTables AJAX endpoint (Optimized with DTO)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var classes = await _classRepository.GetAllClassListAsync();
            
            var classData = classes.Select(c => new
            {
                classId = c.ClassId,
                courseId = c.CourseId,
                courseName = c.CourseName,
                roomId = c.RoomId,
                roomName = c.RoomName,
                facId = c.FacId,
                facultyName = c.FacultyName,
                termId = c.TermId,
                termName = c.TermName
            });
            
            return Json(new { data = classData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching classes");
            return Json(new { error = "Failed to fetch classes" });
        }
    }

    // GET: Class/GetById/{id} (Optimized with detailed DTO)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var cls = await _classRepository.GetClassDetailByIdAsync(id);
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

    // GET: Class/GetFaculties (Optimized with single query)
    [HttpGet]
    public async Task<IActionResult> GetFaculties()
    {
        try
        {
            _logger.LogInformation("GetFaculties endpoint called");
            
            var faculties = await _facultyRepository.GetAllSimpleAsync();
            
            var facultyCount = faculties.Count();
            _logger.LogInformation($"Retrieved {facultyCount} faculty records from database");

            var facultyList = faculties.Select(f => new
            {
                facId = f.FacId,
                facName = f.FacName
            }).ToList();

            if (facultyCount == 0)
            {
                _logger.LogWarning("No faculty records found. This may indicate that users with faculty role do not have corresponding faculty records in the faculty table.");
            }
            else
            {
                _logger.LogInformation($"Returning {facultyCount} faculty members: {string.Join(", ", facultyList.Select(f => f.facName))}");
            }

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

            // ✅ FIX: Auto-create faculty record if user has Faculty role but no faculty record
            if (cls.FacId.HasValue)
            {
                // Check if this is a user_id (not fac_id) by checking if faculty record exists
                var facultyExists = await _facultyRepository.ExistsAsync(cls.FacId.Value);
                
                if (!facultyExists)
                {
                    // This is a user_id, not a fac_id - check if user has Faculty role
                    var faculty = await _facultyRepository.GetByUserIdAsync(cls.FacId.Value);
                    
                    if (faculty == null)
                    {
                        // User doesn't have a faculty record - create one automatically
                        var newFaculty = new Models.Faculty
                        {
                            UserId = cls.FacId.Value,
                            DeptId = null,
                            FacRank = null,
                            FacCreatedAt = DateTime.Now
                        };
                        
                        var facultyCreated = await _facultyRepository.AddAsync(newFaculty);
                        
                        if (facultyCreated)
                        {
                            // Get the newly created faculty record to get the correct fac_id
                            faculty = await _facultyRepository.GetByUserIdAsync(cls.FacId.Value);
                            if (faculty != null)
                            {
                                cls.FacId = faculty.FacId;
                                _logger.LogInformation($"Auto-created faculty record (fac_id: {faculty.FacId}) for user_id: {newFaculty.UserId}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to auto-create faculty record for user_id: {cls.FacId.Value}");
                        }
                    }
                    else
                    {
                        // Faculty record exists, use the correct fac_id
                        cls.FacId = faculty.FacId;
                    }
                }
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

            // ✅ FIX: Auto-create faculty record if user has Faculty role but no faculty record
            if (cls.FacId.HasValue)
            {
                // Check if this is a user_id (not fac_id) by checking if faculty record exists
                var facultyExists = await _facultyRepository.ExistsAsync(cls.FacId.Value);
                
                if (!facultyExists)
                {
                    // This is a user_id, not a fac_id - check if user has Faculty role
                    var faculty = await _facultyRepository.GetByUserIdAsync(cls.FacId.Value);
                    
                    if (faculty == null)
                    {
                        // User doesn't have a faculty record - create one automatically
                        var newFaculty = new Models.Faculty
                        {
                            UserId = cls.FacId.Value,
                            DeptId = null,
                            FacRank = null,
                            FacCreatedAt = DateTime.Now
                        };
                        
                        var facultyCreated = await _facultyRepository.AddAsync(newFaculty);
                        
                        if (facultyCreated)
                        {
                            // Get the newly created faculty record to get the correct fac_id
                            faculty = await _facultyRepository.GetByUserIdAsync(cls.FacId.Value);
                            if (faculty != null)
                            {
                                cls.FacId = faculty.FacId;
                                _logger.LogInformation($"Auto-created faculty record (fac_id: {faculty.FacId}) for user_id: {newFaculty.UserId}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to auto-create faculty record for user_id: {cls.FacId.Value}");
                        }
                    }
                    else
                    {
                        // Faculty record exists, use the correct fac_id
                        cls.FacId = faculty.FacId;
                    }
                }
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
