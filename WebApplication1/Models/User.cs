using System.ComponentModel.DataAnnotations;
using Fingerprint_Attendance_System.Models.Enums;

namespace Fingerprint_Attendance_System.Models
{
    public class User
    {
        public int UserId { get; set; }
        
        [StringLength(255, ErrorMessage = "Profile image path cannot exceed 255 characters")]
        public string UserProfileImage { get; set; } = "templates/default_profile.png";
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters")]
        public string UserFirstName { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters")]
        public string? UserMiddleName { get; set; }
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters")]
        public string UserLastName { get; set; } = string.Empty;
        
        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
        public string? UserGender { get; set; } 
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? UserPhone { get; set; }
        
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string? UserAddress { get; set; }
        
        public DateTime? UserBirthdate { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string UserEmail { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
        public string UserPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Role is required")]
        [Range(1, 3, ErrorMessage = "Invalid role ID")]
        public int RoleId { get; set; }

        // Automatically maps RoleId ↔ enum
        public USER_ROLES RoleType
        {
            get => (USER_ROLES)RoleId;
            set => RoleId = (int)value;
        }

        public DateTime UserCreatedAt { get; set; } = DateTime.Now;
        public DateTime UserUpdatedAt { get; set; } = DateTime.Now;
        public DateTime? UserLastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property
        public UserRole? Role { get; set; }
    }
}