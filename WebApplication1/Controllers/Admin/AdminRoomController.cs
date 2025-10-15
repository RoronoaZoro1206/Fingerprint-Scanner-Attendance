using System.Diagnostics;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

public class AdminRoomController : Controller
{
    private readonly ILogger<AdminRoomController> _logger;
    private readonly IRoomRepository _roomRepository;
    private readonly AdminAuditService _auditService;

    public AdminRoomController(
        ILogger<AdminRoomController> logger, 
        IRoomRepository roomRepository,
        AdminAuditService auditService)
    {
        _logger = logger;
        _roomRepository = roomRepository;
        _auditService = auditService;
    }

    // GET: Room/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Rooms.cshtml");
    }

    // GET: Room/GetAll - DataTables AJAX endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var rooms = await _roomRepository.GetAllAsync();
            return Json(new { data = rooms });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rooms");
            return Json(new { error = "Failed to fetch rooms" });
        }
    }

    // GET: Room/GetById/{id}
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                return Json(new { success = false, message = "Room not found" });
            }
            return Json(new { success = true, data = room });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching room by ID");
            return Json(new { success = false, message = "Failed to fetch room" });
        }
    }

    // POST: Room/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Room room)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _roomRepository.AddAsync(room);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Created Room", 
                    $"Created room: {room.RoomCode} - {room.RoomDescription ?? "No description"}");
                return Json(new { success = true, message = "Room created successfully" });
            }
            return Json(new { success = false, message = "Failed to create room" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return Json(new { success = false, message = "An error occurred while creating room" });
        }
    }

    // PUT: Room/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Room room)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _roomRepository.UpdateAsync(room);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Updated Room", 
                    $"Updated room: {room.RoomCode} - {room.RoomDescription ?? "No description"}");
                return Json(new { success = true, message = "Room updated successfully" });
            }
            return Json(new { success = false, message = "Failed to update room" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room");
            return Json(new { success = false, message = "An error occurred while updating room" });
        }
    }

    // DELETE: Room/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(id);
            var result = await _roomRepository.DeleteAsync(id);

            if (result)
            {
                await _auditService.LogActionAsync(
                    "Deleted Room", 
                    $"Deleted room: {room?.RoomCode} - {room?.RoomDescription ?? "No description"}");
                return Json(new { success = true, message = "Room deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete room" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room");
            return Json(new { success = false, message = "An error occurred while deleting room" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
