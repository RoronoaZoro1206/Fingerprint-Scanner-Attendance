namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for class schedule with course information
/// Used to avoid N+1 queries by fetching all data in a single query
/// </summary>
public class ClassScheduleDetailDto
{
    public int SchedId { get; set; }
    public int ClassId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string SchedDayOfWeek { get; set; } = string.Empty;
    public string SchedStartTime { get; set; } = string.Empty;
    public string SchedEndTime { get; set; } = string.Empty;
}

/// <summary>
/// DTO for student schedule with all necessary details
/// Used for late detection and absence marking
/// </summary>
public class StudentScheduleDto
{
    public int ClassId { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CourseUnits { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
}
