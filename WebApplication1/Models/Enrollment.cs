namespace Fingerprint_Attendance_System.Models
{
    public class Enrollment
    {
        public int EnrollId { get; set; }
        public int StudId { get; set; }
        public int ClassId { get; set; }
        public DateTime EnrollAt { get; set; }

        public Student? Student { get; set; }
        public Class? Class { get; set; }
    }
}