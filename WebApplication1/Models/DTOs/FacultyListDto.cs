namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for faculty list display with joined user and department data
    /// </summary>
    public class FacultyListDto
    {
        public int FacId { get; set; }
        public int UserId { get; set; }
        public string FacName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int? DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public string FacRank { get; set; } = string.Empty;
        public DateTime FacCreatedAt { get; set; }
    }
}
