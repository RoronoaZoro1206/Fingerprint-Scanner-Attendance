using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class FacultyController : Controller
{
    private readonly ILogger<FacultyController> _logger;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public FacultyController(
        ILogger<FacultyController> logger, 
        IFacultyRepository facultyRepository,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository)
    {
        _logger = logger;
        _facultyRepository = facultyRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    // GET: Faculty/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Faculties.cshtml");
    }

    // GET: Faculty/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all faculties...");
            
            var faculties = await _facultyRepository.GetAllAsync();
            _logger.LogInformation($"Found {faculties.Count()} faculties");
            
            // Fetch all users and departments once
            var allUsers = await _userRepository.GetAllAsync();
            var allDepartments = await _departmentRepository.GetAllAsync();
            
            _logger.LogInformation($"Found {allUsers.Count()} users and {allDepartments.Count()} departments");
            
            // Create lookup dictionaries for better performance
            var userDict = allUsers.ToDictionary(u => u.UserId);
            var deptDict = allDepartments.ToDictionary(d => d.DeptId);

            var facultyList = faculties.Select(faculty =>
            {
                var user = userDict.ContainsKey(faculty.UserId) ? userDict[faculty.UserId] : null;
                var department = faculty.DeptId.HasValue && deptDict.ContainsKey(faculty.DeptId.Value) 
                    ? deptDict[faculty.DeptId.Value] 
                    : null;

                return new
                {
                    facId = faculty.FacId,
                    userId = faculty.UserId,
                    facName = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : "Unknown",
                    userEmail = user?.UserEmail ?? "",
                    deptId = faculty.DeptId,
                    deptName = department?.DeptName ?? "No Department",
                    facRank = faculty.FacRank ?? "Not Specified",
                    facCreatedAt = faculty.FacCreatedAt
                };
            }).ToList();

            _logger.LogInformation($"Returning {facultyList.Count} faculty records");
            return Json(new { data = facultyList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching faculties: {Message}", ex.Message);
            return Json(new { error = $"Failed to fetch faculties: {ex.Message}" });
        }
    }

    // GET: Faculty/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var faculty = await _facultyRepository.GetByIdAsync(id);
            if (faculty == null)
            {
                return Json(new { success = false, message = "Faculty not found" });
            }

            var user = await _userRepository.GetByIdAsync(faculty.UserId);
            
            return Json(new { 
                success = true, 
                data = new {
                    facId = faculty.FacId,
                    userId = faculty.UserId,
                    deptId = faculty.DeptId,
                    facRank = faculty.FacRank,
                    userEmail = user?.UserEmail ?? ""
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching faculty by ID");
            return Json(new { success = false, message = "Failed to fetch faculty" });
        }
    }

    // GET: Faculty/GetUsers - Get users with Faculty role
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var facultyUsers = users.Where(u => u.RoleId == 2).Select(u => new
            {
                userId = u.UserId,
                userName = $"{u.UserFirstName} {u.UserLastName}".Trim(),
                userEmail = u.UserEmail
            });

            return Json(new { success = true, data = facultyUsers });
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
    public async Task<IActionResult> Create([FromBody] Faculty faculty)
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
    public async Task<IActionResult> Update([FromBody] Faculty faculty)
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

            var result = await _facultyRepository.DeleteAsync(id);

            if (result)
            {
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