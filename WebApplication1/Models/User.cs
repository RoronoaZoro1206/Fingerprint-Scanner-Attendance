using Fingerprint_Attendance_System.Models.Enums;

namespace Fingerprint_Attendance_System.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserProfileImage { get; set; } = "default_profile.png";
        public string UserFirstName { get; set; } = string.Empty;
        public string? UserMiddleName { get; set; }
        public string UserLastName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPassword { get; set; } = string.Empty;
        public int RoleId { get; set; }

        // Automatically maps RoleId ↔ enum
        public USER_ROLES RoleType
        {
            get => (USER_ROLES)RoleId;
            set => RoleId = (int)value;
        }

        public DateTime UserCreatedAt { get; set; }
        public DateTime UserUpdatedAt { get; set; }
        public bool IsActive { get; set; }

        public UserRole? Role { get; set; }
    }
}