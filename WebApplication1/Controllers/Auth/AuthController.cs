using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services; // ? ADDED: For UniversalAuditService
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Auth;

public class AuthController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentIdPoolRepository _studentIdPoolRepository;
    private readonly UniversalAuditService _auditService; // ? ADDED: Universal audit service
    private readonly ILogger<AuthController> _logger;

    // BCrypt work factor constant (recommended: 12 for 2024)
    private const int BCRYPT_WORK_FACTOR = 12;

    public AuthController(
        IUserRepository userRepository, 
        IStudentRepository studentRepository, 
        IStudentIdPoolRepository studentIdPoolRepository,
        UniversalAuditService auditService, // ? ADDED: Inject audit service
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _studentIdPoolRepository = studentIdPoolRepository;
        _auditService = auditService; // ? ADDED: Store audit service
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
    // ? ADDED: Log failed login attempt (invalid email)
                await _auditService.LogActionAsync(
                    "Failed Password Login",
                    $"Failed login attempt for non-existent email: {model.Email}"
                );
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
                // ? ADDED: Log failed login attempt (inactive account)
                await _auditService.LogActionAsync(
                    "Failed Password Login",
                    $"Failed login attempt for inactive account: {user.UserEmail} (ID: {user.UserId}, Role: {user.RoleType})"
                );
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
       // BCrypt.Verify is compatible with all hash formats: $2a$, $2b$, $2y$
       // It automatically reads the work factor from the hash
  isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword);
         
        if (!isPasswordValid)
    {
         // ? ADDED: Log failed login attempt (wrong password)
                    await _auditService.LogActionAsync(
                        "Failed Password Login",
                        $"Failed login attempt for {user.UserEmail} (ID: {user.UserId}, Role: {user.RoleType}) - incorrect password"
                    );
   _logger.LogWarning("Failed login attempt for user {Email} - invalid password", model.Email);
     }
    }
            catch (BCrypt.Net.SaltParseException ex)
       {
         // Specific exception for malformed BCrypt hashes
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
    // General BCrypt verification error
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
   
            // ? ADDED: Log successful password login for ALL roles
            await _auditService.LogActionAsync(
                "Password Login",
                $"User {user.UserEmail} (ID: {user.UserId}, Role: {user.RoleType}) logged in via password authentication"
            );
            
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
        // ? ADDED: Get user info before clearing session
        var userId = HttpContext.Session.GetInt32("UserId");
        var userEmail = HttpContext.Session.GetString("UserEmail");
        var roleId = HttpContext.Session.GetInt32("RoleId");
        
        // ? ADDED: Log logout for all roles
        if (userId.HasValue && !string.IsNullOrEmpty(userEmail) && roleId.HasValue)
        {
            var roleName = roleId.Value switch
            {
                1 => "ADMIN",
                2 => "FACULTY",
                3 => "STUDENT",
                _ => "UNKNOWN"
            };
            
            await _auditService.LogActionAsync(
                "User Logout",
                $"User {userEmail} (ID: {userId}, Role: {roleName}) logged out"
            );
        }
        
        HttpContext.Session.Clear();
      return RedirectToAction("Index", "Auth");
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