using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Attributes;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Controllers
{
    [Route("api/fingerprint")]
    [ApiController]
    [RoleAuthorize(USER_ROLES.ADMIN, USER_ROLES.FACULTY, USER_ROLES.STUDENT)]
    public class FingerprintStatusController : ControllerBase
    {
        private readonly IFingerprintTemplateRepository _fingerprintRepo;

        public FingerprintStatusController(IFingerprintTemplateRepository fingerprintRepo)
        {
            _fingerprintRepo = fingerprintRepo;
        }

        [HttpGet("check-status")]
        public async Task<IActionResult> CheckFingerprintStatus()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                if (userId == null)
                {
                    return Unauthorized(new { hasFingerprint = true }); // Return true to avoid showing prompt
                }

                // Check if user has fingerprint registered
                // Now that fingerprint_templates references users(user_id) directly,
                // we can simply check by user_id regardless of role
                var hasFingerprint = await _fingerprintRepo.HasFingerprintAsync(userId.Value);

                return Ok(new { hasFingerprint });
            }
            catch (Exception ex)
            {
                // Log error but return true to avoid annoying users with errors
                Console.WriteLine($"Error checking fingerprint status: {ex.Message}");
                return Ok(new { hasFingerprint = true });
            }
        }
    }
}
