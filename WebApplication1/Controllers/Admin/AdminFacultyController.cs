using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminFacultyController : Controller
{
    private readonly ILogger<AdminFacultyController> _logger;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly AdminAuditService _auditService;

    public AdminFacultyController(
        ILogger<AdminFacultyController> logger, 
        IFacultyRepository facultyRepository,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _facultyRepository = facultyRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _auditService = auditService;
    }

    // GET: Faculty/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Faculties.cshtml");
    }

    // GET: Faculty/GetAll - DataTables AJAX endpoint (Optimized with single JOIN query)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all faculties with details...");
            
            var facultyList = await _facultyRepository.GetAllWithDetailsAsync();
            
            var result = facultyList.Select(f => new
            {
                facId = f.FacId,
                userId = f.UserId,
                facName = f.FacName,
                userEmail = f.UserEmail,
                deptId = f.DeptId,
                deptName = f.DeptName,
                facRank = f.FacRank,
                facCreatedAt = f.FacCreatedAt
            }).ToList();

            _logger.LogInformation($"Returning {result.Count} faculty records");
            return Json(new { data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching faculties: {Message}", ex.Message);
            return Json(new { error = $"Failed to fetch faculties: {ex.Message}" });
        }
    }

    // GET: Faculty/GetById/{id} (Optimized with single JOIN query)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var faculty = await _facultyRepository.GetDetailByIdAsync(id);
            if (faculty == null)
            {
                return Json(new { success = false, message = "Faculty not found" });
            }
            
            return Json(new { 
                success = true, 
                data = new {
                    facId = faculty.FacId,
                    userId = faculty.UserId,
                    deptId = faculty.DeptId,
                    facRank = faculty.FacRank,
                    userEmail = faculty.UserEmail
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching faculty by ID");
            return Json(new { success = false, message = "Failed to fetch faculty" });
        }
    }

    // GET: Faculty/GetUsers - Get users with Faculty role (Optimized with single query)
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var facultyUsers = await _facultyRepository.GetAvailableFacultyUsersAsync();
            
            var result = facultyUsers.Select(u => new
            {
                userId = u.UserId,
                userName = u.UserName,
                userEmail = u.UserEmail,
                hasFacultyRecord = u.HasFacultyRecord
            });

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching faculty users");
            return Json(new { success = false, message = "Failed to fetch users" });
        }
    }

    // GET: Faculty/GetDepartments
    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        try
        {
            var departments = await _departmentRepository.GetAllAsync();
            return Json(new { success = true, data = departments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching departments");
            return Json(new { success = false, message = "Failed to fetch departments" });
        }
    }

    // POST: Faculty/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Fingerprint_Attendance_System.Models.Faculty faculty)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Check if user already has a faculty record
            var existing = await _facultyRepository.GetByUserIdAsync(faculty.UserId);
            if (existing != null)
            {
                return Json(new { success = false, message = "This user already has a faculty record" });
            }

            faculty.FacCreatedAt = DateTime.Now;
            var result = await _facultyRepository.AddAsync(faculty);

            if (result)
            {
                var user = await _userRepository.GetByIdAsync(faculty.UserId);
                var userName = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : "Unknown";
                await _auditService.LogActionAsync(
                    "Created Faculty", 
                    $"Created faculty record for: {userName} (User ID: {faculty.UserId})");
                return Json(new { success = true, message = "Faculty created successfully" });
            }
            return Json(new { success = false, message = "Failed to create faculty" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating faculty");
            return Json(new { success = false, message = "An error occurred while creating faculty" });
        }
    }

    // PUT: Faculty/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Fingerprint_Attendance_System.Models.Faculty faculty)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var exists = await _facultyRepository.ExistsAsync(faculty.FacId);
            if (!exists)
            {
                return Json(new { success = false, message = "Faculty not found" });
            }

            var result = await _facultyRepository.UpdateAsync(faculty);

            if (result)
            {
                var user = await _userRepository.GetByIdAsync(faculty.UserId);
                var userName = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : "Unknown";
                await _auditService.LogActionAsync(
                    "Updated Faculty", 
                    $"Updated faculty record for: {userName} (User ID: {faculty.UserId})");
                return Json(new { success = true, message = "Faculty updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update faculty" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating faculty");
            return Json(new { success = false, message = "An error occurred while updating faculty" });
        }
    }

    // DELETE: Faculty/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var exists = await _facultyRepository.ExistsAsync(id);
            if (!exists)
            {
                return Json(new { success = false, message = "Faculty not found" });
            }

            var faculty = await _facultyRepository.GetByIdAsync(id);
            var user = faculty != null ? await _userRepository.GetByIdAsync(faculty.UserId) : null;
            var userName = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : "Unknown";

            var result = await _facultyRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Faculty", 
                    $"Deleted faculty record for: {userName} (User ID: {faculty?.UserId})");
                return Json(new { success = true, message = "Faculty deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete faculty" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting faculty");
            return Json(new { success = false, message = "An error occurred while deleting faculty" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}