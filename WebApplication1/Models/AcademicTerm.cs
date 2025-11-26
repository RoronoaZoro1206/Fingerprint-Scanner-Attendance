using System;
using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class AcademicTerm
    {
        public int TermId { get; set; }
        
        [Required(ErrorMessage = "Term name is required")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Term name must be between 5 and 100 characters")]
        public string TermName { get; set; } = string.Empty; // e.g. "1st Semester AY 2025-2026"
        
        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = false;
    }
}