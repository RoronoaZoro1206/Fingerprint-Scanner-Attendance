using System;

namespace Fingerprint_Attendance_System.Models
{
    public class AcademicTerm
    {
        public int TermId { get; set; }
        public string TermName { get; set; } = string.Empty; // e.g. "1st Semester AY 2025-2026"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = false;
    }
}