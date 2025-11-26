using System;
using System.ComponentModel.DataAnnotations;

namespace Fingerprint_Attendance_System.Models
{
    public class Department
    {
        public int DeptId { get; set; }
        
        [Required(ErrorMessage = "Department code is required")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Department code must be between 2 and 20 characters")]
        public string DeptCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Department name must be between 3 and 100 characters")]
        public string DeptName { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? DeptDesc { get; set; }
        
        public DateTime DeptCreatedAt { get; set; } = DateTime.Now;
    }
}