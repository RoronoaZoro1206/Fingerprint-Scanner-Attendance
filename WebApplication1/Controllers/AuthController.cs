using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Controllers;

public class AuthController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;

    public AuthController(IUserRepository userRepository, IStudentRepository studentRepository)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult FingerprintLogin()
    {
        return View();
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
            return View(model);
        }

        try
        {
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
                RoleId = 3, // Assuming 3 is the student role ID
                UserCreatedAt = DateTime.Now,
                UserUpdatedAt = DateTime.Now,
                IsActive = true,
                UserProfileImage = "default_profile.png"
            };

            // Add user to database
            bool userCreated = await _userRepository.AddAsync(user);

            if (!userCreated)
            {
                ModelState.AddModelError("", "Failed to create user account. Please try again.");
                return View(model);
            }

            // Get the created user to obtain the UserId
            var users = await _userRepository.GetAllAsync();
            var createdUser = users.FirstOrDefault(u => u.UserEmail == model.Email);

            if (createdUser == null)
            {
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
                ModelState.AddModelError("", "User created but failed to create student record.");
                return View(model);
            }
    
            // Registration successful - redirect to login
            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("Index", "Auth");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred during registration: {ex.Message}");
            return View(model);
        }
    }
}  