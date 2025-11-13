namespace Fingerprint_Attendance_System.Models.DTOs
{
    public class FingerprintRegistrationDto
    {
        public string? TemplateBase64 { get; set; }
        public string? TemplateName { get; set; }
    }

    public class FingerprintUpdateDto
    {
        public string? TemplateBase64 { get; set; }
        public string? TemplateName { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class FingerprintStatusDto
    {
        public bool IsRegistered { get; set; }
        public string? TemplateName { get; set; }
        public DateTime? RegisteredAt { get; set; }
    }

    public class DeviceStatusDto
    {
        public bool IsInitialized { get; set; }
        public bool IsDeviceConnected { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
