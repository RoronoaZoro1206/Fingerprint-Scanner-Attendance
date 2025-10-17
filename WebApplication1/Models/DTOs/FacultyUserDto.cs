namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for users with Faculty role (for dropdown/selection)
    /// </summary>
    public class FacultyUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool HasFacultyRecord { get; set; }
    }
}
