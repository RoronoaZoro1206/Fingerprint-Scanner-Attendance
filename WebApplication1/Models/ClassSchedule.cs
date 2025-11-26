using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class ClassSchedule
    {
        public int SchedId { get; set; }
        
        [Required(ErrorMessage = "Class is required")]
        public int ClassId { get; set; }
        
        [Required(ErrorMessage = "Day of week is required")]
        [StringLength(10, ErrorMessage = "Day of week cannot exceed 10 characters")]
        public string SchedDayOfWeek { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan SchedStartTime { get; set; }
        
        [Required(ErrorMessage = "End time is required")]
        public TimeSpan SchedEndTime { get; set; }

        public Class? Class { get; set; }
    }
}