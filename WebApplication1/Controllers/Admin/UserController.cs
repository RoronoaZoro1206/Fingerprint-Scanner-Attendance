using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class UserController : Controller
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public UserController(
        ILogger<UserController> logger,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
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
            var users = await _userRepository.GetAllAsync();
            var roles = await _userRoleRepository.GetAllAsync();

            var userData = users.Select(u => new
            {
                u.UserId,
                u.UserProfileImage,
                FullName = $"{u.UserFirstName} {u.UserLastName}".Trim(),
                u.UserEmail,
                u.UserPhone,
                u.RoleId,
                RoleName = roles.FirstOrDefault(r => r.RoleId == u.RoleId)?.RoleName ?? "N/A",
                u.IsActive,
                StatusText = u.IsActive ? "Active" : "Inactive"
            });

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
                user.UserLastName,
                user.UserEmail,
                user.UserPhone,
                user.UserAddress,
                UserBirthdate = user.UserBirthdate?.ToString("yyyy-MM-dd"),
                user.RoleId,
                user.IsActive
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

            // Hash password before saving (you should use proper password hashing)
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword);
            user.UserCreatedAt = DateTime.Now;
            user.UserUpdatedAt = DateTime.Now;

            var result = await _userRepository.AddAsync(user);

            if (result)
            {
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

            // If password is provided, hash it; otherwise keep existing
            if (!string.IsNullOrEmpty(user.UserPassword))
            {
                user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword);
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
            var result = await _userRepository.DeleteAsync(id);

            if (result)
            {
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
