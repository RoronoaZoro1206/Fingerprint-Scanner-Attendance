using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Controllers;

public class ProgramController : Controller
{
    private readonly ILogger<ProgramController> _logger;
    private readonly IProgramRepository _programRepository;

    public ProgramController(ILogger<ProgramController> logger, IProgramRepository programRepository)
    {
        _logger = logger;
        _programRepository = programRepository;
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
            var result = await _programRepository.DeleteAsync(id);

            if (result)
            {
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