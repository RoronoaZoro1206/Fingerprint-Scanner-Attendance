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
    private readonly IDailyAttendanceLogRepository _dailyAttendanceLogRepository;
    private readonly AdminAuditService _auditService;

    public AdminClassScheduleController(
        ILogger<AdminClassScheduleController> logger,
        IClassScheduleRepository classScheduleRepository,
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IDailyAttendanceLogRepository dailyAttendanceLogRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _classScheduleRepository = classScheduleRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _dailyAttendanceLogRepository = dailyAttendanceLogRepository;
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

            // ✅ STEP 2: GET and STORE existing schedules BEFORE deleting (for invalidation)
            var existingSchedules = (await _classScheduleRepository.GetByClassIdAsync(classId)).ToList();
            int totalInvalidatedRecords = 0;
            
            // ✅ NEW STEP 2.5: Invalidate attendance if schedules are changing
            if (existingSchedules.Any())
            {
                _logger.LogInformation($"Found {existingSchedules.Count} existing schedules for ClassId={classId}. Checking for time changes to invalidate attendance.");
                
                // Invalidate attendance for each existing schedule being replaced
                foreach (var existingSched in existingSchedules)
                {
                    // Find if there's a matching new schedule for the same day
                    var newSchedForSameDay = scheduleDtos.FirstOrDefault(s => 
                        s.SchedDayOfWeek.Equals(existingSched.SchedDayOfWeek, StringComparison.OrdinalIgnoreCase));
                    
                    // Check if times are different (or schedule for this day is being removed)
                    bool needsInvalidation = false;
                    TimeSpan newStartTime = TimeSpan.Zero;
                    TimeSpan newEndTime = TimeSpan.Zero;
                    
                    if (newSchedForSameDay == null)
                    {
                        // Schedule for this day is being removed - invalidate
                        needsInvalidation = true;
                        _logger.LogInformation($"Schedule for {existingSched.SchedDayOfWeek} is being removed. Invalidating attendance.");
                    }
                    else if (TimeSpan.TryParse(newSchedForSameDay.SchedStartTime, out newStartTime) &&
                             TimeSpan.TryParse(newSchedForSameDay.SchedEndTime, out newEndTime))
                    {
                        // Check if times are different
                        if (existingSched.SchedStartTime != newStartTime || existingSched.SchedEndTime != newEndTime)
                        {
                            needsInvalidation = true;
                            _logger.LogInformation($"Schedule for {existingSched.SchedDayOfWeek} times changed: " +
                                $"{existingSched.SchedStartTime:hh\\:mm}-{existingSched.SchedEndTime:hh\\:mm} → " +
                                $"{newStartTime:hh\\:mm}-{newEndTime:hh\\:mm}. Invalidating attendance.");
                        }
                    }
                    
                    if (needsInvalidation)
                    {
                        // Use the new times if available, otherwise use old times (for removed schedules)
                        var (dailyCount, attendCount, details) = await _dailyAttendanceLogRepository
                            .InvalidateAttendanceOnScheduleChangeAsync(
                                classId,
                                existingSched.SchedId,
                                existingSched.SchedStartTime,
                                existingSched.SchedEndTime,
                                newStartTime != TimeSpan.Zero ? newStartTime : existingSched.SchedStartTime,
                                newEndTime != TimeSpan.Zero ? newEndTime : existingSched.SchedEndTime,
                                existingSched.SchedDayOfWeek);
                        
                        totalInvalidatedRecords += dailyCount + attendCount;
                        _logger.LogInformation($"✅ {details}");
                    }
                }
                
                // Now delete all existing schedules
                _logger.LogInformation($"Deleting {existingSchedules.Count} existing schedules for ClassId={classId}");
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
                
                // Build audit message with invalidation info
                var auditMessage = $"Created {successCount} class schedule(s) for {course?.CourseName ?? "Unknown"}";
                if (totalInvalidatedRecords > 0)
                {
                    auditMessage += $" | Invalidated {totalInvalidatedRecords} attendance records to allow re-attendance";
                }
                
                await _auditService.LogActionAsync("Created Multiple Class Schedules", auditMessage);
                
                // Build success message
                var successMessage = $"✅ Successfully saved {successCount} schedule(s)";
                if (failCount > 0)
                {
                    successMessage += $". {failCount} failed.";
                }
                if (totalInvalidatedRecords > 0)
                {
                    successMessage += $"\n\n🔄 {totalInvalidatedRecords} student attendance record(s) were reset. " +
                                     "Students can now time in again for the new schedule.";
                }
                    
                return Json(new { 
                    success = true, 
                    message = successMessage,
                    invalidatedRecords = totalInvalidatedRecords
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

            // ✅ STEP 1: Get the EXISTING schedule to compare times
            var existingSchedule = await _classScheduleRepository.GetByIdAsync(dto.SchedId);
            if (existingSchedule == null)
            {
                return Json(new { success = false, message = "Schedule not found" });
            }

            // ✅ STEP 2: Check if time has changed (this triggers attendance invalidation)
            bool timeChanged = existingSchedule.SchedStartTime != startTime || 
                              existingSchedule.SchedEndTime != endTime;
            bool dayChanged = !existingSchedule.SchedDayOfWeek.Equals(dto.SchedDayOfWeek, StringComparison.OrdinalIgnoreCase);

            // Check for faculty schedule conflicts (exclude current schedule ID)
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

            // Check for room schedule conflicts (exclude current schedule ID)
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

            // ✅ STEP 3: If time or day changed, invalidate today's attendance records
            int invalidatedDailyCount = 0;
            int invalidatedAttendanceCount = 0;
            
            if (timeChanged || dayChanged)
            {
                _logger.LogInformation($"Schedule time/day changed for SchedId {dto.SchedId}. Invalidating today's attendance records.");
                
                var (dailyCount, attendCount, details) = await _dailyAttendanceLogRepository
                    .InvalidateAttendanceOnScheduleChangeAsync(
                        dto.ClassId,
                        dto.SchedId,
                        existingSchedule.SchedStartTime,
                        existingSchedule.SchedEndTime,
                        startTime,
                        endTime,
                        dto.SchedDayOfWeek);
                
                invalidatedDailyCount = dailyCount;
                invalidatedAttendanceCount = attendCount;
                
                _logger.LogInformation($"✅ {details}");
            }

            // ✅ STEP 4: Update the schedule
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
                
                // Build audit message with invalidation info if applicable
                var auditMessage = $"Updated schedule for {course?.CourseName ?? "Unknown"} on {dto.SchedDayOfWeek} ({dto.SchedStartTime}-{dto.SchedEndTime})";
                if (invalidatedDailyCount > 0 || invalidatedAttendanceCount > 0)
                {
                    auditMessage += $" | Invalidated {invalidatedDailyCount} daily + {invalidatedAttendanceCount} attendance records to allow re-attendance";
                }
                
                await _auditService.LogActionAsync("Updated Class Schedule", auditMessage);
                
                // Build success message
                var successMessage = "Schedule updated successfully";
                if (invalidatedDailyCount > 0 || invalidatedAttendanceCount > 0)
                {
                    successMessage += $"\n\n🔄 {invalidatedDailyCount + invalidatedAttendanceCount} student attendance record(s) were reset. " +
                                     "Students can now time in again for the new schedule.";
                }
                
                return Json(new { 
                    success = true, 
                    message = successMessage,
                    invalidatedRecords = invalidatedDailyCount + invalidatedAttendanceCount
                });
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
