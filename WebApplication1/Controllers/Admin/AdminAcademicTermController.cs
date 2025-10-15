using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;
public class AdminAcademicTermController : Controller
{
    private readonly ILogger<AdminAcademicTermController> _logger;
    private readonly IAcademicTermRepository _academicTermRepository;
    private readonly AdminAuditService _auditService;

    public AdminAcademicTermController(
        ILogger<AdminAcademicTermController> logger, 
        IAcademicTermRepository academicTermRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _academicTermRepository = academicTermRepository;
        _auditService = auditService;
    }

    // GET: AcademicTerm/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/AcademicTerms.cshtml");
    }

    // GET: AcademicTerm/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var terms = await _academicTermRepository.GetAllAsync();
            return Json(new { data = terms });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching academic terms");
            return Json(new { error = "Failed to fetch academic terms" });
        }
    }

    // GET: AcademicTerm/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var term = await _academicTermRepository.GetByIdAsync(id);
            if (term == null)
            {
                return Json(new { success = false, message = "Academic term not found" });
            }
            return Json(new { success = true, data = term });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching academic term by ID");
            return Json(new { success = false, message = "Failed to fetch academic term" });
        }
    }

    // POST: AcademicTerm/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AcademicTerm term)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Validate dates
            if (term.EndDate <= term.StartDate)
            {
                return Json(new { success = false, message = "End date must be after start date" });
            }

            var result = await _academicTermRepository.AddAsync(term);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Created Academic Term", 
                    $"Created academic term: {term.TermName} ({term.StartDate:yyyy-MM-dd} to {term.EndDate:yyyy-MM-dd})");
                return Json(new { success = true, message = "Academic term created successfully" });
            }
            return Json(new { success = false, message = "Failed to create academic term" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating academic term");
            return Json(new { success = false, message = "An error occurred while creating academic term" });
        }
    }

    // PUT: AcademicTerm/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AcademicTerm term)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Validate dates
            if (term.EndDate <= term.StartDate)
            {
                return Json(new { success = false, message = "End date must be after start date" });
            }

            var result = await _academicTermRepository.UpdateAsync(term);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Academic Term", 
                    $"Updated academic term: {term.TermName} ({term.StartDate:yyyy-MM-dd} to {term.EndDate:yyyy-MM-dd})");
                return Json(new { success = true, message = "Academic term updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update academic term" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating academic term");
            return Json(new { success = false, message = "An error occurred while updating academic term" });
        }
    }

    // DELETE: AcademicTerm/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var term = await _academicTermRepository.GetByIdAsync(id);
            var result = await _academicTermRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Academic Term", 
                    $"Deleted academic term: {term?.TermName}");
                return Json(new { success = true, message = "Academic term deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete academic term" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting academic term");
            return Json(new { success = false, message = "An error occurred while deleting academic term" });
        }
    }

    // POST: AcademicTerm/SetActive/{id}
    [HttpPost]
    public async Task<IActionResult> SetActive(int id)
    {
        try
        {
            var term = await _academicTermRepository.GetByIdAsync(id);
            var result = await _academicTermRepository.SetActiveTermAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Set Active Academic Term", 
                    $"Set active academic term to: {term?.TermName}");
                return Json(new { success = true, message = "Active term set successfully" });
            }
            return Json(new { success = false, message = "Failed to set active term" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active term");
            return Json(new { success = false, message = "An error occurred while setting active term" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
