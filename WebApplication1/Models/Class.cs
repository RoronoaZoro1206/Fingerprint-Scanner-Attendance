namespace Fingerprint_Attendance_System.Models
{
    public class Class
    {
        public int ClassId { get; set; }
        public int? CourseId { get; set; }
        public int? RoomId { get; set; }
        public int? FacId { get; set; }
        public int? TermId { get; set; }
        public DateTime ClassCreatedAt { get; set; } = DateTime.Now;
        public DateTime ClassUpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Course? Course { get; set; }
        public Room? Room { get; set; }
        public Faculty? Faculty { get; set; }
        public AcademicTerm? Term { get; set; }
    }
}