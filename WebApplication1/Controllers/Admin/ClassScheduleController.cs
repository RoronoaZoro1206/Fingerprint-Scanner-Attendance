using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

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
    public async Task<IActionResult> Create([FromBody] ClassScheduleDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            if (!TimeSpan.TryParse(dto.SchedStartTime, out var startTime))
                return Json(new { success = false, message = "Invalid start time format" });

            if (!TimeSpan.TryParse(dto.SchedEndTime, out var endTime))
                return Json(new { success = false, message = "Invalid end time format" });

            var schedule = new ClassSchedule
            {
                ClassId = dto.ClassId,
                SchedDayOfWeek = dto.SchedDayOfWeek,
                SchedStartTime = startTime,
                SchedEndTime = endTime
            };

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

    [HttpPost]
    public async Task<IActionResult> CreateMultiple([FromBody] List<ClassScheduleDto> scheduleDtos)
    {
        try
        {
            if (scheduleDtos == null || !scheduleDtos.Any())
                return Json(new { success = false, message = "No schedules provided" });

            _logger.LogInformation($"Received {scheduleDtos.Count} schedules to create");

            int successCount = 0;
            int failCount = 0;

            foreach (var dto in scheduleDtos)
            {
                _logger.LogInformation($"Processing schedule: ClassId={dto.ClassId}, Day={dto.SchedDayOfWeek}, Start={dto.SchedStartTime}, End={dto.SchedEndTime}");

                if (!TimeSpan.TryParse(dto.SchedStartTime, out var startTime))
                {
                    _logger.LogWarning($"Invalid start time format: {dto.SchedStartTime}");
                    failCount++;
                    continue;
                }

                if (!TimeSpan.TryParse(dto.SchedEndTime, out var endTime))
                {
                    _logger.LogWarning($"Invalid end time format: {dto.SchedEndTime}");
                    failCount++;
                    continue;
                }

                var schedule = new ClassSchedule
                {
                    ClassId = dto.ClassId,
                    SchedDayOfWeek = dto.SchedDayOfWeek,
                    SchedStartTime = startTime,
                    SchedEndTime = endTime
                };

                var result = await _classScheduleRepository.AddAsync(schedule);
                if (result)
                {
                    successCount++;
                    _logger.LogInformation($"Successfully created schedule for ClassId={dto.ClassId}");
                }
                else
                {
                    failCount++;
                    _logger.LogWarning($"Failed to create schedule for ClassId={dto.ClassId}");
                }
            }

            if (successCount > 0)
                return Json(new { success = true, message = $"Created {successCount} schedule(s) successfully" });
            
            return Json(new { success = false, message = "Failed to create schedules" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple schedules: {Message}", ex.Message);
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    public class ClassScheduleDto
    {
        public int ClassId { get; set; }
        public string SchedDayOfWeek { get; set; } = string.Empty;
        public string SchedStartTime { get; set; } = string.Empty;
        public string SchedEndTime { get; set; } = string.Empty;
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ClassScheduleUpdateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            if (!TimeSpan.TryParse(dto.SchedStartTime, out var startTime))
                return Json(new { success = false, message = "Invalid start time format" });

            if (!TimeSpan.TryParse(dto.SchedEndTime, out var endTime))
                return Json(new { success = false, message = "Invalid end time format" });

            var schedule = new ClassSchedule
            {
                SchedId = dto.SchedId,
                ClassId = dto.ClassId,
                SchedDayOfWeek = dto.SchedDayOfWeek,
                SchedStartTime = startTime,
                SchedEndTime = endTime
            };

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

    public class ClassScheduleUpdateDto : ClassScheduleDto
    {
        public int SchedId { get; set; }
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
