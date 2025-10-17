namespace Fingerprint_Attendance_System.Models.DTOs
{
    /// <summary>
    /// DTO for simple faculty information (for dropdown/selection)
    /// </summary>
    public class FacultySimpleDto
    {
        public int FacId { get; set; }
        public string FacName { get; set; } = string.Empty;
    }
}
