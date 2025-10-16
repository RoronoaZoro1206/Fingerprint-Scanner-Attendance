using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class AdminClassScheduleController : Controller
{
    private readonly ILogger<AdminClassScheduleController> _logger;
    private readonly IClassScheduleRepository _classScheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly AdminAuditService _auditService;

    public AdminClassScheduleController(
        ILogger<AdminClassScheduleController> logger,
        IClassScheduleRepository classScheduleRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _classScheduleRepository = classScheduleRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _auditService = auditService;
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

            // Single optimized query with all JOINs instead of 3 separate queries
            var schedules = await _classScheduleRepository.GetAllWithDetailsAsync();

            _logger.LogInformation($"Returning {schedules.Count()} schedule records");
            return Json(new { data = schedules });
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
            {
                var cls = await _classRepository.GetByIdAsync(dto.ClassId);
                var course = cls != null && cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
                await _auditService.LogActionAsync(
                    "Created Class Schedule", 
                    $"Created schedule for {course?.CourseName ?? "Unknown"} on {dto.SchedDayOfWeek} ({dto.SchedStartTime}-{dto.SchedEndTime})");
                return Json(new { success = true, message = "Schedule created successfully" });
            }
            
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
            {
                await _auditService.LogActionAsync(
                    "Created Multiple Class Schedules", 
                    $"Created {successCount} class schedule(s) successfully");
                return Json(new { success = true, message = $"Created {successCount} schedule(s) successfully" });
            }
            
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
            {
                var cls = await _classRepository.GetByIdAsync(dto.ClassId);
                var course = cls != null && cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
                await _auditService.LogActionAsync(
                    "Updated Class Schedule", 
                    $"Updated schedule for {course?.CourseName ?? "Unknown"} on {dto.SchedDayOfWeek} ({dto.SchedStartTime}-{dto.SchedEndTime})");
                return Json(new { success = true, message = "Schedule updated successfully" });
            }
            
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
            var schedule = await _classScheduleRepository.GetByIdAsync(id);
            var cls = schedule != null ? await _classRepository.GetByIdAsync(schedule.ClassId) : null;
            var course = cls != null && cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
            
            var result = await _classScheduleRepository.DeleteAsync(id);
            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Class Schedule", 
                    $"Deleted schedule for {course?.CourseName ?? "Unknown"} on {schedule?.SchedDayOfWeek}");
                return Json(new { success = true, message = "Schedule deleted successfully" });
            }
            
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
