using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;

namespace Fingerprint_Attendance_System.Controllers.Admin
{
    [Route("Admin/AdminRegisterFingerprint")]
    public class AdminRegisterFingerprintController : Controller
    {
        private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IUserRepository _userRepo;
        private readonly AdminAuditService _auditService;
        private readonly ILogger<AdminRegisterFingerprintController> _logger;

        public AdminRegisterFingerprintController(
            FingerprintService fingerprintService,
            IFingerprintTemplateRepository fingerprintRepo,
            IUserRepository userRepo,
            AdminAuditService auditService,
            ILogger<AdminRegisterFingerprintController> logger)
        {
            _fingerprintService = fingerprintService;
            _fingerprintRepo = fingerprintRepo;
            _userRepo = userRepo;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: Admin/AdminRegisterFingerprint
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/AdminRegisterFingerprint.cshtml");
        }

        // GET: Check device status
        [HttpGet("GetDeviceStatus")]
        public IActionResult GetDeviceStatus()
        {
            try
            {
                var status = _fingerprintService.GetStatus();
                var isInitialized = status != "Not Initialized";
                var isConnected = status == "Device Connected";

                return Json(new DeviceStatusDto
                {
                    IsInitialized = isInitialized,
                    IsDeviceConnected = isConnected,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status");
                return Json(new DeviceStatusDto
                {
                    IsInitialized = false,
                    IsDeviceConnected = false,
                    Status = "Error: " + ex.Message
                });
            }
        }

        // POST: Initialize device
        [HttpPost("InitializeDevice")]
        public IActionResult InitializeDevice()
        {
            try
            {
                _logger.LogInformation("Attempting to initialize fingerprint device...");

                var initialized = _fingerprintService.Initialize();
                if (initialized)
                {
                    _logger.LogInformation("SDK initialized, opening device...");
                    var deviceOpened = _fingerprintService.TurnOnDevice();

                    if (deviceOpened)
                    {
                        _logger.LogInformation("Device opened successfully");
                        return Json(new { success = true, message = "Device initialized successfully" });
                    }
                    else
                    {
                        _logger.LogError("Failed to open device");
                        return Json(new { success = false, message = "Failed to open device" });
                    }
                }

                _logger.LogError("Failed to initialize SDK");
                return Json(new { success = false, message = "Failed to initialize SDK" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing device");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Check registration status
        [HttpGet("GetRegistrationStatus")]
        public async Task<IActionResult> GetRegistrationStatus()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                var template = await _fingerprintRepo.GetByUserIdAsync(userId.Value);

                return Json(new FingerprintStatusDto
                {
                    IsRegistered = template != null,
                    TemplateName = template?.FtempName,
                    RegisteredAt = template?.FtempCreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Capture fingerprint
        [HttpPost("CaptureFingerprint")]
        public IActionResult CaptureFingerprint()
        {
            try
            {
                _logger.LogInformation("Attempting to capture fingerprint with image...");

                // Capture both image and template
                var (imageData, width, height, template) = _fingerprintService.CaptureFingerprintWithImage();

                if (template == null || imageData == null)
                {
                    _logger.LogWarning("Failed to capture fingerprint - no data");

                    // Error feedback: red light + beep sound
                    _fingerprintService.SetLedLight("red", true);
                    _fingerprintService.Beep(true);
                    _ = Task.Delay(500).ContinueWith(_ =>
                    {
                        _fingerprintService.SetLedLight("red", false);
                        _fingerprintService.Beep(false);
                    });

                    return Json(new { success = false, message = "Failed to capture fingerprint. Please try again." });
                }

                // Convert image to Base64 for display
                var imageBase64 = _fingerprintService.ConvertImageToBase64(imageData, width, height);
                var templateBase64 = Convert.ToBase64String(template);

                _logger.LogInformation($"Fingerprint captured successfully. Image: {width}x{height}, Template: {template.Length} bytes");

                // Success feedback: green light + beep sound
                _fingerprintService.SetLedLight("green", true);
                _fingerprintService.Beep(true);
                _ = Task.Delay(200).ContinueWith(_ =>
                {
                    _fingerprintService.SetLedLight("green", false);
                    _fingerprintService.Beep(false);
                });

                return Json(new
                {
                    success = true,
                    templateBase64,
                    imageBase64,  // Add image data for display
                    imageWidth = width,
                    imageHeight = height
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing fingerprint");

                // Error feedback: red light + beep sound
                _fingerprintService.SetLedLight("red", true);
                _fingerprintService.Beep(true);
                _ = Task.Delay(500).ContinueWith(_ =>
                {
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                });

                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Register fingerprint
        [HttpPost("RegisterFingerprint")]
        public async Task<IActionResult> RegisterFingerprint([FromBody] FingerprintRegistrationDto dto)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                if (string.IsNullOrEmpty(dto.TemplateBase64))
                {
                    return Json(new { success = false, message = "No fingerprint data provided" });
                }

                // Get user details for audit logging
                var user = await _userRepo.GetByIdAsync(userId.Value);

                // Convert base64 to byte array
                byte[] templateData = Convert.FromBase64String(dto.TemplateBase64);

                // Check if user already has a fingerprint
                var existingTemplate = await _fingerprintRepo.GetByUserIdAsync(userId.Value);

                if (existingTemplate != null)
                {
                    // User already has a fingerprint - they can only update with password confirmation
                    return Json(new {
                        success = false,
                        requirePasswordConfirmation = true,
                        message = "You already have a registered fingerprint. To update it, please confirm your password first."
                    });
                }

                // GLOBAL DUPLICATE CHECK: Compare new fingerprint against ALL registered fingerprints
                _logger.LogInformation($"Checking for duplicate fingerprints across all users for new registration (user: {user?.UserEmail})...");
                var allTemplates = await _fingerprintRepo.GetAllAsync();
    
                // Match threshold for duplicate detection
                const int duplicateMatchThreshold = 40;
     
                foreach (var existingDbTemplate in allTemplates)
                {
                    // Compare new template with this existing template
                    int matchScore = _fingerprintService.GetMatchScore(templateData, existingDbTemplate.FtempData);
                   
                   _logger.LogInformation($"Comparing with user {existingDbTemplate.UserId}: Match score = {matchScore}");
                      
                   if (matchScore >= duplicateMatchThreshold)
                    {
                      // Found a duplicate! This fingerprint is already registered to another user
                      var otherUser = await _userRepo.GetByIdAsync(existingDbTemplate.UserId);
                   
                      _logger.LogWarning(
                       $"DUPLICATE FINGERPRINT BLOCKED! User {user?.UserEmail} attempted to register " +
                          $"a fingerprint that is already registered to user {otherUser?.UserEmail} (Match score: {matchScore})"
                     );
                   
                    // Log security incident
                  await _auditService.LogActionAsync(
                        "Duplicate Fingerprint Blocked (Registration)",
                  $"Attempted to register a fingerprint already registered to user ID {existingDbTemplate.UserId} " +
              $"(Match score: {matchScore})"
        );

                  // Error feedback: red light + beep
                  _fingerprintService.SetLedLight("red", true);
                     _fingerprintService.Beep(true);
          _ = Task.Delay(500).ContinueWith(_ =>
         {
                 _fingerprintService.SetLedLight("red", false);
                 _fingerprintService.Beep(false);
                 });
              
    return Json(new 
                   { 
 success = false, 
    message = "This fingerprint is already registered to another user in the system. Please use a different finger.",
               isDuplicate = true,
         matchScore = matchScore
         });
         }
         }
             
           _logger.LogInformation($"No duplicate fingerprints found. Proceeding with registration for user {user?.UserEmail}");

  // Create new template (first registration)
          var newTemplate = new FingerprintTemplate
    {
     UserId = userId.Value,
       FtempData = templateData,
        FtempName = dto.TemplateName,
    FtempCreatedAt = DateTime.UtcNow
      };

      var templateId = await _fingerprintRepo.CreateAsync(newTemplate);

          if (templateId > 0)
                {
              // Log audit
   await _auditService.LogActionAsync(
             "Fingerprint Registration",
   $"Admin registered fingerprint biometric data (Template: {dto.TemplateName})"
          );

       // Success feedback
   _fingerprintService.SetLedLight("green", true);
           _fingerprintService.Beep(true);
      _ = Task.Delay(300).ContinueWith(_ =>
     {
   _fingerprintService.SetLedLight("green", false);
       _fingerprintService.Beep(false);
      });

           _logger.LogInformation($"Fingerprint registered successfully for user {user?.UserEmail}");
         return Json(new { success = true, message = "Fingerprint registered successfully" });
    }

       return Json(new { success = false, message = "Failed to save fingerprint" });
      }
        catch (Exception ex)
  {
 _logger.LogError(ex, "Error registering fingerprint");

                // Error feedback
                _fingerprintService.SetLedLight("red", true);
   _ = Task.Delay(500).ContinueWith(_ => _fingerprintService.SetLedLight("red", false));

        return Json(new { success = false, message = "Error: " + ex.Message });
      }
        }

        // POST: Update fingerprint with password confirmation
        [HttpPost("UpdateFingerprint")]
        public async Task<IActionResult> UpdateFingerprint([FromBody] FingerprintUpdateDto dto)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                if (string.IsNullOrEmpty(dto.TemplateBase64))
                {
                    return Json(new { success = false, message = "No fingerprint data provided" });
                }

                if (string.IsNullOrEmpty(dto.Password))
                {
                    return Json(new { success = false, message = "Password is required to update your fingerprint" });
                }

                // Verify password
                var user = await _userRepo.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Verify password using BCrypt
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.UserPassword);
                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Failed fingerprint update attempt for user {user.UserEmail} - invalid password");
                    
                    // Log failed update attempt
                    await _auditService.LogActionAsync(
                        "Failed Fingerprint Update",
                        $"Failed fingerprint update attempt - invalid password"
                    );

                    return Json(new { success = false, message = "Invalid password. Please try again." });
                }

                // Convert base64 to byte array
                byte[] newTemplateData = Convert.FromBase64String(dto.TemplateBase64);

                // DUPLICATE CHECK: Compare against all other users' fingerprints
                _logger.LogInformation($"Checking for duplicate fingerprints for user {user.UserEmail}...");
                var allTemplates = await _fingerprintRepo.GetAllAsync();
                    
                // Match threshold for duplicate detection (lower than login threshold for security)
                const int duplicateMatchThreshold = 40;
              
                foreach (var existingTemplate in allTemplates)
                {
                    // Skip the current user's own fingerprint
                    if (existingTemplate.UserId == userId.Value)
                    {
                        continue;
                    }

                    // Compare new template with this existing template
                    int matchScore = _fingerprintService.GetMatchScore(newTemplateData, existingTemplate.FtempData);
                   
                   _logger.LogInformation($"Comparing with user {existingTemplate.UserId}: Match score = {matchScore}");
                      
                   if (matchScore >= duplicateMatchThreshold)
                    {
                      // Found a duplicate! Return error
                      var otherUser = await _userRepo.GetByIdAsync(existingTemplate.UserId);
                   
                      _logger.LogWarning(
                       $"Duplicate fingerprint detected! User {user.UserEmail} attempted to register " +
                          $"a fingerprint matching user {otherUser?.UserEmail} (Match score: {matchScore})"
                     );
                   
                   // Log security incident
                  await _auditService.LogActionAsync(
                        "Duplicate Fingerprint Detected",
                  $"Attempted to update fingerprint with a template already registered to user ID {existingTemplate.UserId} " +
              $"(Match score: {matchScore})"
        );

                  // Error feedback: red light + beep
                  _fingerprintService.SetLedLight("red", true);
                     _fingerprintService.Beep(true);
          _ = Task.Delay(500).ContinueWith(_ =>
         {
                 _fingerprintService.SetLedLight("red", false);
                 _fingerprintService.Beep(false);
                 });

           return Json(new 
           { 
                 success = false, 
                  message = "This fingerprint is already registered to another user. Please use a different finger.",
                   isDuplicate = true,
              matchScore = matchScore
               });
         }
            }

         _logger.LogInformation($"No duplicate fingerprints found. Proceeding with update for user {user.UserEmail}");

        // Update existing template
           var existingUserTemplate = await _fingerprintRepo.GetByUserIdAsync(userId.Value);

       if (existingUserTemplate == null)
         {
 return Json(new { success = false, message = "No existing fingerprint found" });
         }

                var oldTemplateName = existingUserTemplate.FtempName;
      existingUserTemplate.FtempData = newTemplateData;
            existingUserTemplate.FtempName = dto.TemplateName;

        var updated = await _fingerprintRepo.UpdateAsync(existingUserTemplate);

           if (updated)
                {
 // Log audit
                 await _auditService.LogActionAsync(
                "Fingerprint Update",
          $"Admin updated fingerprint biometric data (Old: {oldTemplateName}, New: {dto.TemplateName})"
          );

          // Success feedback
         _fingerprintService.SetLedLight("green", true);
      _fingerprintService.Beep(true);
    _ = Task.Delay(300).ContinueWith(_ =>
            {
      _fingerprintService.SetLedLight("green", false);
         _fingerprintService.Beep(false);
           });

     _logger.LogInformation($"Fingerprint updated successfully for user {user.UserEmail}");
            return Json(new { success = true, message = "Fingerprint updated successfully" });
            }

              return Json(new { success = false, message = "Failed to update fingerprint" });
        }
           catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating fingerprint");

                // Error feedback
    _fingerprintService.SetLedLight("red", true);
           _ = Task.Delay(500).ContinueWith(_ => _fingerprintService.SetLedLight("red", false));

        return Json(new { success = false, message = "Error: " + ex.Message });
     }
        }

        // POST: Delete fingerprint
        [HttpPost("DeleteFingerprint")]
        public async Task<IActionResult> DeleteFingerprint()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                // Get user details for audit logging
                var user = await _userRepo.GetByIdAsync(userId.Value);
                var existingTemplate = await _fingerprintRepo.GetByUserIdAsync(userId.Value);

                var deleted = await _fingerprintRepo.DeleteByUserIdAsync(userId.Value);

                if (deleted)
                {
                    // Log audit
                    await _auditService.LogActionAsync(
                        "Fingerprint Deletion",
                        $"Admin deleted fingerprint biometric data (Template: {existingTemplate?.FtempName})"
                    );

                    _logger.LogInformation($"Fingerprint deleted for user {user?.UserEmail}");
                    return Json(new { success = true, message = "Fingerprint deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete fingerprint" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fingerprint");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
