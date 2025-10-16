using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
public class AdminStudentIdPoolController : Controller
{
    private readonly ILogger<AdminStudentIdPoolController> _logger;
    private readonly IStudentIdPoolRepository _studentIdPoolRepository;
    private readonly AdminAuditService _auditService;

    public AdminStudentIdPoolController(
        ILogger<AdminStudentIdPoolController> logger,
        IStudentIdPoolRepository studentIdPoolRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _studentIdPoolRepository = studentIdPoolRepository;
        _auditService = auditService;
    }

    // GET: StudentIdPool/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/StudentIdPools.cshtml");
    }

    // GET: StudentIdPool/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all student ID pools...");

            var pools = await _studentIdPoolRepository.GetAllAsync();
            _logger.LogInformation($"Found {pools.Count()} student ID pool entries");

            var poolList = pools.Select(pool => new
            {
                poolId = pool.PoolId,
                studentIdCode = pool.StudentIdCode,
                isUsed = pool.IsUsed,
                generatedBy = pool.GeneratedBy,
                generatedAt = pool.GeneratedAt,
                usedAt = pool.UsedAt,
                remarks = pool.Remarks
            }).ToList();

            _logger.LogInformation($"Returning {poolList.Count} student ID pool records");
            return Json(new { data = poolList });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student ID pools: {Message}", ex.Message);
            return Json(new { error = $"Failed to fetch student ID pools: {ex.Message}" });
        }
    }

    // GET: StudentIdPool/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var pool = await _studentIdPoolRepository.GetByIdAsync(id);
            if (pool == null)
            {
                return Json(new { success = false, message = "Student ID not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    poolId = pool.PoolId,
                    studentIdCode = pool.StudentIdCode,
                    isUsed = pool.IsUsed,
                    remarks = pool.Remarks
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student ID pool by ID");
            return Json(new { success = false, message = "Failed to fetch student ID" });
        }
    }

    // POST: StudentIdPool/Generate
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateStudentIdsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            if (request.Count <= 0 || request.Count > 1000)
            {
                return Json(new { success = false, message = "Count must be between 1 and 1000" });
            }

            var year = request.Year;
            var startNumber = request.StartNumber;
            var generatedIds = new List<string>();
            var failedIds = new List<string>();

            for (int i = 0; i < request.Count; i++)
            {
                var studentIdCode = $"{year}-{(startNumber + i):D4}";

                // Check if ID already exists
                var exists = await _studentIdPoolRepository.ExistsAsync(studentIdCode);
                if (exists)
                {
                    failedIds.Add(studentIdCode);
                    continue;
                }

                var pool = new StudentIdPool
                {
                    StudentIdCode = studentIdCode,
                    IsUsed = false,
                    GeneratedBy = request.GeneratedBy,
                    GeneratedAt = DateTime.Now,
                    Remarks = request.Remarks
                };

                var result = await _studentIdPoolRepository.AddAsync(pool);
                if (result)
                {
                    generatedIds.Add(studentIdCode);
                }
                else
                {
                    failedIds.Add(studentIdCode);
                }
            }

            var message = $"Generated {generatedIds.Count} student IDs successfully.";
            if (failedIds.Count > 0)
            {
                message += $" {failedIds.Count} IDs already exist or failed to create.";
            }

            if (generatedIds.Count > 0)
            {
                await _auditService.LogActionAsync(
                    "Generated Student IDs", 
                    $"Generated {generatedIds.Count} student IDs for year {year} starting from {year}-{startNumber:D4}");
            }

            return Json(new
            {
                success = true,
                message = message,
                generated = generatedIds.Count,
                failed = failedIds.Count,
                generatedIds = generatedIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating student IDs");
            return Json(new { success = false, message = "An error occurred while generating student IDs" });
        }
    }

    // PUT: StudentIdPool/UpdateRemarks
    [HttpPut]
    public async Task<IActionResult> UpdateRemarks([FromBody] UpdateRemarksRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var pool = await _studentIdPoolRepository.GetByIdAsync(request.PoolId);
            var result = await _studentIdPoolRepository.UpdateRemarksAsync(request.PoolId, request.Remarks ?? "");

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Student ID Remarks", 
                    $"Updated remarks for student ID: {pool?.StudentIdCode}");
                return Json(new { success = true, message = "Remarks updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update remarks" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating remarks");
            return Json(new { success = false, message = "An error occurred while updating remarks" });
        }
    }

    // DELETE: StudentIdPool/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Check if the ID is already used
            var pool = await _studentIdPoolRepository.GetByIdAsync(id);
            if (pool == null)
            {
                return Json(new { success = false, message = "Student ID not found" });
            }

            if (pool.IsUsed)
            {
                return Json(new { success = false, message = "Cannot delete a student ID that is already in use" });
            }

            var result = await _studentIdPoolRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Student ID", 
                    $"Deleted unused student ID: {pool.StudentIdCode}");
                return Json(new { success = true, message = "Student ID deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete student ID" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student ID");
            return Json(new { success = false, message = "An error occurred while deleting student ID" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

// Request models
public class GenerateStudentIdsRequest
{
    public int Year { get; set; }
    public int StartNumber { get; set; }
    public int Count { get; set; }
    public int? GeneratedBy { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateRemarksRequest
{
    public int PoolId { get; set; }
    public string? Remarks { get; set; }
}
