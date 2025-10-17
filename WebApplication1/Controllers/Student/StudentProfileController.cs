using System.Diagnostics;
using System.Security.Claims;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
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

    public StudentProfileController(
        ILogger<StudentProfileController> logger,
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IProgramRepository programRepository,
        ISectionRepository sectionRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _programRepository = programRepository;
        _sectionRepository = sectionRepository;
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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value == 0)
                return Json(new { success = false, message = "User not authenticated" });

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId);

            var programs = await _programRepository.GetAllAsync();
            var sections = await _sectionRepository.GetAllAsync();

            var programName = student?.ProgramId.HasValue == true
                ? programs.FirstOrDefault(p => p.ProgramId == student.ProgramId)?.ProgramName
                : null;

            var sectionName = student?.SectionId.HasValue == true
                ? sections.FirstOrDefault(s => s.SectionId == student.SectionId)?.SectionName
                : null;

            return Json(new
            {
                success = true,
                data = new
                {
                    userId = user.UserId,
                    userProfileImage = user.UserProfileImage,
                    userFirstName = user.UserFirstName,
                    userMiddleName = user.UserMiddleName,
                    userLastName = user.UserLastName,
                    userEmail = user.UserEmail,
                    userPhone = user.UserPhone,
                    userGender = user.UserGender,
                    userBirthdate = user.UserBirthdate?.ToString("yyyy-MM-dd"),
                    userAddress = user.UserAddress,
                    studId = student?.StudId,
                    studNumber = student?.StudNumber,
                    programId = student?.ProgramId,
                    programName = programName,
                    studYearLevel = student?.StudYearLevel,
                    sectionId = student?.SectionId,
                    sectionName = sectionName,
                    profileSubmitted = student?.ProfileSubmitted ?? false,
                    profileSubmittedAt = student?.ProfileSubmittedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                    hasStudentProfile = student != null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile");
            return Json(new { success = false, message = "Failed to load profile" });
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
            if (!userId.HasValue || userId.Value == 0)
                return Json(new { success = false, message = "User not authenticated" });

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

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
                return Json(new { success = false, message = "Failed to update user information" });

            // Update session with new name
            HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");

            // Update or create student profile
            var student = (await _studentRepository.GetAllAsync())
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student != null)
            {
                // Check if profile is already submitted and trying to change PROGRAM
                if (student.ProfileSubmitted)
                {
                    // Only lock the PROGRAM - allow year level and section updates
                    bool programChanged = student.ProgramId != request.ProgramId;

                    if (programChanged)
                    {
                        return Json(new 
                        { 
                            success = false, 
                            message = "Your program is locked after first submission. Please contact administration if you need to change your program." 
                        });
                    }
                    
                    // Year level and section can be updated freely
                }
                else
                {
                    // First time submitting with complete academic info - lock the profile
                    if (request.ProgramId.HasValue)
                    {
                        student.ProfileSubmitted = true;
                        student.ProfileSubmittedAt = DateTime.Now;
                    }
                }

                // Update student profile (year level and section can always be updated)
                student.ProgramId = request.ProgramId;
                student.StudYearLevel = request.StudYearLevel;
                student.SectionId = request.SectionId;

                var studentResult = await _studentRepository.UpdateAsync(student);
                if (!studentResult)
                    return Json(new { success = false, message = "Failed to update student profile" });
            }
            else
            {
                // Student profile doesn't exist - this shouldn't happen for registered students
                // but we'll handle it gracefully
                return Json(new { success = false, message = "Student profile not found. Please contact administration." });
            }

            string message = student.ProfileSubmitted 
                ? "Profile updated successfully" 
                : "Profile updated successfully. Your program is now locked.";

            return Json(new { success = true, message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
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
