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

    public AdminProgramController(
        ILogger<AdminProgramController> logger, 
        IProgramRepository programRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _programRepository = programRepository;
        _auditService = auditService;
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
            var programs = await _programRepository.GetAllAsync();
            return Json(new { data = programs });
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
            return Json(new { success = true, data = program });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching program by ID");
            return Json(new { success = false, message = "Failed to fetch program" });
        }
    }

    // POST: Program/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProgramModel program)
    {
        try
        {
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
    public async Task<IActionResult> Update([FromBody] ProgramModel program)
    {
        try
        {
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