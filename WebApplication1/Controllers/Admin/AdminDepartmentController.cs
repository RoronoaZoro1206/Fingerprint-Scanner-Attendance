using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class AdminDepartmentController : Controller
{
    private readonly ILogger<AdminDepartmentController> _logger;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly AdminAuditService _auditService;

    public AdminDepartmentController(
        ILogger<AdminDepartmentController> logger, 
        IDepartmentRepository departmentRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _departmentRepository = departmentRepository;
        _auditService = auditService;
    }

    // GET: Department/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Departments.cshtml");
    }

    // GET: Department/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var departments = await _departmentRepository.GetAllAsync();
            return Json(new { data = departments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching departments");
            return Json(new { error = "Failed to fetch departments" });
        }
    }

    // GET: Department/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null)
            {
                return Json(new { success = false, message = "Department not found" });
            }
            return Json(new { success = true, data = department });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching department by ID");
            return Json(new { success = false, message = "Failed to fetch department" });
        }
    }

    // POST: Department/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Department department)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Check if department code already exists
            var existing = await _departmentRepository.GetByCodeAsync(department.DeptCode);
            if (existing != null)
            {
                return Json(new { success = false, message = "Department code already exists" });
            }

            department.DeptCreatedAt = DateTime.Now;
            var result = await _departmentRepository.AddAsync(department);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Created Department", 
                    $"Created department: {department.DeptCode} - {department.DeptName}");
                return Json(new { success = true, message = "Department created successfully" });
            }
            return Json(new { success = false, message = "Failed to create department" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return Json(new { success = false, message = "An error occurred while creating department" });
        }
    }

    // PUT: Department/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Department department)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var exists = await _departmentRepository.ExistsAsync(department.DeptId);
            if (!exists)
            {
                return Json(new { success = false, message = "Department not found" });
            }

            // Check if department code already exists for another department
            var existing = await _departmentRepository.GetByCodeAsync(department.DeptCode);
            if (existing != null && existing.DeptId != department.DeptId)
            {
                return Json(new { success = false, message = "Department code already exists" });
            }

            var result = await _departmentRepository.UpdateAsync(department);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Department", 
                    $"Updated department: {department.DeptCode} - {department.DeptName}");
                return Json(new { success = true, message = "Department updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update department" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department");
            return Json(new { success = false, message = "An error occurred while updating department" });
        }
    }

    // DELETE: Department/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var exists = await _departmentRepository.ExistsAsync(id);
            if (!exists)
            {
                return Json(new { success = false, message = "Department not found" });
            }

            var department = await _departmentRepository.GetByIdAsync(id);
            var result = await _departmentRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Department", 
                    $"Deleted department: {department?.DeptCode} - {department?.DeptName}");
                return Json(new { success = true, message = "Department deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete department" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department");
            return Json(new { success = false, message = "An error occurred while deleting department" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}