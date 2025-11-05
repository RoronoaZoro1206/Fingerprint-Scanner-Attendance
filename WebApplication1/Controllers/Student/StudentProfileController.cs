using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services; // ? ADDED: For UniversalAuditService
using Microsoft.AspNetCore.Mvc;
using StudentModel = Fingerprint_Attendance_System.Models.Student;

namespace Fingerprint_Attendance_System.Controllers.Student;

[RoleAuthorize(USER_ROLES.STUDENT)]
public class StudentProfileController : Controller
{
    private readonly ILogger<StudentProfileController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly UniversalAuditService _auditService; // ? ADDED: Universal audit service

    public StudentProfileController(
        ILogger<StudentProfileController> logger,
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IProgramRepository programRepository,
        ISectionRepository sectionRepository,
        UniversalAuditService auditService) // ? ADDED: Inject universal audit service
    {
        _logger = logger;
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _programRepository = programRepository;
        _sectionRepository = sectionRepository;
        _auditService = auditService; // ? ADDED: Store audit service
    }

    public IActionResult Index()
    {
        return View("~/Views/Student/StudentProfile.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            // Enhanced logging for debugging
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetProfile called - UserId from session: {userId}");

            if (!userId.HasValue || userId.Value == 0)
            {
                _logger.LogWarning("GetProfile - No userId in session or userId is 0");
                return Json(new { success = false, message = "User not authenticated. Please log in again." });
            }

            _logger.LogInformation($"GetProfile - Fetching profile for userId: {userId.Value}");

            var profile = await _studentRepository.GetStudentProfileByUserIdAsync(userId.Value);

            if (profile == null)
            {
                _logger.LogWarning($"GetProfile - No profile found for userId: {userId.Value}");
                return Json(new { success = false, message = "Profile not found. Please contact administration." });
            }

            _logger.LogInformation($"GetProfile - Successfully retrieved profile for userId: {userId.Value}, StudId: {profile.StudId}");

            return Json(new
            {
                success = true,
                data = new
                {
                    userId = profile.UserId,
                    userProfileImage = profile.UserProfileImage,
                    userFirstName = profile.UserFirstName,
                    userMiddleName = profile.UserMiddleName,
                    userLastName = profile.UserLastName,
                    userEmail = profile.UserEmail,
                    userPhone = profile.UserPhone,
                    userGender = profile.UserGender,
                    userBirthdate = profile.UserBirthdate,
                    userAddress = profile.UserAddress,
                    studId = profile.StudId,
                    studNumber = profile.StudNumber,
                    programId = profile.ProgramId,
                    programName = profile.ProgramName,
                    studYearLevel = profile.StudYearLevel,
                    sectionId = profile.SectionId,
                    sectionName = profile.SectionName,
                    profileSubmitted = profile.ProfileSubmitted,
                    profileSubmittedAt = profile.ProfileSubmittedAt,
                    hasStudentProfile = profile.HasStudentProfile
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting profile - UserId: {HttpContext.Session.GetInt32("UserId")}, Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            return Json(new { success = false, message = $"Failed to load profile: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPrograms()
    {
        try
        {
            var programs = await _programRepository.GetAllAsync();
            var programList = programs.Select(p => new
            {
                programId = p.ProgramId,
                programName = p.ProgramName,
                programCode = p.ProgramCode
            });

            return Json(new { success = true, data = programList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting programs");
            return Json(new { success = false, message = "Failed to load programs" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSections()
    {
        try
        {
            var sections = await _sectionRepository.GetAllAsync();
            var sectionList = sections.Select(s => new
            {
                sectionId = s.SectionId,
                sectionName = s.SectionName,
                programId = s.ProgramId,
                yearLevel = s.YearLevel
            });

            return Json(new { success = true, data = sectionList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sections");
            return Json(new { success = false, message = "Failed to load sections" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // ? Enhanced logging for debugging
 _logger.LogInformation($"UpdateProfile called - UserId from session: {userId}");
  
         if (!userId.HasValue || userId.Value == 0)
            {
    _logger.LogError("UpdateProfile - No userId in session or userId is 0");
        return Json(new { success = false, message = "User not authenticated" });
   }

    // ? Verify user exists in database
          var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
 {
                _logger.LogError($"UpdateProfile - User not found for userId: {userId.Value}");
          return Json(new { success = false, message = "User not found" });
   }

            _logger.LogInformation($"UpdateProfile - Found user: {user.UserEmail} (UserID: {user.UserId})");

        // Store old values for logging
      var oldName = $"{user.UserFirstName} {user.UserLastName}";

     // Update user information
            user.UserFirstName = request.UserFirstName;
            user.UserMiddleName = request.UserMiddleName;
            user.UserLastName = request.UserLastName;
 user.UserPhone = request.UserPhone;
         user.UserGender = request.UserGender;
     user.UserBirthdate = request.UserBirthdate;
      user.UserAddress = request.UserAddress;
            user.UserUpdatedAt = DateTime.Now;

    var userResult = await _userRepository.UpdateAsync(user);
        if (!userResult)
     {
      _logger.LogError($"UpdateProfile - Failed to update user information for userId: {userId.Value}");
    return Json(new { success = false, message = "Failed to update user information" });
            }

            _logger.LogInformation($"UpdateProfile - Successfully updated user information for userId: {userId.Value}");

     // Update session with new name
            HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");

        // Update or create student profile
     var student = await _studentRepository.GetByUserIdAsync(userId.Value);

            if (student != null)
     {
   _logger.LogInformation($"UpdateProfile - Found student record (StudId: {student.StudId}) for userId: {userId.Value}");

       // Store old values for logging
        var oldProgram = student.ProgramId;
 var oldYearLevel = student.StudYearLevel;
      var oldSection = student.SectionId;

// Check if profile is already submitted and trying to change PROGRAM
  if (student.ProfileSubmitted)
    {
        // Only lock the PROGRAM - allow year level and section updates
          bool programChanged = student.ProgramId != request.ProgramId;

         if (programChanged)
            {
           _logger.LogWarning($"UpdateProfile - Student {student.StudId} attempted to change locked program from {oldProgram} to {request.ProgramId}");
      return Json(new
        {
         success = false,
         message = "Your program is locked after first submission. Please contact administration if you need to change your program."
       });
  }

   // Year level and section can be updated freely
      _logger.LogInformation($"UpdateProfile - Allowing year level/section update for submitted profile (StudId: {student.StudId})");
            }
            else
             {
        // First time submitting with complete academic info - lock the profile
       if (request.ProgramId.HasValue)
  {
         student.ProfileSubmitted = true;
student.ProfileSubmittedAt = DateTime.Now;
   _logger.LogInformation($"UpdateProfile - Locking profile for student {student.StudId} (first submission with program)");

            // ? ADDED: Log audit for student profile submission (first time)
   var program = await _programRepository.GetByIdAsync(request.ProgramId.Value);
      await _auditService.LogProfileSubmissionAsync(
         student.StudId,
                program?.ProgramName ?? "Unknown Program",
                 request.StudYearLevel ?? 1
           );
           }
            }

                // Update student profile (year level and section can always be updated)
         student.ProgramId = request.ProgramId;
    student.StudYearLevel = request.StudYearLevel;
        student.SectionId = request.SectionId;

      _logger.LogInformation($"UpdateProfile - Updating student record for StudId: {student.StudId}, UserId: {student.UserId}");

          var studentResult = await _studentRepository.UpdateAsync(student);
                if (!studentResult)
              {
        _logger.LogError($"UpdateProfile - Failed to update student profile for StudId: {student.StudId}");
      return Json(new { success = false, message = "Failed to update student profile" });
             }

      _logger.LogInformation($"UpdateProfile - Successfully updated student profile for StudId: {student.StudId}");

  // ? ENHANCED: Log ALL profile updates (not just first submission)
    var changes = new List<string>();
              if (oldName != $"{user.UserFirstName} {user.UserLastName}")
 changes.Add($"Name: {oldName} ? {user.UserFirstName} {user.UserLastName}");
          if (oldProgram != student.ProgramId)
       changes.Add($"Program: {oldProgram} ? {student.ProgramId}");
  if (oldYearLevel != student.StudYearLevel)
          changes.Add($"Year Level: {oldYearLevel} ? {student.StudYearLevel}");
             if (oldSection != student.SectionId)
 changes.Add($"Section: {oldSection} ? {student.SectionId}");

      if (changes.Any())
     {
     _logger.LogInformation($"Profile changes for student {student.StudNumber}: {string.Join(", ", changes)}");
                
                // ? ADDED: Log audit for ALL profile updates
                await _auditService.LogActionAsync(
                    "Student Profile Updated",
                    $"Student {student.StudNumber} updated profile: {string.Join(", ", changes)}"
                );
        }
            }
            else
      {
          // Student profile doesn't exist - this shouldn't happen for registered students
        // but we'll handle it gracefully
                _logger.LogError($"UpdateProfile - No student record found for userId: {userId.Value}");
         return Json(new { success = false, message = "Student profile not found. Please contact administration." });
        }

            string message = student?.ProfileSubmitted == true
     ? "Profile updated successfully"
              : "Profile updated successfully. Your program is now locked.";

            _logger.LogInformation($"UpdateProfile - Completed successfully for userId: {userId.Value}");
    return Json(new { success = true, message = message });
 }
        catch (Exception ex)
 {
         _logger.LogError(ex, $"UpdateProfile - Exception occurred for userId: {HttpContext.Session.GetInt32("UserId")}");
            return Json(new { success = false, message = "An error occurred while updating profile" });
        }
    }

    public class UpdateProfileRequest
    {
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string UserLastName { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public string? UserGender { get; set; }
        public DateTime? UserBirthdate { get; set; }
        public string? UserAddress { get; set; }
        public string? StudNumber { get; set; }
        public int? ProgramId { get; set; }
        public int? StudYearLevel { get; set; }
        public int? SectionId { get; set; }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
