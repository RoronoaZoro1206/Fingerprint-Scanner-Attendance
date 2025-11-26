using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services; // ? For AuthenticationAuditService
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting; // ✅ NEW: For Rate Limiting

namespace Fingerprint_Attendance_System.Controllers.Auth;

public class AuthController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentIdPoolRepository _studentIdPoolRepository;
    private readonly AuthenticationAuditService _authAuditService; // ? NEW: Authentication audit service
    private readonly SessionHeartbeatService _heartbeatService; // ✅ NEW: Session heartbeat tracking
    private readonly ILogger<AuthController> _logger;

    // BCrypt work factor constant (recommended: 12 for 2024)
    private const int BCRYPT_WORK_FACTOR = 12;

    public AuthController(
        IUserRepository userRepository, 
        IStudentRepository studentRepository, 
        IStudentIdPoolRepository studentIdPoolRepository,
        AuthenticationAuditService authAuditService, // ? NEW: Inject authentication audit service
        SessionHeartbeatService heartbeatService, // ✅ NEW: Inject session heartbeat service
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _studentIdPoolRepository = studentIdPoolRepository;
        _authAuditService = authAuditService; // ? NEW: Store authentication audit service
        _heartbeatService = heartbeatService; // ✅ NEW: Store session heartbeat service
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")] // ✅ NEW: Rate limiting - 5 attempts per 1 minute
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
            }
            return View("Index", model);
        }

        try
        {
            // Get user by email
            var user = await _userRepository.GetByEmailAsync(model.Email);

            if (user == null)
            {
                // ? NEW: Log failed login attempt (invalid email)
                await _authAuditService.LogFailedLoginAsync(null, model.Email, "Invalid email");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Invalid email or password." } });
                }
                ModelState.AddModelError("", "Invalid email or password.");
                return View("Index", model);
            }

            // Check if account is active
            if (!user.IsActive)
            {
                // ? NEW: Log failed login attempt (inactive account)
                await _authAuditService.LogFailedLoginAsync(user.UserId, user.UserEmail, "Inactive account");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Your account has been deactivated. Please contact administration." } });
                }
                ModelState.AddModelError("", "Your account has been deactivated. Please contact administration.");
                return View("Index", model);
            }

            // Verify password
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword);
                
                if (!isPasswordValid)
                {
                    // ? NEW: Log failed login attempt (wrong password)
                    await _authAuditService.LogFailedLoginAsync(user.UserId, user.UserEmail, "Incorrect password");
                    
                    _logger.LogWarning("Failed login attempt for user {Email} - invalid password", model.Email);
                }
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                _logger.LogError(ex, "Invalid BCrypt hash format for user {Email}", model.Email);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Invalid email or password." } });
                }
                ModelState.AddModelError("", "Invalid email or password.");
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BCrypt verification failed for user {Email}", model.Email);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Invalid email or password." } });
                }
                ModelState.AddModelError("", "Invalid email or password.");
                return View("Index", model);
            }

            if (!isPasswordValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Invalid email or password." } });
                }
                ModelState.AddModelError("", "Invalid email or password.");
                return View("Index", model);
            }

            // Update last login timestamp
            await _userRepository.UpdateLastLoginAsync(user.UserId);
            
            // ? NEW: Log successful password login for ALL roles
            await _authAuditService.LogPasswordLoginAsync(user.UserId, user.UserEmail, user.RoleType.ToString());
            
            _logger.LogInformation("Successful login for user {Email}", model.Email);

            // Store user information in session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserEmail", user.UserEmail);
            HttpContext.Session.SetString("UserProfileImage", user.UserProfileImage);
            HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");
            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            // Get redirect URL based on role
            string redirectUrl = user.RoleType switch
            {
                USER_ROLES.ADMIN => Url.Action("Index", "Admin") ?? "/Admin",
                USER_ROLES.FACULTY => Url.Action("Index", "Faculty") ?? "/Faculty",
                USER_ROLES.STUDENT => Url.Action("Index", "Student") ?? "/Student",
                _ => Url.Action("Index", "Auth") ?? "/Auth"
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl });
            }

            // Redirect based on role
            return user.RoleType switch
            {
                USER_ROLES.ADMIN => RedirectToAction("Users", "Admin"),
                USER_ROLES.FACULTY => RedirectToAction("Index", "Faculty"),
                USER_ROLES.STUDENT => RedirectToAction("Index", "Student"),
                _ => RedirectToAction("Index", "Auth")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", model.Email);
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, errors = new[] { "An error occurred during login. Please try again." } });
            }
            ModelState.AddModelError("", "An error occurred during login. Please try again.");
            return View("Index", model);
        }
    }
    
    public IActionResult FingerprintLogin()
    {
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        try
        {
            // Get user info before clearing session
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            _logger.LogInformation($"Logout attempt - UserId: {userId}, Email: {userEmail}, RoleId: {roleId}");
            
            // Log logout for all authenticated users
            if (userId.HasValue && !string.IsNullOrEmpty(userEmail))
            {
                // ✅ Remove all sessions for this user from heartbeat tracking
                _heartbeatService.RemoveUserSessions(userId.Value);
                
                // Get user from database to get accurate role information
                var user = await _userRepository.GetByIdAsync(userId.Value);
                
                string roleName = "UNKNOWN";
                if (user != null)
                {
                    // Use the USER_ROLES enum for accurate role naming
                    roleName = user.RoleType.ToString().ToUpper();
                    _logger.LogInformation($"User {userEmail} role from database: {roleName}");
                }
                else if (roleId.HasValue)
                {
                    // Fallback to session roleId if user not found
                    roleName = roleId.Value switch
                    {
                        1 => "ADMIN",
                        2 => "FACULTY",
                        3 => "STUDENT",
                        _ => "UNKNOWN"
                    };
                    _logger.LogWarning($"User not found in database, using session roleId: {roleId.Value} -> {roleName}");
                }
                
                await _authAuditService.LogLogoutAsync(userId.Value, userEmail, roleName);
                _logger.LogInformation($"Logout logged for user {userEmail} ({roleName})");
            }
            else
            {
                _logger.LogWarning("Logout called but no user session data found");
            }
            
            // Clear session
            HttpContext.Session.Clear();
            
            return RedirectToAction("Index", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            
            // Still clear session even if logging fails
            HttpContext.Session.Clear();
            
            return RedirectToAction("Index", "Auth");
        }
    }
    
    /// <summary>
    /// Auto Logout - Called when user closes browser/tab without clicking logout
    /// Uses Beacon API from JavaScript to reliably log logout even during page unload
    /// ✅ ENHANCED: Now includes session duration, last activity, and device info
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AutoLogout([FromBody] AutoLogoutRequest request)
    {
        try
        {
            _logger.LogInformation("🚨 ===== AUTO LOGOUT ENDPOINT HIT =====");
            _logger.LogInformation($"📥 Request received - UserId: {request.UserId}, Email: {request.UserEmail}");
            _logger.LogInformation($"📊 Session Info - Duration: {request.SessionDuration}, Last Activity: {request.LastActivityTime}");
            _logger.LogInformation($"🖥️ Browser: {request.BrowserInfo}");
            _logger.LogInformation($"📝 Reason: {request.Reason}");
            
            if (request.UserId > 0 && !string.IsNullOrEmpty(request.UserEmail))
            {
                // Get user from database to get accurate role information
                var user = await _userRepository.GetByIdAsync(request.UserId);
                
                string roleName = "UNKNOWN";
                if (user != null)
                {
                    roleName = user.RoleType.ToString().ToUpper();
                    _logger.LogInformation($"✅ User found in database - Role: {roleName}");
                }
                else
                {
                    _logger.LogWarning($"⚠️ User ID {request.UserId} not found in database");
                }
                
                // Build detailed logout message with all context
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                
                string enhancedDetails = $"User {request.UserEmail} (ID: {request.UserId}, Role: {roleName}) automatically logged out after {request.SessionDuration} session - {request.Reason} (Last activity: {request.LastActivityTime}, IP: {ipAddress}, {request.BrowserInfo})";
                
                _logger.LogInformation($"📝 Enhanced details: {enhancedDetails}");
                
                // Log as "Auto Logout" instead of "User Logout" to distinguish
                await _authAuditService.LogAutoLogoutAsync(
                    request.UserId, 
                    request.UserEmail, 
                    roleName, 
                    enhancedDetails
                );
                
                _logger.LogInformation($"✅ Auto logout logged successfully for user {request.UserEmail} ({roleName})");
                _logger.LogInformation("🚨 ===== AUTO LOGOUT COMPLETE =====");
                
                // Return 200 OK (Beacon API doesn't care about response, but for logging)
                return Ok(new { success = true, message = "Auto logout logged successfully" });
            }
            else
            {
                _logger.LogWarning("❌ Auto logout called but no valid user data provided");
                _logger.LogWarning($"   UserId: {request.UserId}, UserEmail: '{request.UserEmail}'");
                return BadRequest(new { success = false, message = "Invalid user data" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR during auto logout");
            // Return 200 OK even on error (Beacon API doesn't retry on error responses)
            return Ok(new { success = false, message = "Error logged" });
        }
    }
    
    /// <summary>
    /// DTO for auto logout request
    /// ? ENHANCED: Now includes session tracking and device info
    /// </summary>
    public class AutoLogoutRequest
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        
        // ? NEW: Enhanced tracking fields
        public string SessionDuration { get; set; } = "Unknown";
        public string LastActivityTime { get; set; } = "Unknown";
        public string BrowserInfo { get; set; } = "Unknown";
    }
    
    /// <summary>
    /// Heartbeat endpoint - receives periodic heartbeats from active tabs
    /// Server tracks active sessions and detects closed tabs when heartbeats stop
    /// </summary>
    [HttpPost]
    public IActionResult Heartbeat([FromBody] HeartbeatRequest? request)
    {
        if (request == null || string.IsNullOrEmpty(request.TabId) || request.UserId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid heartbeat data" });
        }
        
        // Validate email is not empty for proper session tracking
        if (string.IsNullOrWhiteSpace(request.UserEmail))
        {
            return BadRequest(new { success = false, message = "User email is required" });
        }
        
        // Record the heartbeat in the session tracking service
        _heartbeatService.RecordHeartbeat(
            request.TabId,
            request.UserId,
            request.UserEmail,
            request.BrowserInfo ?? "Unknown"
        );
        
        return Ok(new { success = true, timestamp = DateTime.UtcNow });
    }
    
    /// <summary>
    /// Called when user explicitly logs out - removes session from heartbeat tracking
    /// </summary>
    [HttpPost]
    public IActionResult RemoveSession([FromBody] RemoveSessionRequest request)
    {
        if (!string.IsNullOrEmpty(request.TabId))
        {
            _heartbeatService.RemoveSession(request.TabId);
        }
        return Ok(new { success = true });
    }
    
    public class HeartbeatRequest
    {
        public string TabId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string BrowserInfo { get; set; } = string.Empty;
    }
    
    public class RemoveSessionRequest
    {
        public string TabId { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Cancel Auto Logout - Called when user returns to page after visibility hidden
    /// This prevents false positives from tab switching
    /// </summary>
    [HttpPost]
    public IActionResult CancelAutoLogout([FromBody] CancelAutoLogoutRequest request)
    {
        _logger.LogInformation($"🚫 Cancel auto logout received - User: {request.UserEmail}, LogoutId: {request.LogoutId}, Reason: {request.Reason}");
        
        // In a more sophisticated implementation, you could:
        // 1. Store pending logouts in a cache/database with the logoutId
        // 2. Cancel them here before they're processed
        // For now, we just acknowledge the cancel - the client handles the logic
        
        return Ok(new { success = true, message = "Logout cancelled" });
    }
    
    public class CancelAutoLogoutRequest
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string LogoutId { get; set; } = string.Empty;
        public string TabId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
    if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
    {
     var errors = new Dictionary<string, string>();
         foreach (var key in ModelState.Keys)
    {
               var error = ModelState[key]?.Errors.FirstOrDefault();
if (error != null)
   {
       errors[key] = error.ErrorMessage;
          }
     }
      return Json(new { success = false, errors });
            }
  return View(model);
        }

        try
  {
            // Validate password match
 if (model.Password != model.ConfirmPassword)
 {
    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
      {
    return Json(new { success = false, errors = new Dictionary<string, string> { { "ConfirmPassword", "Passwords do not match." } } });
         }
      ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
        return View(model);
  }

     // Validate student ID against the pool
            var studentIdPool = await _studentIdPoolRepository.GetByCodeAsync(model.StudentId);
       
       if (studentIdPool == null)
        {
         if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
             {
           return Json(new { success = false, errors = new Dictionary<string, string> { { "StudentId", "This student ID is not valid. Please contact administration." } } });
 }
                ModelState.AddModelError("StudentId", "This student ID is not valid. Please contact administration.");
      return View(model);
          }
            
       if (studentIdPool.IsUsed)
        {
   if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
     {
         return Json(new { success = false, errors = new Dictionary<string, string> { { "StudentId", "This student ID has already been used to create an account." } } });
         }
          ModelState.AddModelError("StudentId", "This student ID has already been used to create an account.");
    return View(model);
         }

          // Optimized: Check if email is already in use without loading all users
      var emailExists = await _userRepository.EmailExistsAsync(model.Email);
     if (emailExists)
      {
     if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
          return Json(new { success = false, errors = new Dictionary<string, string> { { "Email", "This email address is already registered." } } });
     }
        ModelState.AddModelError("Email", "This email address is already registered.");
        return View(model);
            }

    // Hash password using BCrypt.Net-Next 4.0.3 best practices
      // Work factor 12 = 2^12 = 4096 iterations (recommended for 2024)
            // This provides strong security while maintaining reasonable performance
  string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password, BCRYPT_WORK_FACTOR);
         
        _logger.LogInformation("New user password hashed with BCrypt work factor: {WorkFactor}", BCRYPT_WORK_FACTOR);

   // Create user object
     var user = new User
      {
   UserFirstName = model.FirstName,
        UserMiddleName = null,
   UserLastName = model.LastName,
         UserEmail = model.Email,
      UserPassword = hashedPassword,
      RoleType = USER_ROLES.STUDENT,
      UserCreatedAt = DateTime.Now,
      UserUpdatedAt = DateTime.Now,
            IsActive = true,
      UserProfileImage = "default_profile.png"
            };

        // Add user to database
     bool userCreated = await _userRepository.AddAsync(user);

            if (!userCreated)
    {
if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
   {
          return Json(new { success = false, errors = new Dictionary<string, string> { { "", "Failed to create user account. Please try again." } } });
     }
    ModelState.AddModelError("", "Failed to create user account. Please try again.");
           return View(model);
  }

        // Optimized: Get the created user by email without loading all users
   var createdUser = await _userRepository.GetByEmailAsync(model.Email);

 if (createdUser == null)
            {
       if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
     {
     return Json(new { success = false, errors = new Dictionary<string, string> { { "", "User created but could not retrieve user information." } } });
       }
      ModelState.AddModelError("", "User created but could not retrieve user information.");
       return View(model);
            }

        // Create student record
            var student = new Models.Student
       {
       UserId = createdUser.UserId,
       StudNumber = model.StudentId,
      StudCreatedAt = DateTime.Now
            };

  bool studentCreated = await _studentRepository.AddAsync(student);

    if (!studentCreated)
{
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
  {
             return Json(new { success = false, errors = new Dictionary<string, string> { { "", "User created but failed to create student record." } } });
        }
           ModelState.AddModelError("", "User created but failed to create student record.");
            return View(model);
    }

     // Mark the student ID as used
            await _studentIdPoolRepository.MarkAsUsedAsync(studentIdPool.PoolId);
    
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
      return Json(new { success = true, message = "Registration successful! Redirecting to login...", redirectUrl = Url.Action("Index", "Auth") });
            }

     // Registration successful - redirect to login
            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("Index", "Auth");
        }
  catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            
       if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
       return Json(new { success = false, errors = new Dictionary<string, string> { { "", "An error occurred during registration. Please try again." } } });
  }
         ModelState.AddModelError("", "An error occurred during registration. Please try again.");
     return View(model);
        }
    }
}
