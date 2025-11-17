using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;

namespace Fingerprint_Attendance_System.Controllers.Student
{
    [Route("Student/StudentRegisterFingerprint")]
 public class StudentRegisterFingerprintController : Controller
    {
     private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IUserRepository _userRepo;
private readonly IStudentRepository _studentRepo;
        private readonly UniversalAuditService _auditService; // ✅ ADDED: Universal audit service
 private readonly ILogger<StudentRegisterFingerprintController> _logger;

  public StudentRegisterFingerprintController(
FingerprintService fingerprintService,
 IFingerprintTemplateRepository fingerprintRepo,
  IUserRepository userRepo,
   IStudentRepository studentRepo,
   UniversalAuditService auditService,
      ILogger<StudentRegisterFingerprintController> logger)
      {
         _fingerprintService = fingerprintService;
      _fingerprintRepo = fingerprintRepo;
 _userRepo = userRepo;
  _studentRepo = studentRepo;
 _auditService = auditService;
     _logger = logger;
}

 // GET: Student/StudentRegisterFingerprint
  [HttpGet("")]
[HttpGet("Index")]
        public IActionResult Index()
        {
       return View("~/Views/Student/StudentRegisterFingerprint.cshtml");
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

                return Json(new
                {
                    isInitialized = isInitialized,        // ✅ camelCase
                    isDeviceConnected = isConnected,      // ✅ camelCase
                    status = status                        // ✅ camelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status");
                return Json(new
                {
                    isInitialized = false,                // ✅ camelCase
                    isDeviceConnected = false,            // ✅ camelCase
                    status = "Error: " + ex.Message       // ✅ camelCase
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
          
                return Json(new
                {
                    isRegistered = template != null,              // ✅ camelCase
                    templateName = template?.FtempName,           // ✅ camelCase
                    registeredAt = template?.FtempCreatedAt       // ✅ camelCase
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
       
      // ✅ Ensure device is initialized before capture
      if (!_fingerprintService.IsDeviceConnected)
      {
          _logger.LogWarning("Device not connected. Attempting to initialize...");
          var initialized = _fingerprintService.Initialize();
          if (initialized)
          {
              var deviceOpened = _fingerprintService.TurnOnDevice();
              if (!deviceOpened)
              {
                  return Json(new { 
                      success = false, 
                      message = "Failed to connect to scanner. Please check USB connection.",
                      shouldRefreshStatus = true 
                  });
              }
          }
          else
          {
              return Json(new { 
                  success = false, 
                  message = "Failed to initialize scanner SDK.",
                  shouldRefreshStatus = true 
              });
          }
      }
      
                // ✅ STEP 1: Capture fingerprint (LED should be OFF during capture)
                _logger.LogInformation("🔴 LED OFF - Starting capture phase");
                
  var (imageData, width, height, template, errorCode, errorMessage) = _fingerprintService.CaptureFingerprintWithImage();

        if (template == null || imageData == null)
       {
        _logger.LogWarning($"Failed to capture fingerprint - {errorMessage} (Error code: {errorCode})");
   
                    // ✅ Error case: Activate RED LED immediately (error feedback is immediate)
                    _logger.LogInformation("🔴 LED RED - Capture failed, activating error feedback");
 _fingerprintService.SetLedLight("red", true);
      _fingerprintService.Beep(true);
                    
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        _fingerprintService.SetLedLight("red", false);
                        _fingerprintService.Beep(false);
                        _logger.LogInformation("🔴 LED OFF - Error feedback complete after 3 seconds");
                    });
   
    return Json(new { 
           success = false, 
   message = string.IsNullOrEmpty(errorMessage) 
     ? "Failed to capture fingerprint. Please try again." 
     : errorMessage,
   errorCode = errorCode,
   shouldRefreshStatus = true,
                        ledDuration = 3000
});
           }

                // ✅ STEP 2: SUCCESS - Convert image to Base64
  var imageBase64 = _fingerprintService.ConvertImageToBase64(imageData, width, height);
var templateBase64 = Convert.ToBase64String(template);

         _logger.LogInformation($"✅ Fingerprint captured successfully. Image: {width}x{height}, Template: {template.Length} bytes");

                // ✅ FIX: DO NOT activate LED here - let frontend control timing after UI updates
                _logger.LogInformation("⏸️ LED control deferred to frontend for synchronized timing");

          return Json(new 
    { 
   success = true, 
  templateBase64,
 imageBase64,
 imageWidth = width,
     imageHeight = height,
     shouldRefreshStatus = true,
                    ledControlDeferred = true // ✅ Signal frontend to activate LED after UI updates
});
         }
     catch (Exception ex)
     {
     _logger.LogError(ex, "Error capturing fingerprint");

                // Error feedback: red LED immediately
                _logger.LogInformation("🔴 LED RED - Exception occurred, activating error feedback");
      _fingerprintService.SetLedLight("red", true);
   _fingerprintService.Beep(true);
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    _fingerprintService.SetLedLight("red", false);
                    _fingerprintService.Beep(false);
                    _logger.LogInformation("🔴 LED OFF - Error feedback complete after exception");
                });
 
    return Json(new { 
        success = false, 
        message = "Error: " + ex.Message,
        shouldRefreshStatus = true,
                    ledDuration = 3000
    });
   }
 }

        // ✅ NEW: Separate endpoint for frontend to activate LED after UI updates
        [HttpPost("ActivateLED")]
        public IActionResult ActivateLED()
        {
            try
            {
                _logger.LogInformation("🟢 LED GREEN - Frontend requested activation after UI updates (synchronized)");
                _fingerprintService.SetLedLight("green", true);
                _fingerprintService.Beep(true);
                
                // Schedule LED turn-off after 3 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    _fingerprintService.SetLedLight("green", false);
                    _fingerprintService.Beep(false);
                    _logger.LogInformation("🟢 LED OFF - Success feedback complete after 3 seconds");
                });

                return Ok(new { success = true, message = "LED activated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating LED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: Register fingerprint
        [HttpPost("RegisterFingerprint")]
public async Task<IActionResult> RegisterFingerprint([FromBody] FingerprintRegistrationDto dto)
        {
    try
    {
   // ? Get userId from session with enhanced logging
var userId = HttpContext.Session.GetInt32("UserId");
 _logger.LogInformation($"RegisterFingerprint - UserId from session: {userId}");
 
   if (userId == null)
       {
      _logger.LogError("RegisterFingerprint - No userId in session");
     return Json(new { success = false, message = "User not logged in" });
    }

            // ? Verify user exists in users table
 _logger.LogInformation($"RegisterFingerprint - Checking if user {userId.Value} exists in database...");
 var user = await _userRepo.GetByIdAsync(userId.Value);
   
   if (user == null)
     {
       _logger.LogError($"RegisterFingerprint - User not found in database for userId: {userId.Value}");
  return Json(new { success = false, message = "User not found. Please contact administration." });
  }

 _logger.LogInformation($"RegisterFingerprint - User found: {user.UserEmail} (UserID: {user.UserId})");

 if (string.IsNullOrEmpty(dto.TemplateBase64))
    {
       _logger.LogWarning("RegisterFingerprint - No fingerprint data provided");
          return Json(new { success = false, message = "No fingerprint data provided" });
}

   // Convert base64 to byte array
      byte[] templateData = Convert.FromBase64String(dto.TemplateBase64);
 _logger.LogInformation($"RegisterFingerprint - Template data size: {templateData.Length} bytes");

     // Check if user already has a fingerprint
    var existingTemplate = await _fingerprintRepo.GetByUserIdAsync(userId.Value);
 
     if (existingTemplate != null)
      {
    _logger.LogWarning($"RegisterFingerprint - User {userId.Value} already has a registered fingerprint (TemplateId: {existingTemplate.FtempId})");
    // User already has a fingerprint - they can only update with password confirmation
  return Json(new { 
  success = false, 
        requirePasswordConfirmation = true,
      message = "You already have a registered fingerprint. To update it, please confirm your password first." 
     });
 }

     // GLOBAL DUPLICATE CHECK: Compare new fingerprint against ALL registered fingerprints
  _logger.LogInformation($"Checking for duplicate fingerprints across all users for new registration (user: {user.UserEmail})...");
        var allTemplates = await _fingerprintRepo.GetAllAsync();
  _logger.LogInformation($"RegisterFingerprint - Found {allTemplates.Count()} existing fingerprint templates to check against");

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
      $"DUPLICATE FINGERPRINT BLOCKED! User {user.UserEmail} attempted to register " +
          $"a fingerprint that is already registered to user {otherUser?.UserEmail} (Match score: {matchScore})"
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
    
      _logger.LogInformation($"No duplicate fingerprints found. Proceeding with registration for user {user.UserEmail}");

   // ? Create new template with explicit userId logging
     _logger.LogInformation($"RegisterFingerprint - Creating new template for UserId: {userId.Value}");
     var newTemplate = new FingerprintTemplate
  {
     UserId = userId.Value, // ? This MUST be a valid user_id from the users table
FtempData = templateData,
      FtempName = dto.TemplateName ?? "Primary Fingerprint",
FtempCreatedAt = DateTime.UtcNow
   };

            _logger.LogInformation($"RegisterFingerprint - Template object created. UserId: {newTemplate.UserId}, DataSize: {newTemplate.FtempData.Length}, Name: {newTemplate.FtempName}");

            // ? Attempt to insert into database with detailed logging
  _logger.LogInformation($"RegisterFingerprint - Attempting to insert template into fingerprint_templates table...");
      
            try
   {
  var templateId = await _fingerprintRepo.CreateAsync(newTemplate);

   if (templateId > 0)
   {
 // ✅ ADDED: Log audit for student fingerprint registration
    await _auditService.LogFingerprintActionAsync(
        "registered",
   dto.TemplateName ?? "Primary Fingerprint",
              isUpdate: false
     );

          _logger.LogInformation($"RegisterFingerprint - ✅ SUCCESS! Template created with ID: {templateId} for userId: {userId.Value}");
  // Success feedback
    _fingerprintService.SetLedLight("green", true);
_fingerprintService.Beep(true);
         _ = Task.Delay(300).ContinueWith(_ =>
   {
 _fingerprintService.SetLedLight("green", false);
     _fingerprintService.Beep(false);
      });
  
   _logger.LogInformation($"Fingerprint registered successfully for user {user.UserEmail}");
  return Json(new { 
      success = true, 
      message = "Fingerprint registered successfully",
      shouldRefreshStatus = true // ✅ Tell frontend to refresh status
  });
           }
         else
{
      _logger.LogError($"RegisterFingerprint - CreateAsync returned 0 or negative ID");
return Json(new { success = false, message = "Failed to save fingerprint" });
            }
  }
    catch (Exception dbEx)
   {
          // ? Catch database-specific errors
   _logger.LogError(dbEx, $"RegisterFingerprint - Database error when inserting template for userId: {userId.Value}. Error: {dbEx.Message}");
        
    // Check if it's a foreign key violation
                if (dbEx.Message.Contains("23503") || dbEx.Message.Contains("foreign key"))
      {
           _logger.LogError($"RegisterFingerprint - FOREIGN KEY VIOLATION! UserId {userId.Value} does not exist in users table!");
       return Json(new { success = false, message = $"Database error: User ID {userId.Value} is invalid. Please contact support." });
      }
  
           return Json(new { success = false, message = $"Database error: {dbEx.Message}" });
     }
}
catch (Exception ex)
  {
_logger.LogError(ex, $"RegisterFingerprint - Unexpected error for userId: {HttpContext.Session.GetInt32("UserId")}");

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
  // ✅ ADDED: Log audit for student fingerprint update
  await _auditService.LogActionAsync(
"Fingerprint Update (Student)",
$"Student {user.UserEmail} updated fingerprint biometric data (Old: {oldTemplateName}, New: {dto.TemplateName})"
       );

// Success feedback: green light + beep sound
        _fingerprintService.SetLedLight("green", true);
   _fingerprintService.Beep(true);
      _ = Task.Delay(300).ContinueWith(_ =>
{
_fingerprintService.SetLedLight("green", false);
   _fingerprintService.Beep(false);
    });
     
  _logger.LogInformation($"Fingerprint updated successfully for user {user.UserEmail}");
 return Json(new { 
     success = true, 
     message = "Fingerprint updated successfully",
     shouldRefreshStatus = true // ✅ Tell frontend to refresh status
 });
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

   // Get template name before deletion for audit log
 var existingTemplate = await _fingerprintRepo.GetByUserIdAsync(userId.Value);
      var templateName = existingTemplate?.FtempName ?? "Unknown";

    var deleted = await _fingerprintRepo.DeleteByUserIdAsync(userId.Value);
     
       if (deleted)
   {
   // ✅ ADDED: Log audit for student fingerprint deletion
            var user = await _userRepo.GetByIdAsync(userId.Value);
       await _auditService.LogActionAsync(
    "Fingerprint Deletion (Student)",
       $"Student {user?.UserEmail} deleted fingerprint biometric data (Template: {templateName})"
  );

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
