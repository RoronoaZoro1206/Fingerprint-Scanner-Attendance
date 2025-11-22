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

            // ✅ FIX: Ensure proper profile image path
            var profileImagePath = profile.UserProfileImage;
            if (string.IsNullOrEmpty(profileImagePath) || profileImagePath == "default_profile.png")
            {
                // Use default from templates folder
                profileImagePath = "templates/default_profile.png";
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    userId = profile.UserId,
                    userProfileImage = profileImagePath, // ✅ Returns proper path
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
            
            _logger.LogInformation($"UpdateProfile called - UserId from session: {userId}");
  
            if (!userId.HasValue || userId.Value == 0)
            {
                _logger.LogError("UpdateProfile - No userId in session or userId is 0");
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Verify user exists in database
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                _logger.LogError($"UpdateProfile - User not found for userId: {userId.Value}");
                return Json(new { success = false, message = "User not found" });
            }

            _logger.LogInformation($"UpdateProfile - Found user: {user.UserEmail} (UserID: {user.UserId})");

            // Store old values for logging
            var oldName = $"{user.UserFirstName} {user.UserLastName}";

            // ✅ SECURITY FIX: Students can ONLY update personal information
            // Academic information (Program, Year Level, Section) is READ-ONLY
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

            // ✅ SECURITY FIX: DO NOT update student academic information
            // Students cannot modify Program, Year Level, or Section
            // These fields are managed ONLY by administrators via Admin/Students page

            // Log personal information updates only
            var changes = new List<string>();
            if (oldName != $"{user.UserFirstName} {user.UserLastName}")
                changes.Add($"Name: {oldName} → {user.UserFirstName} {user.UserLastName}");

            if (changes.Any())
            {
                _logger.LogInformation($"Profile changes for userId {userId.Value}: {string.Join(", ", changes)}");
                
                await _auditService.LogActionAsync(
                    "Student Profile Updated",
                    $"Student userId {userId.Value} updated personal information: {string.Join(", ", changes)}"
                );
            }

            _logger.LogInformation($"UpdateProfile - Completed successfully for userId: {userId.Value}");
            return Json(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"UpdateProfile - Exception occurred for userId: {HttpContext.Session.GetInt32("UserId")}");
            return Json(new { success = false, message = "An error occurred while updating profile" });
        }
    }

    public class UpdateProfileRequest
    {
        // ✅ SECURITY FIX: Personal information fields - students CAN update these
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string UserLastName { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public string? UserGender { get; set; }
        public DateTime? UserBirthdate { get; set; }
        public string? UserAddress { get; set; }
        
        // ✅ SECURITY FIX: Academic information fields - REMOVED from student profile updates
        // These fields are READ-ONLY for students and can only be modified by administrators
        // Students should not be able to send these values in their profile update requests
        // If they try to send them, they will be ignored by the controller
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
