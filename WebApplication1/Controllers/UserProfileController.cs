using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers;

[RoleAuthorize(USER_ROLES.ADMIN, USER_ROLES.FACULTY, USER_ROLES.STUDENT)]
public class UserProfileController : Controller
{
    private readonly ILogger<UserProfileController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly AdminAuditService? _auditService; // Optional - only for admins

    public UserProfileController(
        ILogger<UserProfileController> logger,
        IUserRepository userRepository,
      IWebHostEnvironment environment,
        AdminAuditService? auditService = null) // Optional injection
    {
        _logger = logger;
        _userRepository = userRepository;
_environment = environment;
        _auditService = auditService;
    }

    public IActionResult Profile()
    {
        return View("~/Views/Shared/UserProfile.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileData()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"GetProfileData - UserId from session: {userId}");

            if (!userId.HasValue)
            {
                _logger.LogWarning("GetProfileData - No userId in session");
                return Json(new { success = false, message = "User not authenticated" });
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);

            if (user == null)
            {
                _logger.LogWarning($"GetProfileData - No user found for userId: {userId.Value}");
                return Json(new { success = false, message = "User not found" });
            }

            _logger.LogInformation($"GetProfileData - User: {user.UserId}");

            return Json(new
            {
                success = true,
                data = new
                {
                    userId = user.UserId,
                    firstName = user.UserFirstName,
                    middleName = user.UserMiddleName,
                    lastName = user.UserLastName,
                    gender = user.UserGender,
                    phone = user.UserPhone,
                    address = user.UserAddress,
                    birthdate = user.UserBirthdate?.ToString("yyyy-MM-dd"),
                    email = user.UserEmail,
                    profileImage = user.UserProfileImage ?? "default_profile.png"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting profile data: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
    {
     var userId = HttpContext.Session.GetInt32("UserId");
   _logger.LogInformation($"UpdateProfile - UserId from session: {userId}");

            if (!userId.HasValue)
            {
  _logger.LogWarning("UpdateProfile - No userId in session");
       return Json(new { success = false, message = "User not authenticated" });
}

          var user = await _userRepository.GetByIdAsync(userId.Value);

            if (user == null)
            {
          _logger.LogWarning($"UpdateProfile - No user found for userId: {userId.Value}");
      return Json(new { success = false, message = "User not found" });
            }

      // Store old values for audit logging
 var oldName = $"{user.UserFirstName} {user.UserLastName}";

 // Update user information
   user.UserFirstName = request.FirstName;
     user.UserMiddleName = request.MiddleName;
         user.UserLastName = request.LastName;
   user.UserGender = request.Gender;
            user.UserPhone = request.Phone;
   user.UserAddress = request.Address;
user.UserBirthdate = !string.IsNullOrEmpty(request.Birthdate) 
  ? DateTime.Parse(request.Birthdate) 
      : (DateTime?)null;
 user.UserUpdatedAt = DateTime.Now;

            var result = await _userRepository.UpdateAsync(user);

    if (result)
 {
       // Update session
       HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");

                // ? AUDIT LOGGING - Only for Admin users
    if (_auditService != null && HttpContext.Session.GetString("UserRole") == "Admin")
{
      var newName = $"{user.UserFirstName} {user.UserLastName}";
    await _auditService.LogActionAsync(
    "Profile Update",
             $"Updated own profile (Name: {oldName} ? {newName}, Email: {user.UserEmail})"
       );
     }
      
     _logger.LogInformation($"UpdateProfile - Successfully updated user: {user.UserId}");
             return Json(new { success = true, message = "Profile updated successfully" });
            }

            _logger.LogWarning($"UpdateProfile - Failed to update user: {user.UserId}");
            return Json(new { success = false, message = "Failed to update profile" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating profile: {ex.Message}");
        return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
    {
      try
        {
   var userId = HttpContext.Session.GetInt32("UserId");
       _logger.LogInformation($"UploadProfilePicture - UserId from session: {userId}");

       if (!userId.HasValue)
  {
           return Json(new { success = false, message = "User not authenticated" });
         }

     if (profilePicture == null || profilePicture.Length == 0)
      {
    return Json(new { success = false, message = "No file uploaded" });
            }

        // Validate file type
  var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
         var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
     {
      return Json(new { success = false, message = "Invalid file type. Only JPG, PNG, and GIF are allowed." });
    }

            // Validate file size (max 5MB)
     if (profilePicture.Length > 5 * 1024 * 1024)
      {
         return Json(new { success = false, message = "File size must be less than 5MB" });
            }

      var user = await _userRepository.GetByIdAsync(userId.Value);
   if (user == null)
         {
     return Json(new { success = false, message = "User not found" });
          }

  // Generate unique filename
         var fileName = $"user_{userId}_{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "profile");
            
   // Create directory if it doesn't exist
        if (!Directory.Exists(uploadsFolder))
        {
  Directory.CreateDirectory(uploadsFolder);
    }

 var filePath = Path.Combine(uploadsFolder, fileName);

       // Delete old profile picture if it exists and is not default
      if (!string.IsNullOrEmpty(user.UserProfileImage) && user.UserProfileImage != "default_profile.png")
     {
              var oldFilePath = Path.Combine(uploadsFolder, user.UserProfileImage);
    if (System.IO.File.Exists(oldFilePath))
              {
         System.IO.File.Delete(oldFilePath);
        }
      }

         // Save new file
   using (var stream = new FileStream(filePath, FileMode.Create))
   {
             await profilePicture.CopyToAsync(stream);
            }

            // Update user record
user.UserProfileImage = fileName;
       user.UserUpdatedAt = DateTime.Now;
            
        var result = await _userRepository.UpdateAsync(user);

 if (result)
            {
    // Update session
   HttpContext.Session.SetString("UserProfileImage", fileName);
         
           // ? AUDIT LOGGING - Only for Admin users
         if (_auditService != null && HttpContext.Session.GetString("UserRole") == "Admin")
       {
          await _auditService.LogActionAsync(
     "Profile Picture Update",
                 $"Updated profile picture (File: {fileName})"
              );
    }
          
  _logger.LogInformation($"UploadProfilePicture - Successfully uploaded for user: {user.UserId}");
          return Json(new { success = true, message = "Profile picture updated successfully", fileName = fileName });
         }

       return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
    _logger.LogError(ex, $"Error uploading profile picture: {ex.Message}");
   return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Birthdate { get; set; }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
