namespace Fingerprint_Attendance_System.Models.DTOs;

/// <summary>
/// DTO for student attendance statistics
/// Contains aggregated attendance data for a student
/// </summary>
public class StudentAttendanceStatsDto
{
    public int ClassesAttended { get; set; }
    public int LateArrivals { get; set; }
    public int Absences { get; set; }
    public int AttendanceRate { get; set; }
}
