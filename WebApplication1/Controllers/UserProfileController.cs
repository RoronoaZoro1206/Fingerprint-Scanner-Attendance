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
    private readonly AdminAuditService? _auditService;

    // Folder paths for new structure
    private const string TEMPLATES_FOLDER = "templates";
    private const string USERS_FOLDER = "users";
    private const string DEFAULT_PROFILE = "default_profile.png";

    public UserProfileController(
        ILogger<UserProfileController> logger,
        IUserRepository userRepository,
        IWebHostEnvironment environment,
        AdminAuditService? auditService = null)
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

            // ✅ Ensure proper path format for profile image
            var profileImagePath = GetProfileImagePath(user.UserProfileImage);

            _logger.LogInformation($"GetProfileData - User: {user.UserId}, ProfileImage: {profileImagePath}");

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
                    profileImage = profileImagePath
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
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var oldName = $"{user.UserFirstName} {user.UserLastName}";

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
                HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");

                if (_auditService != null && HttpContext.Session.GetString("UserRole") == "Admin")
                {
                    var newName = $"{user.UserFirstName} {user.UserLastName}";
                    await _auditService.LogActionAsync(
                        "Profile Update",
                        $"Updated own profile (Name: {oldName} → {newName}, Email: {user.UserEmail})"
                    );
                }
                
                _logger.LogInformation($"UpdateProfile - Successfully updated user: {user.UserId}");
                return Json(new { success = true, message = "Profile updated successfully" });
            }

            return Json(new { success = false, message = "Failed to update profile" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating profile: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // ✅ ENHANCED: Upload with user-specific naming and old image cleanup
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

            // ✅ DELETE OLD PROFILE PICTURE (if not default)
            await DeleteOldProfilePicture(user.UserProfileImage);

            // ✅ CREATE USER-SPECIFIC FILENAME: user_{userId}_{timestamp}.{ext}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"user_{userId}_{timestamp}{extension}";
            
            var usersFolder = Path.Combine(_environment.WebRootPath, "images", "profile", USERS_FOLDER);
            
            // Create users directory if it doesn't exist
            if (!Directory.Exists(usersFolder))
            {
                Directory.CreateDirectory(usersFolder);
            }

            var filePath = Path.Combine(usersFolder, fileName);

            // Save new file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            // ✅ Update user record with new path format: users/user_{userId}_{timestamp}.ext
            user.UserProfileImage = $"{USERS_FOLDER}/{fileName}";
            user.UserUpdatedAt = DateTime.Now;
            
            var result = await _userRepository.UpdateAsync(user);

            if (result)
            {
                HttpContext.Session.SetString("UserProfileImage", user.UserProfileImage);
                
                if (_auditService != null && HttpContext.Session.GetString("UserRole") == "Admin")
                {
                    await _auditService.LogActionAsync(
                        "Profile Picture Update",
                        $"Uploaded new profile picture (File: {fileName})"
                    );
                }
                
                _logger.LogInformation($"UploadProfilePicture - Successfully uploaded for user: {user.UserId}");
                return Json(new { success = true, message = "Profile picture updated successfully", fileName = user.UserProfileImage });
            }

            return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading profile picture: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // ✅ ENHANCED: Get template images from templates folder
    [HttpGet]
    public IActionResult GetAvailableProfileImages()
    {
        try
        {
            var templatesPath = Path.Combine(_environment.WebRootPath, "images", "profile", TEMPLATES_FOLDER);
            
            if (!Directory.Exists(templatesPath))
            {
                // Create templates folder if it doesn't exist
                Directory.CreateDirectory(templatesPath);
                
                // Copy default_profile.png to templates if it exists in root
                var rootDefaultPath = Path.Combine(_environment.WebRootPath, "images", "profile", DEFAULT_PROFILE);
                var templateDefaultPath = Path.Combine(templatesPath, DEFAULT_PROFILE);
                
                if (System.IO.File.Exists(rootDefaultPath) && !System.IO.File.Exists(templateDefaultPath))
                {
                    System.IO.File.Copy(rootDefaultPath, templateDefaultPath);
                }
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var templateFiles = Directory.GetFiles(templatesPath)
                .Select(Path.GetFileName)
                .Where(fileName => 
                    !string.IsNullOrEmpty(fileName) && 
                    allowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())
                )
                .OrderBy(fileName => fileName)
                .ToList();

            _logger.LogInformation($"Found {templateFiles.Count} template images");
            
            return Json(new { success = true, images = templateFiles, folder = TEMPLATES_FOLDER });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting available profile images: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // ✅ ENHANCED: Reference template directly OR copy based on user choice
    [HttpPost]
    public async Task<IActionResult> SetProfileImageFromServer([FromBody] ServerImageRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation($"SetProfileImageFromServer - UserId from session: {userId}");

            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (string.IsNullOrEmpty(request.ImageName))
            {
                return Json(new { success = false, message = "No image name provided" });
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // ✅ DELETE OLD PROFILE PICTURE ONLY if it's a user-uploaded image (not template)
            await DeleteOldProfilePicture(user.UserProfileImage);

            // ✅ Verify template exists
            var templatesPath = Path.Combine(_environment.WebRootPath, "images", "profile", TEMPLATES_FOLDER);
            var templateFilePath = Path.Combine(templatesPath, request.ImageName);

            if (!System.IO.File.Exists(templateFilePath))
            {
                return Json(new { success = false, message = "Selected template not found on server" });
            }

            // ✅ KEY FIX: Reference template directly instead of copying
            // Store path as: templates/imageName.ext
            user.UserProfileImage = $"{TEMPLATES_FOLDER}/{request.ImageName}";
            user.UserUpdatedAt = DateTime.Now;
            
            var result = await _userRepository.UpdateAsync(user);

            if (result)
            {
                HttpContext.Session.SetString("UserProfileImage", user.UserProfileImage);
                
                if (_auditService != null && HttpContext.Session.GetString("UserRole") == "Admin")
                {
                    await _auditService.LogActionAsync(
                        "Profile Picture Update",
                        $"Set profile picture from template (Template: {request.ImageName})"
                    );
                }
                
                _logger.LogInformation($"SetProfileImageFromServer - Successfully set template reference for user: {user.UserId}");
                return Json(new { success = true, message = "Profile picture updated successfully", fileName = user.UserProfileImage });
            }

            return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting profile image from server: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // ✅ COMPREHENSIVE RULE: Delete gallery image with FULL PROTECTION
    [HttpPost]
    public async Task<IActionResult> DeleteGalleryImage([FromBody] DeleteImageRequest request)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            _logger.LogInformation($"DeleteGalleryImage - UserId: {userId}, RoleId: {roleId}, ImageName: {request.ImageName}");

            if (!userId.HasValue || !roleId.HasValue)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (string.IsNullOrEmpty(request.ImageName))
            {
                return Json(new { success = false, message = "No image name provided" });
            }

            // ✅ RULE 3: Role-Based Permissions - Only Admin (RoleId = 1) can delete
            if (roleId.Value != 1) // Not Admin
            {
                _logger.LogWarning($"Non-admin user (RoleId: {roleId.Value}) attempted to delete image: {request.ImageName}");
                return Json(new { 
                    success = false, 
                    message = "Only administrators can delete profile pictures from the gallery",
                    permissionDenied = true 
                });
            }

            // ✅ SECURITY: Validate filename (prevent path traversal)
            if (request.ImageName.Contains("..") || request.ImageName.Contains("/") || request.ImageName.Contains("\\"))
            {
                _logger.LogWarning($"Attempted path traversal: {request.ImageName}");
                return Json(new { success = false, message = "Invalid image name" });
            }

            // ✅ Determine which folder the image is in
            var templatesPath = Path.Combine(_environment.WebRootPath, "images", "profile", TEMPLATES_FOLDER);
            var usersPath = Path.Combine(_environment.WebRootPath, "images", "profile", USERS_FOLDER);
            
            var templateFilePath = Path.Combine(templatesPath, request.ImageName);
            var userFilePath = Path.Combine(usersPath, request.ImageName);
            
            bool isTemplate = System.IO.File.Exists(templateFilePath);
            bool isUserImage = System.IO.File.Exists(userFilePath);
            
            string imagePath;
            string imageFolder;
            
            if (isTemplate)
            {
                imagePath = templateFilePath;
                imageFolder = "templates";
            }
            else if (isUserImage)
            {
                imagePath = userFilePath;
                imageFolder = "users";
            }
            else
            {
                return Json(new { success = false, message = "Image not found" });
            }

            // ✅ RULE 1: Template Protection - NO images in templates/ folder can EVER be deleted
            if (imageFolder == "templates")
            {
                _logger.LogWarning($"Attempted to delete template image: {request.ImageName}");
                return Json(new { 
                    success = false, 
                    message = "Cannot delete template images. Templates are shared resources used by multiple users.",
                    isTemplate = true
                });
            }

            // ✅ RULE 2: Users Folder Deletion Rules - Check if image is in use
            if (imageFolder == "users")
            {
                // Construct the relative path format used in database (e.g., "users/user_1_timestamp.jpg")
                var relativePath = $"{USERS_FOLDER}/{request.ImageName}";
                
                // Check if ANY user is using this image
                var usersUsingImage = await _userRepository.GetUsersByProfileImageAsync(relativePath);
                var usersList = usersUsingImage.ToList();
                
                if (usersList.Any())
                {
                    // Build user list for error message
                    var userDetails = usersList.Select(u => new
                    {
                        Name = $"{u.UserFirstName} {u.UserLastName}",
                        Role = u.RoleId switch
                        {
                            1 => "Admin",
                            2 => "Faculty",
                            3 => "Student",
                            _ => "Unknown"
                        }
                    }).ToList();
                    
                    var userNames = string.Join(", ", userDetails.Select(u => $"{u.Name} ({u.Role})"));
                    
                    _logger.LogWarning($"Attempted to delete in-use image: {request.ImageName}. Used by {usersList.Count} user(s)");
                    
                    return Json(new { 
                        success = false, 
                        message = $"Cannot delete: Image is currently used by {usersList.Count} user(s): {userNames}",
                        inUse = true,
                        usersCount = usersList.Count,
                        users = userDetails
                    });
                }
            }

            // ✅ SECURITY: Ensure file is within allowed directories
            var fullImagePath = Path.GetFullPath(imagePath);
            var fullTemplatesPath = Path.GetFullPath(templatesPath);
            var fullUsersPath = Path.GetFullPath(usersPath);
            
            if (!fullImagePath.StartsWith(fullTemplatesPath) && !fullImagePath.StartsWith(fullUsersPath))
            {
                _logger.LogWarning($"Attempted to delete file outside allowed directories: {request.ImageName}");
                return Json(new { success = false, message = "Invalid image path" });
            }

            // ✅ ALL CHECKS PASSED - Delete the file
            System.IO.File.Delete(imagePath);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(
                    "User Image Deleted",
                    $"Admin deleted user image: {request.ImageName} from {imageFolder}/ folder"
                );
            }

            _logger.LogInformation($"DeleteGalleryImage - Successfully deleted: {request.ImageName} from {imageFolder}/");
            return Json(new { 
                success = true, 
                message = $"Image '{request.ImageName}' has been deleted successfully",
                folder = imageFolder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting gallery image: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Helper method to delete old profile picture when user uploads/selects new one
    /// </summary>
    private async Task DeleteOldProfilePicture(string? profileImagePath)
    {
        if (string.IsNullOrEmpty(profileImagePath))
            return;

        // Don't delete if it's a template or default
        if (profileImagePath.StartsWith(TEMPLATES_FOLDER) || profileImagePath == DEFAULT_PROFILE)
            return;

        try
        {
            var oldFilePath = Path.Combine(_environment.WebRootPath, "images", "profile", profileImagePath);
            
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
                _logger.LogInformation($"Deleted old profile picture: {profileImagePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to delete old profile picture: {profileImagePath}");
            // Don't throw - this is a cleanup operation, continue with upload
        }
    }

    /// <summary>
    /// Helper method to ensure proper profile image path format
    /// </summary>
    private string GetProfileImagePath(string? profileImage)
    {
        if (string.IsNullOrEmpty(profileImage))
            return $"{TEMPLATES_FOLDER}/{DEFAULT_PROFILE}";

        // If already in proper format (contains folder prefix), return as-is
        if (profileImage.Contains("/"))
            return profileImage;

        // Legacy format - assume it's in root profile folder
        // Check if it exists in users folder first, then templates, then assume default
        var usersPath = Path.Combine(_environment.WebRootPath, "images", "profile", USERS_FOLDER, profileImage);
        if (System.IO.File.Exists(usersPath))
            return $"{USERS_FOLDER}/{profileImage}";

        var templatesPath = Path.Combine(_environment.WebRootPath, "images", "profile", TEMPLATES_FOLDER, profileImage);
        if (System.IO.File.Exists(templatesPath))
            return $"{TEMPLATES_FOLDER}/{profileImage}";

        return $"{TEMPLATES_FOLDER}/{DEFAULT_PROFILE}";
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

    public class ServerImageRequest
    {
        public string ImageName { get; set; } = string.Empty;
    }

    public class DeleteImageRequest
    {
        public string ImageName { get; set; } = string.Empty;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
