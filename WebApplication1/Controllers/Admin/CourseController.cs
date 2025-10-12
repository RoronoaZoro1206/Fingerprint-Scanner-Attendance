using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class CourseController : Controller
{
    private readonly ILogger<CourseController> _logger;
    private readonly ICourseRepository _courseRepository;
    private readonly IProgramRepository _programRepository;

    public CourseController(ILogger<CourseController> logger, ICourseRepository courseRepository, IProgramRepository programRepository)
    {
        _logger = logger;
        _courseRepository = courseRepository;
        _programRepository = programRepository;
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
            var courses = await _courseRepository.GetAllAsync();
            var programs = await _programRepository.GetAllAsync();
            
            var courseData = courses.Select(c => new
            {
                c.CourseId,
                c.CourseCode,
                c.CourseName,
                c.CourseDesc,
                c.CourseUnits,
                c.ProgramId,
                ProgramName = programs.FirstOrDefault(p => p.ProgramId == c.ProgramId)?.ProgramName ?? "N/A"
            });
            
            return Json(new { data = courseData });
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
            return Json(new { success = true, data = course });
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
            return Json(new { success = true, data = programs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching programs");
            return Json(new { success = false, message = "Failed to fetch programs" });
        }
    }

    // POST: Course/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course course)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            course.CourseCreatedAt = DateTime.Now;
            var result = await _courseRepository.AddAsync(course);

            if (result)
            {
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
    public async Task<IActionResult> Update([FromBody] Course course)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _courseRepository.UpdateAsync(course);

            if (result)
            {
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
            var result = await _courseRepository.DeleteAsync(id);

            if (result)
            {
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
