using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Section
    {
        public int SectionId { get; set; }
        
        [Required(ErrorMessage = "Section name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Section name must be between 1 and 50 characters")]
        public string SectionName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Program is required")]
        public int ProgramId { get; set; }
        
        [Range(1, 6, ErrorMessage = "Year level must be between 1 and 6")]
        public int? YearLevel { get; set; }
        
        public DateTime SectionCreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public ProgramModel? Program { get; set; }
    }
}