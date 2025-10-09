namespace Fingerprint_Attendance_System.Models
{
    public class ClassSchedule
    {
        public int SchedId { get; set; }
        public int ClassId { get; set; }
        public string SchedDayOfWeek { get; set; } = string.Empty;
        public TimeSpan SchedStartTime { get; set; }
        public TimeSpan SchedEndTime { get; set; }

        public Class? Class { get; set; }
    }
}