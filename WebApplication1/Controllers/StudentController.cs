using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Controllers;

public class StudentController : Controller
{
    private readonly ILogger<StudentController> _logger;
    private readonly IStudentRepository _studentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ISectionRepository _sectionRepository;

    public StudentController(
        ILogger<StudentController> logger,
        IStudentRepository studentRepository,
        IUserRepository userRepository,
        IProgramRepository programRepository,
        ISectionRepository sectionRepository)
    {
        _logger = logger;
        _studentRepository = studentRepository;
        _userRepository = userRepository;
        _programRepository = programRepository;
        _sectionRepository = sectionRepository;
    }

    // GET: Student/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Students.cshtml");
    }

    // GET: Student/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all students...");

            var students = await _studentRepository.GetAllAsync();
            _logger.LogInformation($"Found {students.Count()} students");

            // Fetch all related data once
            var allUsers = await _userRepository.GetAllAsync();
            var allPrograms = await _programRepository.GetAllAsync();
            var allSections = await _sectionRepository.GetAllAsync();

            _logger.LogInformation($"Found {allUsers.Count()} users, {allPrograms.Count()} programs, {allSections.Count()} sections");

            // Create lookup dictionaries for better performance
            var userDict = allUsers.ToDictionary(u => u.UserId);
            var programDict = allPrograms.ToDictionary(p => p.ProgramId);
            var sectionDict = allSections.ToDictionary(s => s.SectionId);

            var studentList = students.Select(student =>
            {
                var user = userDict.ContainsKey(student.UserId) ? userDict[student.UserId] : null;
                var program = student.ProgramId.HasValue && programDict.ContainsKey(student.ProgramId.Value)
                    ? programDict[student.ProgramId.Value]
                    : null;
                var section = student.SectionId.HasValue && sectionDict.ContainsKey(student.SectionId.Value)
                    ? sectionDict[student.SectionId.Value]
                    : null;

                return new
                {
                    studId = student.StudId,
                    userId = student.UserId,
                    studName = user != null ? $"{user.UserFirstName} {user.UserLastName}".Trim() : "Unknown",
                    userEmail = user?.UserEmail ?? "",
                    studNumber = student.StudNumber,
                    programId = student.ProgramId,
                    programName = program?.ProgramName ?? "No Program",
                    studYearLevel = student.StudYearLevel,
                    sectionId = student.SectionId,
                    sectionName = section?.SectionName ?? "No Section",
                    studCreatedAt = student.StudCreatedAt
                };
            }).ToList();

            _logger.LogInformation($"Returning {studentList.Count} student records");
            return Json(new { data = studentList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching students: {Message}", ex.Message);
            return Json(new { error = $"Failed to fetch students: {ex.Message}" });
        }
    }

    // GET: Student/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            var user = await _userRepository.GetByIdAsync(student.UserId);

            return Json(new
            {
                success = true,
                data = new
                {
                    studId = student.StudId,
                    userId = student.UserId,
                    studNumber = student.StudNumber,
                    programId = student.ProgramId,
                    studYearLevel = student.StudYearLevel,
                    sectionId = student.SectionId,
                    userEmail = user?.UserEmail ?? ""
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student by ID");
            return Json(new { success = false, message = "Failed to fetch student" });
        }
    }

    // GET: Student/GetUsers - Get users with Student role
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var studentUsers = users.Where(u => u.RoleId == 3).Select(u => new
            {
                userId = u.UserId,
                userName = $"{u.UserFirstName} {u.UserLastName}".Trim(),
                userEmail = u.UserEmail
            });

            return Json(new { success = true, data = studentUsers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student users");
            return Json(new { success = false, message = "Failed to fetch users" });
        }
    }

    // GET: Student/GetPrograms
    [HttpGet]
    public async Task<IActionResult> GetPrograms()
    {
        try
        {
            var programs = await _programRepository.GetAllAsync();
            return Json(new { success = true, data = programs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching programs");
            return Json(new { success = false, message = "Failed to fetch programs" });
        }
    }

    // GET: Student/GetSections
    [HttpGet]
    public async Task<IActionResult> GetSections()
    {
        try
        {
            var sections = await _sectionRepository.GetAllAsync();
            return Json(new { success = true, data = sections });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sections");
            return Json(new { success = false, message = "Failed to fetch sections" });
        }
    }

    // POST: Student/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Student student)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            student.StudCreatedAt = DateTime.Now;
            var result = await _studentRepository.AddAsync(student);

            if (result)
            {
                return Json(new { success = true, message = "Student created successfully" });
            }
            return Json(new { success = false, message = "Failed to create student" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return Json(new { success = false, message = "An error occurred while creating student" });
        }
    }

    // PUT: Student/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Student student)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _studentRepository.UpdateAsync(student);

            if (result)
            {
                return Json(new { success = true, message = "Student updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update student" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student");
            return Json(new { success = false, message = "An error occurred while updating student" });
        }
    }

    // DELETE: Student/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _studentRepository.DeleteAsync(id);

            if (result)
            {
                return Json(new { success = true, message = "Student deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete student" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student");
            return Json(new { success = false, message = "An error occurred while deleting student" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}