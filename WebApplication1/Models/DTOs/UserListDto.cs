namespace Fingerprint_Attendance_System.Models.DTOs;

public class UserListDto
{
    public int UserId { get; set; }
    public string UserProfileImage { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserPhone { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusText { get; set; } = string.Empty;
}
