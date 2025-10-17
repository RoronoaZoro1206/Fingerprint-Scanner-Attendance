namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for detailed faculty information with user details
    /// </summary>
    public class FacultyDetailDto
    {
        public int FacId { get; set; }
        public int UserId { get; set; }
        public int? DeptId { get; set; }
        public string FacRank { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string UserLastName { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public string? DeptName { get; set; }
        public DateTime FacCreatedAt { get; set; }
    }
}
