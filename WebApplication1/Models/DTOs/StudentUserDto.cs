namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for users with Student role (for dropdown/selection)
    /// </summary>
    public class StudentUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool HasStudentRecord { get; set; }
    }
}
