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
    private readonly IWebHostEnvironment _environment;

    // BCrypt work factor constant (recommended: 12 for 2024)
    private const int BCRYPT_WORK_FACTOR = 12;
    
    // ✅ Folder paths for new structure
    private const string TEMPLATES_FOLDER = "templates";
    private const string USERS_FOLDER = "users";
    private const string DEFAULT_PROFILE = "default_profile.png";

    public AdminUserController(
        ILogger<AdminUserController> logger,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IAdministrationRepository administrationRepository,
        AdminAuditService auditService,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _administrationRepository = administrationRepository;
        _auditService = auditService;
        _environment = environment;
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
            // ✅ Set default profile to templates folder
            user.UserProfileImage = user.UserProfileImage ?? $"{TEMPLATES_FOLDER}/{DEFAULT_PROFILE}";

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
            
            // ✅ Preserve profile image if not changed
            if (string.IsNullOrEmpty(user.UserProfileImage))
            {
                user.UserProfileImage = existingUser.UserProfileImage;
            }

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

     // ✅ Delete user's profile picture when deleting user
            await DeleteUserProfilePicture(user.UserProfileImage);

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

    // ✅ ENHANCED: Upload with user-specific naming and old image cleanup
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

            // ✅ DELETE OLD PROFILE PICTURE (if not default)
            await DeleteOldProfilePicture(user.UserProfileImage);

            // ✅ CREATE USER-SPECIFIC FILENAME: user_{userId}_{timestamp}.{ext}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"user_{userId}_{timestamp}{extension}";
            
            var usersFolder = Path.Combine(_environment.WebRootPath, "images", "profile", USERS_FOLDER);
            
            if (!Directory.Exists(usersFolder))
            {
                Directory.CreateDirectory(usersFolder);
            }

            var filePath = Path.Combine(usersFolder, fileName);

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
                await _auditService.LogActionAsync(
                    "Profile Picture Updated",
                    $"Admin uploaded profile picture for user: {user.UserEmail} (ID: {user.UserId}, File: {fileName})"
                );
                
                _logger.LogInformation($"Profile picture uploaded for user: {user.UserId}");
                return Json(new { success = true, message = "Profile picture updated successfully", fileName = user.UserProfileImage, userId = userId });
            }

            return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // ✅ ENHANCED: Copy template to user-specific image
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

            var user = await _userRepository.GetByIdAsync(request.UserId);
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
                await _auditService.LogActionAsync(
                    "Profile Picture Updated",
                    $"Admin set profile picture from template for user: {user.UserEmail} (ID: {user.UserId}, Template: {request.ImageName})"
                );
                
                _logger.LogInformation($"Profile picture set from template for user: {user.UserId}");
                return Json(new { success = true, message = "Profile picture updated successfully", fileName = user.UserProfileImage, userId = user.UserId });
            }

            return Json(new { success = false, message = "Failed to update profile picture" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting profile image from server");
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
            // Don't throw - this is a cleanup operation
        }
    }

    /// <summary>
    /// Helper method to delete user's profile picture when user is deleted
    /// </summary>
    private async Task DeleteUserProfilePicture(string? profileImagePath)
    {
        if (string.IsNullOrEmpty(profileImagePath))
            return;

        // Don't delete if it's a template or default
        if (profileImagePath.StartsWith(TEMPLATES_FOLDER) || profileImagePath == DEFAULT_PROFILE)
            return;

        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, "images", "profile", profileImagePath);
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation($"Deleted user profile picture: {profileImagePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to delete user profile picture: {profileImagePath}");
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
