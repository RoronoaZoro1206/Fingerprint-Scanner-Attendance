using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminCourseController : Controller
{
    private readonly ILogger<AdminCourseController> _logger;
    private readonly ICourseRepository _courseRepository;
    private readonly IProgramRepository _programRepository;
    private readonly AdminAuditService _auditService;

    public AdminCourseController(
        ILogger<AdminCourseController> logger, 
        ICourseRepository courseRepository, 
        IProgramRepository programRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _courseRepository = courseRepository;
        _programRepository = programRepository;
        _auditService = auditService;
    }

    // GET: Course/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Courses.cshtml");
    }

    // GET: Course/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // Single optimized query with JOIN instead of 2 separate queries
            var courses = await _courseRepository.GetAllWithProgramAsync();
            
            // ? FIX: Convert PascalCase to snake_case to match database and JavaScript
            var result = courses.Select(c => new
            {
                course_id = c.CourseId,
                course_code = c.CourseCode,
                course_name = c.CourseName,
                course_desc = c.CourseDesc,
                course_units = c.CourseUnits,
                program_id = c.ProgramId,
                program_name = c.ProgramName
            });
            
            return Json(new { data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses");
            return Json(new { error = "Failed to fetch courses" });
        }
    }

    // GET: Course/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                return Json(new { success = false, message = "Course not found" });
            }
            
            // ? FIX: Convert PascalCase to snake_case
            var result = new
            {
                course_id = course.CourseId,
                course_code = course.CourseCode,
                course_name = course.CourseName,
                course_desc = course.CourseDesc,
                course_units = course.CourseUnits,
                program_id = course.ProgramId
            };
            
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course by ID");
            return Json(new { success = false, message = "Failed to fetch course" });
        }
    }

    // GET: Course/GetPrograms - Get all programs for dropdown
    [HttpGet]
    public async Task<IActionResult> GetPrograms()
    {
        try
        {
            var programs = await _programRepository.GetAllAsync();
            
            // ? FIX: Convert PascalCase to snake_case for JavaScript
            var programsData = programs.Select(p => new
            {
                program_id = p.ProgramId,
                program_name = p.ProgramName,
                program_code = p.ProgramCode,
                dept_id = p.DeptId
            });
            
            return Json(new { success = true, data = programsData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching programs");
            return Json(new { success = false, message = "Failed to fetch programs" });
        }
    }

    // POST: Course/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course? course)
    {
        try
        {
            if (course == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            if (!course.ProgramId.HasValue || course.ProgramId.Value <= 0)
            {
                return Json(new { success = false, message = "Program selection is required" });
            }

            // ✅ NEW: Check for duplicate course code
            var isDuplicateCode = await _courseRepository.IsCourseCodeDuplicateAsync(course.CourseCode);
            if (isDuplicateCode)
            {
                _logger.LogWarning($"Attempted to create course with duplicate code: {course.CourseCode}");
                return Json(new { 
                    success = false, 
                    message = $"Course code '{course.CourseCode}' is already in use. Please use a different course code." 
                });
            }

            // ✅ NEW: Check for duplicate course name
            var isDuplicateName = await _courseRepository.IsCourseNameDuplicateAsync(course.CourseName);
            if (isDuplicateName)
            {
                _logger.LogWarning($"Attempted to create course with duplicate name: {course.CourseName}");
                return Json(new { 
                    success = false, 
                    message = $"Course name '{course.CourseName}' already exists. Please use a different course name." 
                });
            }

            course.CourseCreatedAt = DateTime.Now;
            var result = await _courseRepository.AddAsync(course);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Created Course", 
                    $"Created course: {course.CourseCode} - {course.CourseName}");
                return Json(new { success = true, message = "Course created successfully" });
            }
            return Json(new { success = false, message = "Failed to create course" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            return Json(new { success = false, message = "An error occurred while creating course" });
        }
    }

    // PUT: Course/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Course? course)
    {
        try
        {
            if (course == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            if (!course.ProgramId.HasValue || course.ProgramId.Value <= 0)
            {
                return Json(new { success = false, message = "Program selection is required" });
            }

            // ✅ NEW: Check for duplicate course code (excluding current course)
            var isDuplicateCode = await _courseRepository.IsCourseCodeDuplicateAsync(
                course.CourseCode, 
                course.CourseId);
            
            if (isDuplicateCode)
            {
                _logger.LogWarning($"Attempted to update course {course.CourseId} with duplicate code: {course.CourseCode}");
                return Json(new { 
                    success = false, 
                    message = $"Course code '{course.CourseCode}' is already in use by another course. Please use a different course code." 
                });
            }

            // ✅ NEW: Check for duplicate course name (excluding current course)
            var isDuplicateName = await _courseRepository.IsCourseNameDuplicateAsync(
                course.CourseName, 
                course.CourseId);
            
            if (isDuplicateName)
            {
                _logger.LogWarning($"Attempted to update course {course.CourseId} with duplicate name: {course.CourseName}");
                return Json(new { 
                    success = false, 
                    message = $"Course name '{course.CourseName}' is already in use by another course. Please use a different course name." 
                });
            }

            var result = await _courseRepository.UpdateAsync(course);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Course", 
                    $"Updated course: {course.CourseCode} - {course.CourseName}");
                return Json(new { success = true, message = "Course updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update course" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course");
            return Json(new { success = false, message = "An error occurred while updating course" });
        }
    }

    // DELETE: Course/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            var result = await _courseRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Course", 
                    $"Deleted course: {course?.CourseCode} - {course?.CourseName}");
                return Json(new { success = true, message = "Course deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete course" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course");
            return Json(new { success = false, message = "An error occurred while deleting course" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
