using System.Diagnostics;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers.Admin;

[RoleAuthorize(USER_ROLES.ADMIN)]
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
            _logger.LogInformation("Retrieved {Count} rooms", rooms.Count());
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
            _logger.LogInformation("Fetching room with ID: {RoomId}", id);
            var room = await _roomRepository.GetByIdAsync(id);
            
            if (room == null)
            {
                _logger.LogWarning("Room with ID {RoomId} not found", id);
                return Json(new { success = false, message = "Room not found" });
            }
            
            _logger.LogInformation("Found room: {RoomCode}", room.RoomCode);
            return Json(new { success = true, data = room });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching room by ID: {RoomId}", id);
            return Json(new { success = false, message = "Failed to fetch room" });
        }
    }

    // POST: Room/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Room? room)
    {
        try
        {
            if (room == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            _logger.LogInformation("Attempting to create room with code: {RoomCode}", room.RoomCode);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for room creation");
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _roomRepository.AddAsync(room);

            if (result)
            {
                _logger.LogInformation("Successfully created room: {RoomCode}", room.RoomCode);
                await _auditService.LogActionAsync(
                    "Created Room", 
                    $"Created room: {room.RoomCode} - {room.RoomDescription ?? "No description"}");
                return Json(new { success = true, message = "Room created successfully" });
            }
            
            _logger.LogWarning("Failed to create room: {RoomCode}", room.RoomCode);
            return Json(new { success = false, message = "Failed to create room" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room: {RoomCode}", room?.RoomCode);
            return Json(new { success = false, message = $"An error occurred while creating room: {ex.Message}" });
        }
    }

    // PUT: Room/Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Room? room)
    {
        try
        {
            if (room == null)
                return Json(new { success = false, message = "Invalid request body" });
            
            _logger.LogInformation("Attempting to update room - ID: {RoomId}, Code: {RoomCode}", room?.RoomId, room?.RoomCode);
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for room update: {Errors}", string.Join(", ", errors));
                return Json(new { success = false, message = "Invalid data provided" });
            }

            if (room == null)
            {
                _logger.LogWarning("Null room object received");
                return Json(new { success = false, message = "Room data is required" });
            }

            if (room.RoomId <= 0)
            {
                _logger.LogWarning("Invalid room ID: {RoomId}", room.RoomId);
                return Json(new { success = false, message = "Invalid room ID" });
            }

            var result = await _roomRepository.UpdateAsync(room);

            if (result)
            {
                _logger.LogInformation("Successfully updated room: {RoomCode} (ID: {RoomId})", room.RoomCode, room.RoomId);
                await _auditService.LogActionAsync(
                    "Updated Room", 
                    $"Updated room: {room.RoomCode} - {room.RoomDescription ?? "No description"}");
                return Json(new { success = true, message = "Room updated successfully" });
            }
            
            _logger.LogWarning("Failed to update room: {RoomCode} (ID: {RoomId})", room.RoomCode, room.RoomId);
            return Json(new { success = false, message = "Failed to update room. Room may not exist." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room - ID: {RoomId}, Code: {RoomCode}", room?.RoomId, room?.RoomCode);
            return Json(new { success = false, message = $"An error occurred while updating room: {ex.Message}" });
        }
    }

    // DELETE: Room/Delete/{id}
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to delete room with ID: {RoomId}", id);
            
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null)
            {
                _logger.LogWarning("Room with ID {RoomId} not found for deletion", id);
                return Json(new { success = false, message = "Room not found" });
            }
            
            var result = await _roomRepository.DeleteAsync(id);

            if (result)
            {
                _logger.LogInformation("Successfully deleted room: {RoomCode} (ID: {RoomId})", room.RoomCode, id);
                await _auditService.LogActionAsync(
                    "Deleted Room", 
                    $"Deleted room: {room.RoomCode} - {room.RoomDescription ?? "No description"}");
                return Json(new { success = true, message = "Room deleted successfully" });
            }
            
            _logger.LogWarning("Failed to delete room: {RoomCode} (ID: {RoomId})", room.RoomCode, id);
            return Json(new { success = false, message = "Failed to delete room" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room with ID: {RoomId}", id);
            return Json(new { success = false, message = "An error occurred while deleting room" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
