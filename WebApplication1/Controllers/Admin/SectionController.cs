using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class SectionController : Controller
{
    private readonly ILogger<SectionController> _logger;
    private readonly ISectionRepository _sectionRepository;
    private readonly IProgramRepository _programRepository;

    public SectionController(ILogger<SectionController> logger, ISectionRepository sectionRepository, IProgramRepository programRepository)
    {
        _logger = logger;
        _sectionRepository = sectionRepository;
        _programRepository = programRepository;
    }

    // GET: Section/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Sections.cshtml");
    }

    // GET: Section/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var sections = await _sectionRepository.GetAllAsync();
            var programs = await _programRepository.GetAllAsync();
            
            var sectionData = sections.Select(s => new
            {
                s.SectionId,
                s.SectionName,
                s.ProgramId,
                ProgramName = programs.FirstOrDefault(p => p.ProgramId == s.ProgramId)?.ProgramName ?? "N/A",
                s.YearLevel
            });
            
            return Json(new { data = sectionData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sections");
            return Json(new { error = "Failed to fetch sections" });
        }
    }

    // GET: Section/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                return Json(new { success = false, message = "Section not found" });
            }
            return Json(new { success = true, data = section });
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
            return Json(new { success = true, data = programs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching programs");
            return Json(new { success = false, message = "Failed to fetch programs" });
        }
    }

    // POST: Section/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Section section)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            section.SectionCreatedAt = DateTime.Now;
            var result = await _sectionRepository.AddAsync(section);

            if (result)
            {
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
    public async Task<IActionResult> Update([FromBody] Section section)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _sectionRepository.UpdateAsync(section);

            if (result)
            {
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
            var result = await _sectionRepository.DeleteAsync(id);

            if (result)
            {
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
