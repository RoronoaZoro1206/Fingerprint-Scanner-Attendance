using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using Fingerprint_Attendance_System.Services;

namespace Fingerprint_Attendance_System.Controllers.Auth
{
    public class FingerprintAuthController : Controller
    {
        private readonly FingerprintService _fingerprintService;
        private readonly IFingerprintTemplateRepository _fingerprintRepo;
        private readonly IUserRepository _userRepo;
    private readonly IScanLogRepository _scanLogRepo;
        private readonly IAdministrationAuditRepository _auditRepo; // NEW: For audit logging
        private readonly IAdministrationRepository _adminRepo; // NEW: To get admin ID
        private readonly ILogger<FingerprintAuthController> _logger;

        public FingerprintAuthController(
  FingerprintService fingerprintService,
 IFingerprintTemplateRepository fingerprintRepo,
            IUserRepository userRepo,
     IScanLogRepository scanLogRepo,
       IAdministrationAuditRepository auditRepo, // NEW: Inject audit repository
            IAdministrationRepository adminRepo, // NEW: Inject admin repository
   ILogger<FingerprintAuthController> logger)
    {
            _fingerprintService = fingerprintService;
   _fingerprintRepo = fingerprintRepo;
        _userRepo = userRepo;
        _scanLogRepo = scanLogRepo;
            _auditRepo = auditRepo; // NEW: Store audit repository
            _adminRepo = adminRepo; // NEW: Store admin repository
            _logger = logger;
        }

        // POST: Authenticate user with fingerprint
        [HttpPost]
        public async Task<IActionResult> AuthenticateWithFingerprint()
     {
        try
         {
                _logger.LogInformation("Starting fingerprint authentication...");

          // Ensure device is initialized and turned on
    EnsureDeviceReady();

       // Capture fingerprint template from scanner
        var capturedTemplate = _fingerprintService.CaptureTemplate();
        
        if (capturedTemplate == null)
        {
            _logger.LogWarning("Failed to capture fingerprint template - no finger detected");
            
       // Log failed scan attempt
         await _scanLogRepo.AddAsync(new ScanLog
            {
    UserId = null,
       ScanTime = DateTime.Now,
       ScanSuccess = false,
     ScanLogMessage = "Failed to capture fingerprint - no finger detected",
        ScanCreatedAt = DateTime.Now
            });
            
      // Error feedback: red light + beep
            _fingerprintService.SetLedLight("red", true);
            _fingerprintService.Beep(true);
            await Task.Delay(500);
    _fingerprintService.SetLedLight("red", false);
            _fingerprintService.Beep(false);
     
 return Json(new { 
     success = false, 
                message = "No fingerprint detected. Please place your finger firmly on the scanner." 
      });
        }

        // ✅ CRITICAL FIX: Turn OFF the scanner's default green LED immediately after capture
        // This prevents the hardware from showing green before verification is complete
        _fingerprintService.TurnOffAllLeds();
        
        // ✅ ENHANCED: Add small delay to ensure LED off command is processed by hardware
        await Task.Delay(100);
        
        _logger.LogInformation("✅ Scanner LEDs disabled - starting verification phase...");

        _logger.LogInformation("Fingerprint captured successfully, matching against database templates...");

    // Get all registered fingerprint templates from database
        var allTemplates = await _fingerprintRepo.GetAllAsync();
        
        if (!allTemplates.Any())
        {
            _logger.LogWarning("No registered fingerprints found in database");
          
            // Log failed scan attempt
            await _scanLogRepo.AddAsync(new ScanLog
     {
      UserId = null,
          ScanTime = DateTime.Now,
        ScanSuccess = false,
  ScanLogMessage = "No registered fingerprints in system",
ScanCreatedAt = DateTime.Now
            });
            
 // Error feedback: red light + beep
_fingerprintService.SetLedLight("red", true);
            _fingerprintService.Beep(true);
            await Task.Delay(500);
 _fingerprintService.SetLedLight("red", false);
       _fingerprintService.Beep(false);
          
     return Json(new { 
       success = false, 
         message = "No registered fingerprints found in the system. Please register first." 
       });
        }

    _logger.LogInformation($"Comparing captured fingerprint against {allTemplates.Count()} registered templates...");

                // Match against all templates using GetMatchScore for 1:1 comparison
       int matchThreshold = 50; // Security threshold (adjust based on requirements)
        FingerprintTemplate? matchedTemplate = null;
 int bestScore = 0;

   foreach (var template in allTemplates)
      {
         try
    {
              // Get match score between captured template and stored template
int score = _fingerprintService.GetMatchScore(capturedTemplate, template.FtempData);
        
        _logger.LogInformation($"Match score for user {template.UserId}: {score}");
           
   if (score > bestScore && score >= matchThreshold)
  {
      bestScore = score;
   matchedTemplate = template;
            }
      }
   catch (Exception ex)
              {
     _logger.LogWarning(ex, $"Error matching template for user {template.UserId}");
   }
      }

     if (matchedTemplate != null)
      {
     _logger.LogInformation($"✅ Fingerprint matched! User ID: {matchedTemplate.UserId}, Score: {bestScore}");
         
       // Get user details from database
          var user = await _userRepo.GetByIdAsync(matchedTemplate.UserId);
   
      if (user == null)
        {
        _logger.LogError($"User not found for ID: {matchedTemplate.UserId}");
             
      // Log failed scan
          await _scanLogRepo.AddAsync(new ScanLog
             {
   UserId = matchedTemplate.UserId,
ScanTime = DateTime.Now,
            ScanSuccess = false,
   ScanLogMessage = "User account not found",
            ScanCreatedAt = DateTime.Now
       });
  
         // Error feedback: red light + beep
 _fingerprintService.SetLedLight("red", true);
           _fingerprintService.Beep(true);
          await Task.Delay(500);
       _fingerprintService.SetLedLight("red", false);
     _fingerprintService.Beep(false);
 
  return Json(new { 
     success = false, 
    message = "User account not found. Please contact administration." 
   });
   }

         // Check if account is active
            if (!user.IsActive)
    {
      _logger.LogWarning($"Inactive account attempted login: {user.UserEmail}");
 
           // Log failed scan
          await _scanLogRepo.AddAsync(new ScanLog
        {
   UserId = user.UserId,
   ScanTime = DateTime.Now,
     ScanSuccess = false,
   ScanLogMessage = $"Inactive account login attempt - {user.UserEmail}",
      ScanCreatedAt = DateTime.Now
   });
  
                  // Error feedback: red light + beep
      _fingerprintService.SetLedLight("red", true);
             _fingerprintService.Beep(true);
             await Task.Delay(500);
                 _fingerprintService.SetLedLight("red", false);
       _fingerprintService.Beep(false);
     
                   return Json(new { 
 success = false, 
     message = "Your account has been deactivated. Please contact administration." 
     });
         }

       // Update last login timestamp
          await _userRepo.UpdateLastLoginAsync(user.UserId);

    // Log successful authentication in scan_logs
         await _scanLogRepo.AddAsync(new ScanLog
               {
   UserId = user.UserId,
      ScanTime = DateTime.Now,
        ScanSuccess = true,
ScanLogMessage = $"Successful biometric login - {user.UserEmail} (Match Score: {bestScore})",
   ScanCreatedAt = DateTime.Now
        });

    // NEW: Create audit log entry for biometric login
     // Check if user is an admin and create audit entry
            var admin = await _adminRepo.GetByUserIdAsync(user.UserId);
            if (admin != null)
            {
 // User is an admin - create audit log entry
      var auditEntry = new AdministrationAudit
{
    AdminId = admin.AdminId,
 AudAction = "Biometric Login",
     AudDetails = $"Successful biometric login - {user.UserEmail} (Match Score: {bestScore})",
   AudActionTime = DateTime.Now
          };

                var auditCreated = await _auditRepo.AddAsync(auditEntry);
         if (auditCreated)
    {
     _logger.LogInformation($"Audit log created for biometric login: {user.UserEmail}");
    }
    else
  {
        _logger.LogWarning($"Failed to create audit log for biometric login: {user.UserEmail}");
   }
            }

    _logger.LogInformation($"✅ Successful login for user: {user.UserEmail}");

       // Create user session
        HttpContext.Session.SetInt32("UserId", user.UserId);
  HttpContext.Session.SetInt32("RoleId", user.RoleId);
       HttpContext.Session.SetString("UserEmail", user.UserEmail);
 HttpContext.Session.SetString("UserProfileImage", user.UserProfileImage);
         HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");
     HttpContext.Session.SetString("justLoggedIn", "true");

      // ✅ CRITICAL FIX: Wait 1.5 seconds to match frontend's "Validating identity..." delay
     // This ensures the physical green LED only activates when the UI shows success message
      _logger.LogInformation("⏳ Waiting 1.5 seconds to synchronize LED with frontend validation display...");
      await Task.Delay(1500);

      // ✅ SUCCESS: ONLY NOW activate the physical scanner's green LED
     // This is triggered AFTER the frontend validation delay, synchronizing hardware and UI
      _logger.LogInformation("✅ Validation delay complete - activating green LED on physical scanner hardware");
     _fingerprintService.SetLedLight("green", true);
  _fingerprintService.Beep(true);
            await Task.Delay(300);
        _fingerprintService.SetLedLight("green", false);
        _fingerprintService.Beep(false);

        // Determine redirect URL based on user role
 string redirectUrl = user.RoleType switch
        {
      USER_ROLES.ADMIN => Url.Action("Index", "Admin") ?? "/Admin",
          USER_ROLES.FACULTY => Url.Action("Index", "Faculty") ?? "/Faculty",
    USER_ROLES.STUDENT => Url.Action("Index", "Student") ?? "/Student",
    _ => Url.Action("Index", "Home") ?? "/Home"
        };

      return Json(new { 
 success = true, 
           message = "Authentication successful! Welcome back.", 
        redirectUrl = redirectUrl,
     matchScore = bestScore,
              userName = $"{user.UserFirstName} {user.UserLastName}"
   });
    }
            else
           {
 _logger.LogWarning($"❌ No matching fingerprint found. Best score: {bestScore} (Threshold: {matchThreshold})");
    
   // Log failed authentication
          await _scanLogRepo.AddAsync(new ScanLog
                 {
    UserId = null,
        ScanTime = DateTime.Now,
        ScanSuccess = false,
         ScanLogMessage = $"Fingerprint not recognized (Best Score: {bestScore}, Threshold: {matchThreshold})",
         ScanCreatedAt = DateTime.Now
          });
 
                 // Error feedback: red light + beep
   _fingerprintService.SetLedLight("red", true);
        _fingerprintService.Beep(true);
    await Task.Delay(500);
         _fingerprintService.SetLedLight("red", false);
        _fingerprintService.Beep(false);

          return Json(new { 
          success = false, 
   message = "Fingerprint not recognized. This fingerprint is not registered in the system.<br>Please try again or contact administration." 
         });
     }
         }
      catch (Exception ex)
 {
   _logger.LogError(ex, "Error during fingerprint authentication");

         // Log error
        await _scanLogRepo.AddAsync(new ScanLog
     {
                 UserId = null,
    ScanTime = DateTime.Now,
  ScanSuccess = false,
   ScanLogMessage = $"Authentication error: {ex.Message}",
    ScanCreatedAt = DateTime.Now
                });

     // Error feedback: red light + beep
            _fingerprintService.SetLedLight("red", true);
_fingerprintService.Beep(true);
             await Task.Delay(500);
  _fingerprintService.SetLedLight("red", false);
             _fingerprintService.Beep(false);
              
                return Json(new { 
         success = false, 
       message = "Authentication error. Please try again or contact support." 
   });
  }
        }

        // GET: Check if scanner is available
    [HttpGet]
        public IActionResult CheckScannerStatus()
        {
            try
       {
      // Ensure device is initialized and turned on
     EnsureDeviceReady();
      
     var status = _fingerprintService.GetStatus();
        var isAvailable = _fingerprintService.IsDeviceConnected;

    return Json(new { 
  success = true, 
        isAvailable = isAvailable,
   status = status
  });
            }
            catch (Exception ex)
        {
    _logger.LogError(ex, "Error checking scanner status");
      return Json(new { 
    success = false, 
        isAvailable = false, 
     status = "Error", 
       message = ex.Message 
     });
        }
   }
        
        /// <summary>
/// Ensures the fingerprint device is initialized and ready
        /// Matches the initialization flow from AdminRegisterFingerprintController
        /// </summary>
        private void EnsureDeviceReady()
        {
            try
            {
      // Check if already initialized and connected
           if (_fingerprintService.IsDeviceConnected)
           {
     _logger.LogInformation("Device already initialized and connected");
              return;
      }

 // Initialize SDK if not already done
       var status = _fingerprintService.GetStatus();
          if (status == "Not Initialized")
{
         _logger.LogInformation("Initializing fingerprint SDK...");
   var initialized = _fingerprintService.Initialize();
          if (!initialized)
  {
       throw new Exception("Failed to initialize fingerprint SDK");
              }
     }

  // Turn on device if not already on
              if (!_fingerprintService.IsDeviceConnected)
          {
     _logger.LogInformation("Turning on fingerprint device...");
             var deviceOn = _fingerprintService.TurnOnDevice();
        if (!deviceOn)
 {
           throw new Exception("Failed to turn on fingerprint device");
      }
}

    _logger.LogInformation("? Device is ready");
    }
        catch (Exception ex)
         {
           _logger.LogError(ex, "Error ensuring device is ready");
           throw;
            }
      }
    }
}
