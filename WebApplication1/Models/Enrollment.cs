using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Enrollment
    {
        public int EnrollId { get; set; }
        
        [Required(ErrorMessage = "Student is required")]
        public int StudId { get; set; }
        
        [Required(ErrorMessage = "Class is required")]
        public int ClassId { get; set; }
        
        public DateTime EnrollAt { get; set; }

        public Student? Student { get; set; }
        public Class? Class { get; set; }
    }
}