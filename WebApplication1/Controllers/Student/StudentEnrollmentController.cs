using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services; // ? ADDED: For UniversalAuditService
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Student;

[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentEnrollmentController : Controller
{
    private readonly ILogger<StudentEnrollmentController> _logger;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassRepository _classRepository; // ? ADDED: For getting class details
    private readonly ICourseRepository _courseRepository; // ? ADDED: For getting course details
    private readonly IEnrollmentBlockRepository _enrollmentBlockRepository; // ✅ NEW: For blocked classes
    private readonly UniversalAuditService _auditService; // ? ADDED: Universal audit service
    
    // ✅ CRITICAL FIX: Per-student enrollment locks to prevent race conditions
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _studentEnrollmentLocks = new();

    public StudentEnrollmentController(
        ILogger<StudentEnrollmentController> logger,
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        IClassRepository classRepository, // ? ADDED: Inject class repository
        ICourseRepository courseRepository, // ? ADDED: Inject course repository
        IEnrollmentBlockRepository enrollmentBlockRepository, // ✅ NEW: Inject enrollment block repository
        UniversalAuditService auditService) // ? ADDED: Inject universal audit service
    {
        _logger = logger;
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _classRepository = classRepository; // ? ADDED: Store class repository
        _courseRepository = courseRepository; // ? ADDED: Store course repository
        _enrollmentBlockRepository = enrollmentBlockRepository; // ✅ NEW: Store enrollment block repository
        _auditService = auditService; // ? ADDED: Store audit service
    }

    public IActionResult Enrollment()
    {
        return View("~/Views/Student/StudentEnrollment.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetStudentInfo()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetStudentInfo - UserId from session: {userId}");
            
            if (!userId.HasValue || userId.Value == 0)
            {
                _logger.LogWarning("GetStudentInfo - User not authenticated");
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Single optimized query instead of 3 separate queries
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogWarning($"GetStudentInfo - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found. Please complete your application first." });
            }

            if (!studentInfo.ProgramId.HasValue)
            {
                _logger.LogWarning($"GetStudentInfo - Student {studentInfo.StudId} has no program assigned");
                return Json(new { success = false, message = "Please complete your student application to enroll in classes." });
            }

            _logger.LogInformation($"GetStudentInfo - Student: {studentInfo.StudId}, Program: {studentInfo.ProgramName}");

            return Json(new
            {
                success = true,
                data = new
                {
                    studId = studentInfo.StudId,
                    studNumber = studentInfo.StudNumber,
                    fullName = studentInfo.FullName,
                    programId = studentInfo.ProgramId,
                    programName = studentInfo.ProgramName,
                    yearLevel = studentInfo.YearLevel,
                    hasProfile = studentInfo.HasProfile
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting student info: {ex.Message}");
            return Json(new { success = false, message = $"Failed to load student information: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentEnrollments()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetCurrentEnrollments - UserId from session: {userId}");
            
            if (!userId.HasValue)
            {
                _logger.LogWarning("GetCurrentEnrollments - No userId in session");
                return Json(new { success = false, data = new List<object>(), message = "User not authenticated" });
            }
            
            // Get student info with single query
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogWarning($"GetCurrentEnrollments - No student found for userId: {userId.Value}");
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found" });
            }

            _logger.LogInformation($"GetCurrentEnrollments - Student: {studentInfo.StudId}");

            // Single optimized query with all JOINs instead of 7 separate queries
            var enrollments = await _enrollmentRepository.GetStudentEnrollmentsWithDetailsAsync(studentInfo.StudId);

            _logger.LogInformation($"GetCurrentEnrollments - Returning {enrollments.Count()} enrollments");
            return Json(new { success = true, data = enrollments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting current enrollments: {ex.Message}");
            return Json(new { success = false, data = new List<object>(), message = $"Error: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableClasses()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetAvailableClasses - UserId from session: {userId}");
            
            if (!userId.HasValue)
            {
                _logger.LogWarning("GetAvailableClasses - No userId in session");
                return Json(new { success = false, data = new List<object>(), message = "User not authenticated" });
            }
            
            // Get student info with single query
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogWarning($"GetAvailableClasses - No student found for userId: {userId.Value}");
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found. Please complete your profile first." });
            }

            if (!studentInfo.ProgramId.HasValue)
            {
                _logger.LogWarning($"GetAvailableClasses - Student {studentInfo.StudId} has no program assigned");
                return Json(new { 
                    success = false, 
                    data = new List<object>(), 
                    message = "Please complete your student profile with a program selection before enrolling in classes." 
                });
            }

            _logger.LogInformation($"GetAvailableClasses - Student: {studentInfo.StudId}, Program: {studentInfo.ProgramId}, ProgramName: {studentInfo.ProgramName}");

            // Single optimized query with all JOINs and filtering
            var availableClasses = await _enrollmentRepository.GetAvailableClassesForStudentAsync(
                studentInfo.StudId, 
                studentInfo.ProgramId.Value);

            // ✅ NEW: Filter out blocked classes
            var blockedClassIds = await _enrollmentBlockRepository.GetBlockedClassIdsForStudentAsync(studentInfo.StudId);
            var blockedSet = new HashSet<int>(blockedClassIds);
            
            var classList = availableClasses.Where(c => !blockedSet.Contains(c.ClassId)).ToList();
            _logger.LogInformation($"GetAvailableClasses - Found {classList.Count} available classes for program {studentInfo.ProgramName} (filtered {blockedSet.Count} blocked)");
            
            return Json(new { success = true, data = classList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting available classes: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return Json(new { success = false, data = new List<object>(), message = $"Error loading classes: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest? request)
    {
        try
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) 
            {
                _logger.LogWarning("Enroll - No userId in session");
                return Json(new { success = false, message = "User not authenticated" });
            }
            
            // Get student info with single query
            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);

            if (studentInfo == null)
            {
                _logger.LogError($"Enroll - No student found for userId: {userId.Value}");
                return Json(new { success = false, message = "Student profile not found. Please complete your profile first." });
            }

            if (!studentInfo.ProgramId.HasValue)
            {
                _logger.LogError($"Enroll - Student {studentInfo.StudId} has no program assigned");
                return Json(new { success = false, message = "Please complete your student profile with a program before enrolling." });
            }

            // ✅ CRITICAL FIX: Acquire per-student lock to prevent race conditions
            // This ensures only one enrollment request per student can be processed at a time
            var studentLock = _studentEnrollmentLocks.GetOrAdd(studentInfo.StudId, _ => new SemaphoreSlim(1, 1));
            
            // Try to acquire lock with timeout (5 seconds)
            if (!await studentLock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning($"Enroll - Student {studentInfo.StudId} has concurrent enrollment request, blocking");
                return Json(new { success = false, message = "Please wait for your previous enrollment request to complete." });
            }

            try
            {
                _logger.LogInformation($"Enroll - Acquired lock for student {studentInfo.StudId}, processing enrollment for class {request.ClassId}");

                // ✅ CRITICAL: Re-check enrollment AFTER acquiring lock to prevent race conditions
                // This ensures we have the latest data after waiting for any concurrent requests
                var existingEnrollments = await _enrollmentRepository.GetByStudentIdAsync(studentInfo.StudId);
                if (existingEnrollments.Any(e => e.ClassId == request.ClassId))
                {
                    _logger.LogWarning($"Enroll - Student {studentInfo.StudId} already enrolled in class {request.ClassId}");
                    return Json(new { success = false, message = "You are already enrolled in this class" });
                }

                // ✅ NEW: Check if student is blocked from this class
                var isBlocked = await _enrollmentBlockRepository.IsStudentBlockedAsync(studentInfo.StudId, request.ClassId);
                if (isBlocked)
                {
                    _logger.LogWarning($"Enroll - Student {studentInfo.StudId} is blocked from class {request.ClassId}");
                    return Json(new { 
                        success = false, 
                        message = "You have been blocked from enrolling in this class. Please contact your faculty or academic advisor.",
                        isBlocked = true
                    });
                }

                // Get new class schedule details
                var newClass = await _classRepository.GetByIdAsync(request.ClassId);
                if (newClass?.CourseId == null)
                {
                    _logger.LogError($"Enroll - Class {request.ClassId} not found or has no course");
                    return Json(new { success = false, message = "Invalid class selection" });
                }

                var newCourse = await _courseRepository.GetByIdAsync(newClass.CourseId.Value);
                var newSchedules = await _classRepository.GetClassSchedulesAsync(request.ClassId);

                // ✅ CRITICAL CHANGE: BLOCK TBA schedules
                if (!newSchedules.Any())
                {
                    _logger.LogWarning($"Enroll - Class {request.ClassId} has TBA schedule, blocking enrollment");
                    
                    return Json(new 
                    { 
                        success = false, 
                        message = $"⚠️ TBA Schedule Not Allowed\n\n" +
                                 $"📚 Cannot enroll in '{newCourse?.CourseName ?? "this class"}' ({newCourse?.CourseCode ?? "N/A"}) " +
                                 $"because it has a TBA (To Be Announced) schedule.\n\n" +
                                 $"🔄 This restriction prevents future schedule conflicts. If an admin later assigns a specific time to this class, " +
                                 $"it may conflict with your existing enrolled classes.\n\n" +
                                 $"💡 Solution: Please wait for a class schedule to be assigned, or contact your academic advisor to request schedule information.",
                        isTbaError = true  // ✅ NEW: Flag to trigger specific UI handling
                    });
                }

                // ✅ Schedule Conflict Detection - FRESH QUERY AFTER LOCK
                _logger.LogInformation($"Enroll - Checking schedule conflicts for student {studentInfo.StudId} enrolling in class {request.ClassId}");
                
                // Get student's current enrollments with schedules (FRESH query after lock acquired)
                var studentSchedules = (await _enrollmentRepository.GetStudentEnrollmentsWithSchedulesAsync(studentInfo.StudId)).ToList();
                
                _logger.LogInformation($"Enroll - Student has {studentSchedules.Count} existing schedule entries after lock");
                foreach (var s in studentSchedules)
                {
                    _logger.LogInformation($"  Existing: ClassId={s.ClassId}, {s.CourseCode} on '{s.DayOfWeek}' from '{s.StartTime}' to '{s.EndTime}'");
                }

                var newSchedulesList = newSchedules.ToList();
                _logger.LogInformation($"Enroll - New class {request.ClassId} has {newSchedulesList.Count} schedule entries");
                foreach (var s in newSchedulesList)
                {
                    _logger.LogInformation($"  New: SchedId={s.SchedId}, '{s.DayOfWeek}' from '{s.StartTime}' to '{s.EndTime}'");
                }

                // Check each new schedule against existing schedules
                foreach (var newSched in newSchedulesList)
                {
                    // Parse new schedule times (format: "HH:mm:ss" from database)
                    if (!TimeSpan.TryParse(newSched.StartTime, out var newStart) ||
                        !TimeSpan.TryParse(newSched.EndTime, out var newEnd))
                    {
                        _logger.LogWarning($"Enroll - Could not parse new class schedule times: '{newSched.StartTime}' - '{newSched.EndTime}'");
                        continue;
                    }

                    var newDay = newSched.DayOfWeek?.Trim();
                    if (string.IsNullOrEmpty(newDay))
                    {
                        _logger.LogWarning("Enroll - New class schedule has empty day of week");
                        continue;
                    }

                    // ✅ CRITICAL FIX: Handle midnight crossing (e.g., 23:00 - 00:00)
                    // If end time is less than start time, it means the class crosses midnight
                    var newEndAdjusted = newEnd;
                    if (newEnd <= newStart)
                    {
                        // Add 24 hours to end time to handle midnight crossing
                        newEndAdjusted = newEnd.Add(TimeSpan.FromHours(24));
                        _logger.LogInformation($"Enroll - New class crosses midnight, adjusted end: {newEnd} -> {newEndAdjusted}");
                    }

                    // Check against all existing schedules
                    foreach (var existingSched in studentSchedules)
                    {
                        var existingDay = existingSched.DayOfWeek?.Trim();
                        
                        // Skip if days don't match (case-insensitive comparison)
                        if (string.IsNullOrEmpty(existingDay) || 
                            !existingDay.Equals(newDay, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Parse existing schedule times (format: "HH24:MI" from database)
                        if (!TimeSpan.TryParse(existingSched.StartTime, out var existingStart) ||
                            !TimeSpan.TryParse(existingSched.EndTime, out var existingEnd))
                        {
                            _logger.LogWarning($"Enroll - Could not parse existing schedule times: '{existingSched.StartTime}' - '{existingSched.EndTime}'");
                            continue;
                        }

                        // ✅ CRITICAL FIX: Handle midnight crossing for existing schedule too
                        var existingEndAdjusted = existingEnd;
                        if (existingEnd <= existingStart)
                        {
                            existingEndAdjusted = existingEnd.Add(TimeSpan.FromHours(24));
                            _logger.LogInformation($"Enroll - Existing class crosses midnight, adjusted end: {existingEnd} -> {existingEndAdjusted}");
                        }

                        // ✅ CRITICAL: Time Overlap Detection Logic (with midnight-adjusted times)
                        // Two time ranges overlap if: (NewStart < ExistingEnd) AND (NewEnd > ExistingStart)
                        bool hasTimeOverlap = (newStart < existingEndAdjusted) && (newEndAdjusted > existingStart);
                        
                        _logger.LogInformation($"Enroll - Overlap check: new={newDay} {newStart}-{newEndAdjusted} vs existing={existingDay} {existingStart}-{existingEndAdjusted} = {hasTimeOverlap}");

                        if (hasTimeOverlap)
                        {
                            // Format times for user-friendly display (12-hour format with AM/PM)
                            var newStartFormatted = DateTime.Today.Add(newStart).ToString("h:mm tt");
                            var newEndFormatted = DateTime.Today.Add(newEnd).ToString("h:mm tt");
                            var existingStartFormatted = DateTime.Today.Add(existingStart).ToString("h:mm tt");
                            var existingEndFormatted = DateTime.Today.Add(existingEnd).ToString("h:mm tt");

                            var conflictMessage = $"⚠️ Schedule Conflict Detected!\n\n" +
                                                $"📚 You are already enrolled in '{existingSched.CourseName}' ({existingSched.CourseCode}) " +
                                                $"on {existingDay} from {existingStartFormatted} to {existingEndFormatted}.\n\n" +
                                                $"The new class '{newCourse?.CourseName}' is scheduled on {newDay} " +
                                                $"from {newStartFormatted} to {newEndFormatted}, which overlaps with your existing schedule.\n\n" +
                                                $"💡 Solution: Please choose a different class or contact your academic advisor.";

                            _logger.LogWarning($"Enroll - SCHEDULE CONFLICT DETECTED! Blocking enrollment.");
                            
                            return Json(new 
                            { 
                                success = false, 
                                message = conflictMessage,
                                isConflict = true
                            });
                        }
                    }
                }

                // No conflicts found, proceed with enrollment
                var enrollment = new Enrollment
                {
                    StudId = studentInfo.StudId,
                    ClassId = request.ClassId,
                    EnrollAt = DateTime.Now
                };

                _logger.LogInformation($"Enroll - Attempting to enroll student {studentInfo.StudId} in class {request.ClassId}");
                var result = await _enrollmentRepository.AddAsync(enrollment);

                if (result)
                {
                    // Log audit for student enrollment
                    var classDetail = await _classRepository.GetByIdAsync(request.ClassId);
                    if (classDetail?.CourseId != null)
                    {
                        var course = await _courseRepository.GetByIdAsync(classDetail.CourseId.Value);
                        if (course != null)
                        {
                            await _auditService.LogEnrollmentActionAsync(
                                course.CourseCode,
                                course.CourseName,
                                isEnrollment: true
                            );
                            
                            _logger.LogInformation($"Enroll - Successfully enrolled student {studentInfo.StudId} in {course.CourseCode}");
                        }
                    }

                    return Json(new { success = true, message = "Successfully enrolled in class" });
                }

                _logger.LogError($"Enroll - Failed to add enrollment for student {studentInfo.StudId} in class {request.ClassId}");
                return Json(new { success = false, message = "Failed to enroll in class. Please try again." });
            }
            finally
            {
                // ✅ CRITICAL: Always release the lock
                studentLock.Release();
                _logger.LogInformation($"Enroll - Released lock for student {studentInfo.StudId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enrolling in class: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return Json(new { success = false, message = "An error occurred while enrolling. Please contact support." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Unenroll([FromBody] UnenrollRequest? request)
    {
        try
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            _logger.LogInformation($"Attempting to unenroll: enrollId={request.EnrollId}");
            
            // ? ADDED: Get enrollment details before deletion for audit log
            var enrollment = await _enrollmentRepository.GetByIdAsync(request.EnrollId);
            string courseCode = "Unknown";
            string courseName = "Unknown";

            if (enrollment?.ClassId != null)
            {
                var classDetail = await _classRepository.GetByIdAsync(enrollment.ClassId);
                if (classDetail?.CourseId != null)
                {
                    var course = await _courseRepository.GetByIdAsync(classDetail.CourseId.Value);
                    if (course != null)
                    {
                        courseCode = course.CourseCode;
                        courseName = course.CourseName;
                    }
                }
            }
            
            var result = await _enrollmentRepository.DeleteAsync(request.EnrollId);

            if (result)
            {
                // ? ADDED: Log audit for student unenrollment
                await _auditService.LogEnrollmentActionAsync(
                    courseCode,
                    courseName,
                    isEnrollment: false
                );

                _logger.LogInformation($"Successfully unenrolled: enrollId={request.EnrollId}");
                return Json(new { success = true, message = "Successfully unenrolled from class" });
            }

            _logger.LogWarning($"Failed to unenroll: enrollId={request.EnrollId}");
            return Json(new { success = false, message = "Failed to unenroll from class" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error unenrolling from class: {ex.Message}");
            return Json(new { success = false, message = "An error occurred while unenrolling" });
        }
    }

    public class EnrollRequest
    {
        public int ClassId { get; set; }
    }

    public class UnenrollRequest
    {
        public int EnrollId { get; set; }
    }

    /// <summary>
    /// ✅ NEW: Get classes that the student has been blocked from (force-unenrolled by faculty)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBlockedClasses()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, data = new List<object>(), message = "User not authenticated" });
            }

            var studentInfo = await _studentRepository.GetStudentInfoByUserIdAsync(userId.Value);
            if (studentInfo == null)
            {
                return Json(new { success = false, data = new List<object>(), message = "Student profile not found" });
            }

            var blockedClasses = await _enrollmentBlockRepository.GetStudentBlockedClassesAsync(studentInfo.StudId);
            
            _logger.LogInformation($"GetBlockedClasses - Found {blockedClasses.Count()} blocked classes for student {studentInfo.StudId}");
            
            // Log details for debugging
            foreach (var blocked in blockedClasses)
            {
                _logger.LogInformation($"GetBlockedClasses - BlockId={blocked.BlockId}, ClassId={blocked.ClassId}, CourseCode={blocked.CourseCode}, CourseName={blocked.CourseName}");
            }
            
            return Json(new { success = true, data = blockedClasses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocked classes");
            return Json(new { success = false, data = new List<object>(), message = "Error loading blocked classes" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
