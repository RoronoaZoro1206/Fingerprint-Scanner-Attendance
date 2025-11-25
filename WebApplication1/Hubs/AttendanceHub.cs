using Microsoft.AspNetCore.SignalR;

namespace Fingerprint_Attendance_System.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time attendance notifications
    /// Broadcasts attendance events to all connected clients (dashboards)
    /// </summary>
    public class AttendanceHub : Hub
    {
        private readonly ILogger<AttendanceHub> _logger;

        public AttendanceHub(ILogger<AttendanceHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// ✅ Server method that clients can call to broadcast attendance
        /// (Not typically used - we broadcast from controllers instead)
        /// </summary>
        public async Task NotifyAttendance(string studentName, string action, string courseCode)
        {
            _logger.LogInformation($"Broadcasting attendance: {studentName} - {action} - {courseCode}");
            
            // Broadcast to ALL connected clients
            await Clients.All.SendAsync("ReceiveAttendance", new
            {
                studentName,
                action,
                courseCode,
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// ✅ NEW: Broadcast to specific user group (e.g., only Faculty for a specific class)
        /// </summary>
        public async Task NotifyClassAttendance(int classId, string studentName, string action)
        {
            string groupName = $"Class_{classId}";
            
            await Clients.Group(groupName).SendAsync("ReceiveClassAttendance", new
            {
                classId,
                studentName,
                action,
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// ✅ Join a specific class group (for faculty monitoring only their classes)
        /// </summary>
        public async Task JoinClassGroup(int classId)
        {
            string groupName = $"Class_{classId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} joined group {groupName}");
        }

        /// <summary>
        /// ✅ Leave a class group
        /// </summary>
        public async Task LeaveClassGroup(int classId)
        {
            string groupName = $"Class_{classId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} left group {groupName}");
        }
    }
}
