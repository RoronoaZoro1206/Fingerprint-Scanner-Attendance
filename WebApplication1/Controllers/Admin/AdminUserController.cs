using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminUserController : Controller
{
    private readonly ILogger<AdminUserController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAdministrationRepository _administrationRepository;
    private readonly AdminAuditService _auditService;

    // BCrypt work factor constant (recommended: 12 for 2024)
    private const int BCRYPT_WORK_FACTOR = 12;

    public AdminUserController(
        ILogger<AdminUserController> logger,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IAdministrationRepository administrationRepository,
        AdminAuditService auditService)
    {
      _logger = logger;
  _userRepository = userRepository;
     _userRoleRepository = userRoleRepository;
        _administrationRepository = administrationRepository;
        _auditService = auditService;
    }

 // GET: User/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Users.cshtml");
    }

    // GET: User/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
  // Optimized: Single query with JOIN to get users with roles
   var userData = await _userRepository.GetUsersWithRolesAsync();

          return Json(new { data = userData });
        }
        catch (Exception ex)
{
            _logger.LogError(ex, "Error fetching users");
     return Json(new { error = "Failed to fetch users" });
        }
    }

    // GET: User/GetById/{id}
    [HttpGet]
 public async Task<IActionResult> GetById(int id)
    {
        try
    {
     var user = await _userRepository.GetByIdAsync(id);
 if (user == null)
      {
return Json(new { success = false, message = "User not found" });
    }

            // Don't send password to client
            var userData = new
            {
        user.UserId,
            user.UserFirstName,
            user.UserMiddleName,
        user.UserLastName,
     user.UserEmail,
     user.UserPhone,
       user.UserAddress,
       user.UserGender,
             UserBirthdate = user.UserBirthdate?.ToString("yyyy-MM-dd"),
     user.RoleId,
   user.IsActive,
   user.UserProfileImage
  };

       return Json(new { success = true, data = userData });
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error fetching user by ID");
return Json(new { success = false, message = "Failed to fetch user" });
        }
    }

    // GET: User/GetRoles
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
        var roles = await _userRoleRepository.GetAllAsync();
            return Json(new { success = true, data = roles });
        }
        catch (Exception ex)
 {
         _logger.LogError(ex, "Error fetching roles");
            return Json(new { success = false, message = "Failed to fetch roles" });
     }
    }

    // POST: User/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Check if email already exists
            var emailExists = await _userRepository.EmailExistsAsync(user.UserEmail);
            if (emailExists)
            {
                return Json(new { success = false, message = "This email address is already registered." });
            }

            // Hash password using BCrypt.Net-Next 4.0.3 best practices
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword, BCRYPT_WORK_FACTOR);
            user.UserCreatedAt = DateTime.Now;
            user.UserUpdatedAt = DateTime.Now;
            user.UserProfileImage = user.UserProfileImage ?? "default_profile.png";

            _logger.LogInformation("Creating user {Email} with BCrypt work factor: {WorkFactor}",
                user.UserEmail, BCRYPT_WORK_FACTOR);

            var result = await _userRepository.AddAsync(user);

            if (result)
            {
                // Get the created user to get the user_id
                var createdUser = await _userRepository.GetByEmailAsync(user.UserEmail);

                if (createdUser != null)
                {
                    // Check if user role is admin (role_id = 1)
                    if (createdUser.RoleId == 1)
                    {
                        // Generate unique admin code
                        var adminCode = await _administrationRepository.GenerateNextAdminCodeAsync();

                        // Create administration record
                        var administration = new Administration
                        {
                            UserId = createdUser.UserId,
                            AdminCode = adminCode,
                            AdminCreatedAt = DateTime.Now
                        };

                        var adminResult = await _administrationRepository.AddAsync(administration);

                        if (adminResult)
                        {
                            // ? LOG AUDIT: Created Admin User
                            await _auditService.LogActionAsync(
                                "Created Admin User",
                                $"Created admin user: {createdUser.UserEmail} with code {adminCode}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to create administration record for user {createdUser.UserId}");
                        }
                    }
                    else
                    {
                        // ? LOG AUDIT: Created Regular User
                        var roleName = createdUser.RoleId == 2 ? "Faculty" : "Student";
                        await _auditService.LogActionAsync(
                            $"Created {roleName} User",
                            $"Created {roleName.ToLower()} user: {createdUser.UserEmail} (ID: {createdUser.UserId})");
                    }

                    // Return success with userId for profile picture upload
                    return Json(new { success = true, message = "User created successfully", userId = createdUser.UserId });
                }

                return Json(new { success = true, message = "User created successfully" });
            }
            return Json(new { success = false, message = "Failed to create user" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Json(new { success = false, message = "An error occurred while creating user" });
        }
    }

    // PUT: User/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] User user)
    {
        try
     {
      if (!ModelState.IsValid)
       {
    return Json(new { success = false, message = "Invalid data provided" });
     }

 // Get existing user to preserve password if not changed
            var existingUser = await _userRepository.GetByIdAsync(user.UserId);
        if (existingUser == null)
        {
     return Json(new { success = false, message = "User not found" });
      }

        // Check if email is being changed and if new email already exists
            if (existingUser.UserEmail != user.UserEmail)
            {
   var emailExists = await _userRepository.EmailExistsAsync(user.UserEmail);
       if (emailExists)
    {
        return Json(new { success = false, message = "This email address is already registered." });
  }
            }

            // Check if role changed to/from admin
            var roleChanged = existingUser.RoleId != user.RoleId;
var wasAdmin = existingUser.RoleId == 1;
  var isNowAdmin = user.RoleId == 1;

     // If password is provided, hash it; otherwise keep existing
          if (!string.IsNullOrEmpty(user.UserPassword))
     {
                user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword, BCRYPT_WORK_FACTOR);
    _logger.LogInformation("Updating password for user {Email} with BCrypt work factor: {WorkFactor}",
        user.UserEmail, BCRYPT_WORK_FACTOR);
  }
 else
            {
    user.UserPassword = existingUser.UserPassword;
}

            user.UserUpdatedAt = DateTime.Now;
   user.UserCreatedAt = existingUser.UserCreatedAt;

        var result = await _userRepository.UpdateAsync(user);

            if (result)
            {
    // Handle administration record based on role change
    if (roleChanged)
      {
           if (isNowAdmin && !wasAdmin)
       {
           // User promoted to admin - create administration record
     var existingAdmin = await _administrationRepository.GetByUserIdAsync(user.UserId);
  if (existingAdmin == null)
           {
      var adminCode = await _administrationRepository.GenerateNextAdminCodeAsync();
   var administration = new Administration
    {
             UserId = user.UserId,
   AdminCode = adminCode,
          AdminCreatedAt = DateTime.Now
   };

   await _administrationRepository.AddAsync(administration);
      
      // ? LOG AUDIT: User promoted to Admin
        await _auditService.LogActionAsync(
"User Promoted to Admin",
       $"User {user.UserEmail} promoted to admin with code {adminCode}");
  }
            }
     else if (wasAdmin && !isNowAdmin)
  {
   // User demoted from admin
       var existingAdmin = await _administrationRepository.GetByUserIdAsync(user.UserId);
     if (existingAdmin != null)
               {
    // ? LOG AUDIT: Admin demoted
         await _auditService.LogActionAsync(
         "Admin Demoted to User",
      $"User {user.UserEmail} demoted from admin (code: {existingAdmin.AdminCode})");
    }
       }
                }
         else
    {
  // ? LOG AUDIT: Regular user update
              var roleName = user.RoleId == 1 ? "Admin" : (user.RoleId == 2 ? "Faculty" : "Student");
await _auditService.LogActionAsync(
     $"Updated {roleName} User",
              $"Updated {roleName.ToLower()} user: {user.UserEmail} (ID: {user.UserId})");
}

             return Json(new { success = true, message = "User updated successfully" });
  }
   return Json(new { success = false, message = "Failed to update user" });
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating user");
  return Json(new { success = false, message = "An error occurred while updating user" });
        }
    }

  // DELETE: User/Delete/{id}
 [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
  try
 {
            var user = await _userRepository.GetByIdAsync(id);
      if (user == null)
          {
       return Json(new { success = false, message = "User not found" });
          }

     var result = await _userRepository.DeleteAsync(id);

       if (result)
{
        // ? LOG AUDIT: User deleted
   var roleName = user.RoleId == 1 ? "Admin" : (user.RoleId == 2 ? "Faculty" : "Student");
            await _auditService.LogActionAsync(
   $"Deleted {roleName} User",
    $"Deleted {roleName.ToLower()} user: {user.UserEmail} (ID: {user.UserId}, Role ID: {user.RoleId})");
                
         return Json(new { success = true, message = "User deleted successfully" });
        }
            return Json(new { success = false, message = "Failed to delete user" });
        }
        catch (Exception ex)
     {
            _logger.LogError(ex, "Error deleting user");
            return Json(new { success = false, message = "An error occurred while deleting user" });
    }
    }

    // POST: AdminUser/UploadUserProfilePicture
    [HttpPost]
    public async Task<IActionResult> UploadUserProfilePicture(IFormFile profilePicture, int userId)
    {
        try
        {
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

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Generate unique filename
            var fileName = $"user_{userId}_{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profile");
            
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
                // ? LOG AUDIT: Profile picture updated by admin
                await _auditService.LogActionAsync(
                    "Profile Picture Updated",
                    $"Admin updated profile picture for user: {user.UserEmail} (ID: {user.UserId}, File: {fileName})"
                );
                
                _logger.LogInformation($"Profile picture updated for user: {user.UserId}");
                return Json(new { success = true, message = "Profile picture updated successfully", fileName = fileName, userId = userId });
            }

            return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // POST: AdminUser/SetUserProfileImageFromServer
    [HttpPost]
    public async Task<IActionResult> SetUserProfileImageFromServer([FromBody] ServerImageRequest request)
    {
        try
        {
            if (request.UserId <= 0)
            {
                return Json(new { success = false, message = "Invalid user ID" });
            }

            if (string.IsNullOrEmpty(request.ImageName))
            {
                return Json(new { success = false, message = "No image name provided" });
            }

            // Validate that the image exists in server directory
            var profileImagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profile");
            var imagePath = Path.Combine(profileImagesPath, request.ImageName);

            if (!System.IO.File.Exists(imagePath))
            {
                return Json(new { success = false, message = "Selected image not found on server" });
            }

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Update user record with server image name
            user.UserProfileImage = request.ImageName;
            user.UserUpdatedAt = DateTime.Now;
            
            var result = await _userRepository.UpdateAsync(user);

            if (result)
            {
                // ✅ LOG AUDIT: Profile picture updated by admin from server
                await _auditService.LogActionAsync(
                    "Profile Picture Updated",
                    $"Admin set profile picture from server for user: {user.UserEmail} (ID: {user.UserId}, File: {request.ImageName})"
                );
                
                _logger.LogInformation($"Profile picture set from server for user: {user.UserId}");
                return Json(new { success = true, message = "Profile picture updated successfully", fileName = request.ImageName, userId = user.UserId });
            }

            return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting profile image from server");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    public class ServerImageRequest
    {
        public int UserId { get; set; }
        public string ImageName { get; set; } = string.Empty;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
  {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
 }
}
