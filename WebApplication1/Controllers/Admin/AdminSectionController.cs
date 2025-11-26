using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminSectionController : Controller
{
    private readonly ILogger<AdminSectionController> _logger;
    private readonly ISectionRepository _sectionRepository;
    private readonly IProgramRepository _programRepository;
    private readonly AdminAuditService _auditService;

    public AdminSectionController(
        ILogger<AdminSectionController> logger, 
        ISectionRepository sectionRepository, 
        IProgramRepository programRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _sectionRepository = sectionRepository;
        _programRepository = programRepository;
        _auditService = auditService;
    }

    // GET: Section/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Sections.cshtml");
    }

    // GET: Section/GetAll - DataTables AJAX endpoint (Optimized with single JOIN query)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var sections = await _sectionRepository.GetAllWithDetailsAsync();
            
            var sectionData = sections.Select(s => new
            {
                sectionId = s.SectionId,
                sectionName = s.SectionName,
                programId = s.ProgramId,
                programName = s.ProgramName,
                yearLevel = s.YearLevel
            });
            
            return Json(new { data = sectionData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sections");
            return Json(new { error = "Failed to fetch sections" });
        }
    }

    // GET: Section/GetById/{id} (Optimized with single JOIN query)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var section = await _sectionRepository.GetDetailByIdAsync(id);
            if (section == null)
            {
                return Json(new { success = false, message = "Section not found" });
            }
            
            // ? FIX: Explicitly format the response to ensure correct property names
            var result = new
            {
                sectionId = section.SectionId,
                sectionName = section.SectionName,
                programId = section.ProgramId,
                programName = section.ProgramName,
                yearLevel = section.YearLevel
            };
            
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching section by ID");
            return Json(new { success = false, message = "Failed to fetch section" });
        }
    }

    // GET: Section/GetPrograms - Get all programs for dropdown
    [HttpGet]
    public async Task<IActionResult> GetPrograms()
    {
        try
        {
            var programs = await _programRepository.GetAllAsync();
            
            // ? FIX: Explicitly format the response to ensure correct property names
            var result = programs.Select(p => new
            {
                programId = p.ProgramId,
                programCode = p.ProgramCode,
                programName = p.ProgramName,
                programDesc = p.ProgramDesc
            }).ToList();
            
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching programs");
            return Json(new { success = false, message = "Failed to fetch programs" });
        }
    }

    // POST: Section/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Section? section)
    {
        try
        {
            if (section == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            section.SectionCreatedAt = DateTime.Now;
            var result = await _sectionRepository.AddAsync(section);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Created Section", 
                    $"Created section: {section.SectionName} (Year Level: {section.YearLevel})");
                return Json(new { success = true, message = "Section created successfully" });
            }
            return Json(new { success = false, message = "Failed to create section" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating section");
            return Json(new { success = false, message = "An error occurred while creating section" });
        }
    }

    // PUT: Section/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Section? section)
    {
        try
        {
            if (section == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _sectionRepository.UpdateAsync(section);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Section", 
                    $"Updated section: {section.SectionName} (Year Level: {section.YearLevel})");
                return Json(new { success = true, message = "Section updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update section" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section");
            return Json(new { success = false, message = "An error occurred while updating section" });
        }
    }

    // DELETE: Section/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var section = await _sectionRepository.GetByIdAsync(id);
            var result = await _sectionRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Section", 
                    $"Deleted section: {section?.SectionName} (Year Level: {section?.YearLevel})");
                return Json(new { success = true, message = "Section deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete section" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting section");
            return Json(new { success = false, message = "An error occurred while deleting section" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
