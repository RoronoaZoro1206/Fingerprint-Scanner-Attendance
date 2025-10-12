using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Controllers;

public class ClassScheduleController : Controller
{
    private readonly ILogger<ClassScheduleController> _logger;
    private readonly IClassScheduleRepository _classScheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;

    public ClassScheduleController(
        ILogger<ClassScheduleController> logger,
        IClassScheduleRepository classScheduleRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository)
    {
        _logger = logger;
        _classScheduleRepository = classScheduleRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
    }

    public IActionResult Index()
    {
        return View("~/Views/Admin/ClassSchedules.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all class schedules...");

            var schedules = await _classScheduleRepository.GetAllAsync();
            _logger.LogInformation($"Found {schedules.Count()} schedules");

            var classes = await _classRepository.GetAllAsync();
            var courses = await _courseRepository.GetAllAsync();

            _logger.LogInformation($"Found {classes.Count()} classes and {courses.Count()} courses");

            var classDict = classes.ToDictionary(c => c.ClassId);
            var courseDict = courses.ToDictionary(c => c.CourseId);

            var scheduleList = schedules.Select(s =>
            {
                var cls = classDict.ContainsKey(s.ClassId) ? classDict[s.ClassId] : null;
                var courseId = cls?.CourseId;
                var course = courseId.HasValue && courseDict.ContainsKey(courseId.Value)
                    ? courseDict[courseId.Value]
                    : null;

                return new
                {
                    schedId = s.SchedId,
                    classId = s.ClassId,
                    courseName = course?.CourseName ?? "N/A",
                    schedDayOfWeek = s.SchedDayOfWeek,
                    schedStartTime = s.SchedStartTime.ToString(@"hh\:mm"),
                    schedEndTime = s.SchedEndTime.ToString(@"hh\:mm")
                };
            }).ToList();

            _logger.LogInformation($"Returning {scheduleList.Count} schedule records");
            return Json(new { data = scheduleList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching class schedules: {Message}", ex.Message);
            return Json(new { error = $"Failed to fetch class schedules: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var schedule = await _classScheduleRepository.GetByIdAsync(id);
            if (schedule == null)
                return Json(new { success = false, message = "Schedule not found" });

            return Json(new { success = true, data = schedule });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedule");
            return Json(new { success = false, message = "Failed to fetch schedule" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        try
        {
            var classes = await _classRepository.GetAllWithDetailsAsync();
            var classList = classes.Select(c => new
            {
                classId = c.ClassId,
                courseName = c.Course?.CourseName ?? "N/A"
            });
            return Json(new { success = true, data = classList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching classes");
            return Json(new { success = false, message = "Failed to fetch classes" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClassSchedule schedule)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var result = await _classScheduleRepository.AddAsync(schedule);
            if (result)
                return Json(new { success = true, message = "Schedule created successfully" });
            
            return Json(new { success = false, message = "Failed to create schedule" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule");
            return Json(new { success = false, message = "An error occurred while creating schedule" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ClassSchedule schedule)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var result = await _classScheduleRepository.UpdateAsync(schedule);
            if (result)
                return Json(new { success = true, message = "Schedule updated successfully" });
            
            return Json(new { success = false, message = "Failed to update schedule" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule");
            return Json(new { success = false, message = "An error occurred while updating schedule" });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _classScheduleRepository.DeleteAsync(id);
            if (result)
                return Json(new { success = true, message = "Schedule deleted successfully" });
            
            return Json(new { success = false, message = "Failed to delete schedule" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule");
            return Json(new { success = false, message = "An error occurred while deleting schedule" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
