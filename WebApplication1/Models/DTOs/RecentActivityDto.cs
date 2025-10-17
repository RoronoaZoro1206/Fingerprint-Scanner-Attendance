namespace Fingerprint_Attendance_System.Models.DTOs;

public class RecentActivityDto
{
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
