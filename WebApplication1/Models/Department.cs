using System;

namespace Fingerprint_Attendance_System.Models
{
    public class Department
    {
        public int DeptId { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public string? DeptDesc { get; set; }
        public DateTime DeptCreatedAt { get; set; } = DateTime.Now;
    }
}