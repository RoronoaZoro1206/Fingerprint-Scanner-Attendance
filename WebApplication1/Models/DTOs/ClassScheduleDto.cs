namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for class schedule information
/// Used for schedule conflict detection
/// </summary>
public class ClassScheduleDto
{
    public int SchedId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
