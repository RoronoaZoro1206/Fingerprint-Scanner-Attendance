using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminProgramController : Controller
{
    private readonly ILogger<AdminProgramController> _logger;
    private readonly IProgramRepository _programRepository;
    private readonly AdminAuditService _auditService;
    private readonly IDepartmentRepository _departmentRepository; // Injecting the department repository

    public AdminProgramController(
        ILogger<AdminProgramController> logger, 
        IProgramRepository programRepository,
        AdminAuditService auditService,
        IDepartmentRepository departmentRepository) // Adding to constructor
    {
        _logger = logger;
        _programRepository = programRepository;
        _auditService = auditService;
        _departmentRepository = departmentRepository; // Assigning
    }

    // GET: Program/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Programs.cshtml");
    }

    // GET: Program/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // ? FIX: Use GetAllAsync() instead of GetAllWithDetailsAsync()
            var programs = await _programRepository.GetAllAsync();
            
            // Get all departments for joining
            var departments = await _departmentRepository.GetAllAsync();
            
            // ? FIX: Return snake_case properties to match database column names
            var result = programs.Select(p => new
            {
                program_id = p.ProgramId,
                program_code = p.ProgramCode,
                program_name = p.ProgramName,
                program_desc = p.ProgramDesc,
                dept_id = p.DeptId,
                dept_name = p.DeptId.HasValue 
                    ? departments.FirstOrDefault(d => d.DeptId == p.DeptId)?.DeptName 
                    : null,
                program_created_at = p.ProgramCreatedAt
            });
            
            return Json(new { data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching programs");
            return Json(new { error = "Failed to fetch programs" });
        }
    }

    // GET: Program/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var program = await _programRepository.GetByIdAsync(id);
            if (program == null)
            {
                return Json(new { success = false, message = "Program not found" });
            }
            
            // ? FIX: Return snake_case properties to match database column names
            var result = new
            {
                program_id = program.ProgramId,
                program_code = program.ProgramCode,
                program_name = program.ProgramName,
                program_desc = program.ProgramDesc,
                dept_id = program.DeptId
            };
            
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching program by ID");
            return Json(new { success = false, message = "Failed to fetch program" });
        }
    }

    // POST: Program/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProgramModel? program)
    {
        try
        {
            if (program == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            program.ProgramCreatedAt = DateTime.Now;
            var result = await _programRepository.AddAsync(program);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Created Program", 
                    $"Created program: {program.ProgramCode} - {program.ProgramName}");
                return Json(new { success = true, message = "Program created successfully" });
            }
            return Json(new { success = false, message = "Failed to create program" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating program");
            return Json(new { success = false, message = "An error occurred while creating program" });
        }
    }

    // PUT: Program/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ProgramModel? program)
    {
        try
        {
            if (program == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _programRepository.UpdateAsync(program);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Program", 
                    $"Updated program: {program.ProgramCode} - {program.ProgramName}");
                return Json(new { success = true, message = "Program updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update program" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating program");
            return Json(new { success = false, message = "An error occurred while updating program" });
        }
    }

    // DELETE: Program/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var program = await _programRepository.GetByIdAsync(id);
            var result = await _programRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Program", 
                    $"Deleted program: {program?.ProgramCode} - {program?.ProgramName}");
                return Json(new { success = true, message = "Program deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete program" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting program");
            return Json(new { success = false, message = "An error occurred while deleting program" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}