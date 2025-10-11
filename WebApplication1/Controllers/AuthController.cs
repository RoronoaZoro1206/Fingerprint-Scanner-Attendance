using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Controllers;

public class AuthController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentIdPoolRepository _studentIdPoolRepository;

    public AuthController(IUserRepository userRepository, IStudentRepository studentRepository, IStudentIdPoolRepository studentIdPoolRepository)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _studentIdPoolRepository = studentIdPoolRepository;
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
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Your account has been deactivated. Please contact administration." } });
                }
                ModelState.AddModelError("", "Your account has been deactivated. Please contact administration.");
                return View("Index", model);
            }

            // Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword);

            if (!isPasswordValid)
            {   
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new[] { "Invalid email or password." } });
                }
                ModelState.AddModelError("", "Invalid email or password.");
                return View("Index", model);
            }

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
                USER_ROLES.ADMIN => RedirectToAction("Index", "Admin"),
                USER_ROLES.FACULTY => RedirectToAction("Index", "Faculty"),
                USER_ROLES.STUDENT => RedirectToAction("Index", "Student"),
                _ => RedirectToAction("Index", "Auth")
            };
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, errors = new[] { $"An error occurred during login: {ex.Message}" } });
            }
            ModelState.AddModelError("", $"An error occurred during login: {ex.Message}");
            return View("Index", model);
        }
    }
    
    public IActionResult FingerprintLogin()
    {
        return View();
    }

    public IActionResult Logout()
    {
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

            // Check if email is already in use
            var existingUsers = await _userRepository.GetAllAsync();
            if (existingUsers.Any(u => u.UserEmail.Equals(model.Email, StringComparison.OrdinalIgnoreCase)))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = new Dictionary<string, string> { { "Email", "This email address is already registered." } } });
                }
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View(model);
            }

            // Hash the password (using BCrypt or similar in production)
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            // Create user object
            var user = new User
            {
                UserFirstName = model.FirstName,
                UserMiddleName = model.MiddleName,
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

            // Get the created user to obtain the UserId
            var users = await _userRepository.GetAllAsync();
            var createdUser = users.FirstOrDefault(u => u.UserEmail == model.Email);

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
            var student = new Student
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
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, errors = new Dictionary<string, string> { { "", $"An error occurred during registration: {ex.Message}" } } });
            }
            ModelState.AddModelError("", $"An error occurred during registration: {ex.Message}");
            return View(model);
        }
    }
}  