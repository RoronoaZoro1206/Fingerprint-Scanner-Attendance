using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
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
            // ✅ CHANGED: Use new method to get only unscheduled (TBA) classes
            var classes = await _classScheduleRepository.GetUnscheduledClassesAsync();
            
            // Format response with PascalCase - now includes FacultyName for proper class identification
            var classList = classes.Select(c => new
            {
                ClassId = c.ClassId,
                CourseName = c.CourseName,
                FacultyName = c.FacultyName,
                // Display name combines course and faculty for unique identification
                DisplayName = c.FacultyName != "TBA" 
                    ? $"{c.CourseName} - {c.FacultyName}" 
                    : c.CourseName
            });
            
            _logger.LogInformation($"Returning {classList.Count()} unscheduled classes for dropdown");
            return Json(new { success = true, data = classList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unscheduled classes");
            return Json(new { success = false, message = "Failed to fetch classes" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClassScheduleDto? dto)
    {
        try
        {
            if (dto == null)
                return Json(new { success = false, message = "Invalid request body" });
            
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

            _logger.LogInformation($"Received {scheduleDtos.Count} schedules to create for ClassId={scheduleDtos[0].ClassId}");

            // ✅ STEP 1: Check for duplicates WITHIN the submitted batch
            var duplicatesInBatch = scheduleDtos
                .GroupBy(s => new { s.SchedDayOfWeek, s.SchedStartTime, s.SchedEndTime })
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key.SchedDayOfWeek} at {g.Key.SchedStartTime} - {g.Key.SchedEndTime}")
                .ToList();

            if (duplicatesInBatch.Any())
            {
                _logger.LogWarning($"Duplicate schedules detected in submission: {string.Join(", ", duplicatesInBatch)}");
                return Json(new { 
                    success = false, 
                    message = $"❌ Duplicate schedules detected in your submission:\n\n" +
                             string.Join("\n", duplicatesInBatch) + "\n\n" +
                             "Each day/time combination can only appear once per class."
                });
            }

            // ✅ STEP 1.5: Check for faculty AND room schedule conflicts
            var classId = scheduleDtos[0].ClassId;
            foreach (var dto in scheduleDtos)
            {
                if (!TimeSpan.TryParse(dto.SchedStartTime, out var startTime) ||
                    !TimeSpan.TryParse(dto.SchedEndTime, out var endTime))
                {
                    continue; // Skip validation for invalid times (will be caught later)
                }

                // Check faculty conflict first
                var (hasFacultyConflict, conflictingCourse, facultyName) = await _classScheduleRepository
                    .CheckFacultyScheduleConflictAsync(classId, dto.SchedDayOfWeek, startTime, endTime);

                if (hasFacultyConflict)
                {
                    _logger.LogWarning($"Faculty conflict detected for {dto.SchedDayOfWeek} at {dto.SchedStartTime}-{dto.SchedEndTime}");
                    return Json(new
                    {
                        success = false,
                        conflictType = "faculty",
                        message = $"❌ Faculty Schedule Conflict Detected!\n\n" +
                                 $"{facultyName} is already teaching '{conflictingCourse}' on {dto.SchedDayOfWeek} " +
                                 $"during this time slot ({dto.SchedStartTime} - {dto.SchedEndTime}).\n\n" +
                                 $"Please choose a different time or assign a different faculty member.",
                        facultyName = facultyName
                    });
                }

                // ✅ NEW: Check room conflict
                var (hasRoomConflict, roomConflictCourse, roomConflictFaculty, roomCode) = await _classScheduleRepository
                    .CheckRoomScheduleConflictAsync(classId, dto.SchedDayOfWeek, startTime, endTime);

                if (hasRoomConflict)
                {
                    _logger.LogWarning($"Room conflict detected for {dto.SchedDayOfWeek} at {dto.SchedStartTime}-{dto.SchedEndTime} in room {roomCode}");
                    return Json(new
                    {
                        success = false,
                        conflictType = "room",
                        message = $"❌ Room Schedule Conflict Detected!\n\n" +
                                 $"Room '{roomCode}' is already booked for '{roomConflictCourse} - {roomConflictFaculty}' on {dto.SchedDayOfWeek} " +
                                 $"during this time slot ({dto.SchedStartTime} - {dto.SchedEndTime}).\n\n" +
                                 $"Please choose a different time or assign a different room to this class.",
                        roomCode = roomCode,
                        conflictingClass = $"{roomConflictCourse} - {roomConflictFaculty}"
                    });
                }
            }

            // ✅ STEP 2: DELETE all existing schedules for this class (if editing)
            // This prevents conflicts with existing records
            var existingSchedules = await _classScheduleRepository.GetByClassIdAsync(classId);
            
            if (existingSchedules.Any())
            {
                _logger.LogInformation($"Deleting {existingSchedules.Count()} existing schedules for ClassId={classId}");
                foreach (var existingSched in existingSchedules)
                {
                    await _classScheduleRepository.DeleteAsync(existingSched.SchedId);
                }
            }

            // ✅ STEP 3: INSERT new schedules
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
                    _logger.LogInformation($"✅ Successfully created schedule for ClassId={dto.ClassId}");
                }
                else
                {
                    failCount++;
                    _logger.LogWarning($"❌ Failed to create schedule for ClassId={dto.ClassId}");
                }
            }

            if (successCount > 0)
            {
                var cls = await _classRepository.GetByIdAsync(classId);
                var course = cls != null && cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
                
                await _auditService.LogActionAsync(
                    "Created Multiple Class Schedules", 
                    $"Created {successCount} class schedule(s) for {course?.CourseName ?? "Unknown"}");
                    
                return Json(new { 
                    success = true, 
                    message = $"✅ Successfully saved {successCount} schedule(s)" + 
                             (failCount > 0 ? $". {failCount} failed." : "")
                });
            }
            
            return Json(new { success = false, message = "❌ Failed to create schedules" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple schedules: {Message}", ex.Message);
            return Json(new { success = false, message = $"❌ An error occurred: {ex.Message}" });
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
    public async Task<IActionResult> Update([FromBody] ClassScheduleUpdateDto? dto)
    {
        try
        {
            if (dto == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            if (!TimeSpan.TryParse(dto.SchedStartTime, out var startTime))
                return Json(new { success = false, message = "Invalid start time format" });

            if (!TimeSpan.TryParse(dto.SchedEndTime, out var endTime))
                return Json(new { success = false, message = "Invalid end time format" });

            // ✅ Check for faculty schedule conflicts (exclude current schedule ID)
            var (hasFacultyConflict, conflictingCourse, facultyName) = await _classScheduleRepository
                .CheckFacultyScheduleConflictAsync(dto.ClassId, dto.SchedDayOfWeek, startTime, endTime, dto.SchedId);

            if (hasFacultyConflict)
            {
                _logger.LogWarning($"Faculty conflict detected when updating schedule {dto.SchedId}");
                return Json(new
                {
                    success = false,
                    conflictType = "faculty",
                    message = $"❌ Faculty Schedule Conflict Detected!\n\n" +
                             $"{facultyName} is already teaching '{conflictingCourse}' on {dto.SchedDayOfWeek} " +
                             $"during this time slot ({dto.SchedStartTime} - {dto.SchedEndTime}).\n\n" +
                             $"Please choose a different time or assign a different faculty member.",
                    facultyName = facultyName
                });
            }

            // ✅ NEW: Check for room schedule conflicts (exclude current schedule ID)
            var (hasRoomConflict, roomConflictCourse, roomConflictFaculty, roomCode) = await _classScheduleRepository
                .CheckRoomScheduleConflictAsync(dto.ClassId, dto.SchedDayOfWeek, startTime, endTime, dto.SchedId);

            if (hasRoomConflict)
            {
                _logger.LogWarning($"Room conflict detected when updating schedule {dto.SchedId} in room {roomCode}");
                return Json(new
                {
                    success = false,
                    conflictType = "room",
                    message = $"❌ Room Schedule Conflict Detected!\n\n" +
                             $"Room '{roomCode}' is already booked for '{roomConflictCourse} - {roomConflictFaculty}' on {dto.SchedDayOfWeek} " +
                             $"during this time slot ({dto.SchedStartTime} - {dto.SchedEndTime}).\n\n" +
                             $"Please choose a different time or assign a different room to this class.",
                    roomCode = roomCode,
                    conflictingClass = $"{roomConflictCourse} - {roomConflictFaculty}"
                });
            }

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

    [HttpDelete]
    public async Task<IActionResult> DeleteByClass(int id)
    {
        try
        {
            var cls = await _classRepository.GetByIdAsync(id);
            var course = cls != null && cls.CourseId.HasValue ? await _courseRepository.GetByIdAsync(cls.CourseId.Value) : null;
            
            // Get all schedules for this class
            var schedules = await _classScheduleRepository.GetByClassIdAsync(id);
            var scheduleCount = schedules.Count();
            
            if (scheduleCount == 0)
            {
                return Json(new { success = false, message = "No schedules found for this class" });
            }
            
            // Delete all schedules for this class
            int deletedCount = 0;
            foreach (var schedule in schedules)
            {
                var result = await _classScheduleRepository.DeleteAsync(schedule.SchedId);
                if (result) deletedCount++;
            }
            
            if (deletedCount > 0)
            {
                await _auditService.LogActionAsync(
                    "Deleted All Class Schedules", 
                    $"Deleted {deletedCount} schedule(s) for {course?.CourseName ?? "Unknown"}");
                return Json(new { success = true, message = $"Successfully deleted {deletedCount} schedule(s)" });
            }
            
            return Json(new { success = false, message = "Failed to delete schedules" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting class schedules");
            return Json(new { success = false, message = "An error occurred while deleting schedules" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
