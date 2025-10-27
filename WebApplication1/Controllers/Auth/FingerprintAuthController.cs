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
        private readonly ILogger<FingerprintAuthController> _logger;

        public FingerprintAuthController(
 FingerprintService fingerprintService,
      IFingerprintTemplateRepository fingerprintRepo,
            IUserRepository userRepo,
       IScanLogRepository scanLogRepo,
            ILogger<FingerprintAuthController> logger)
   {
  _fingerprintService = fingerprintService;
      _fingerprintRepo = fingerprintRepo;
        _userRepo = userRepo;
   _scanLogRepo = scanLogRepo;
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

  // Capture fingerprint
        var capturedTemplate = _fingerprintService.CaptureTemplate();
      
 if (capturedTemplate == null)
          {
     _logger.LogWarning("Failed to capture fingerprint template");
    
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
        
 return Json(new { success = false, message = "No fingerprint detected. Please scan your fingerprint." });
     }

     _logger.LogInformation("Fingerprint captured successfully");

    // Get all registered fingerprint templates
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
     
        return Json(new { success = false, message = "No registered fingerprints found in the system." });
     }

      _logger.LogInformation($"Comparing against {allTemplates.Count()} registered fingerprints");

        // Match against all templates using DBMatch for 1:1 comparison
        int matchThreshold = 50; // Adjust based on your security requirements
     FingerprintTemplate? matchedTemplate = null;
     int bestScore = 0;

      foreach (var template in allTemplates)
        {
         try
  {
            // Use DBMatch for 1:1 comparison
   int score = _fingerprintService.MatchFingerprints(capturedTemplate, template.FtempData, 0) ? 
      _fingerprintService.GetMatchScore(capturedTemplate, template.FtempData) : 0;
 
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
   _logger.LogInformation($"Fingerprint matched! User ID: {matchedTemplate.UserId}, Score: {bestScore}");
      
      // Get user details
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
          
      return Json(new { success = false, message = "User account not found." });
    }

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
    
       return Json(new { success = false, message = "Your account has been deactivated. Please contact administration." });
     }

     // Update last login timestamp
await _userRepo.UpdateLastLoginAsync(user.UserId);

     // Log successful scan
await _scanLogRepo.AddAsync(new ScanLog
     {
        UserId = user.UserId,
  ScanTime = DateTime.Now,
 ScanSuccess = true,
     ScanLogMessage = $"Successful biometric login - {user.UserEmail} (Match Score: {bestScore})",
    ScanCreatedAt = DateTime.Now
    });

        _logger.LogInformation($"Successful login for user: {user.UserEmail}");

        // Store user information in session
 HttpContext.Session.SetInt32("UserId", user.UserId);
         HttpContext.Session.SetInt32("RoleId", user.RoleId);
  HttpContext.Session.SetString("UserEmail", user.UserEmail);
            HttpContext.Session.SetString("UserProfileImage", user.UserProfileImage);
  HttpContext.Session.SetString("UserName", $"{user.UserFirstName} {user.UserLastName}");

       // Success feedback: green light + beep
     _fingerprintService.SetLedLight("green", true);
        _fingerprintService.Beep(true);
       await Task.Delay(300);
      _fingerprintService.SetLedLight("green", false);
   _fingerprintService.Beep(false);

    // Store login flag for fingerprint registration prompt
         HttpContext.Session.SetString("justLoggedIn", "true");

               // Determine redirect URL based on role
  string redirectUrl = user.RoleType switch
    {
       USER_ROLES.ADMIN => Url.Action("Index", "Admin") ?? "/Admin",
    USER_ROLES.FACULTY => Url.Action("Index", "Faculty") ?? "/Faculty",
 USER_ROLES.STUDENT => Url.Action("Index", "Student") ?? "/Student",
 _ => Url.Action("Index", "Home") ?? "/Home"
         };

       return Json(new { 
        success = true, 
            message = $"? Scan successful! Redirecting to your account...", 
   redirectUrl = redirectUrl,
      matchScore = bestScore,
   userName = $"{user.UserFirstName} {user.UserLastName}"
     });
   }
     else
         {
     _logger.LogWarning($"No matching fingerprint found. Best score: {bestScore}");
 
       // Log failed scan
    await _scanLogRepo.AddAsync(new ScanLog
       {
UserId = null,
  ScanTime = DateTime.Now,
        ScanSuccess = false,
          ScanLogMessage = $"Fingerprint not recognized (Best Score: {bestScore})",
   ScanCreatedAt = DateTime.Now
   });
         
      // No match found - error feedback: red light + beep
 _fingerprintService.SetLedLight("red", true);
  _fingerprintService.Beep(true);
      await Task.Delay(500);
              _fingerprintService.SetLedLight("red", false);
      _fingerprintService.Beep(false);

         return Json(new { 
  success = false, 
         message = "? Fingerprint not recognized. This fingerprint is not registered in the system." 
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
  
            return Json(new { success = false, message = "? Authentication error. Please try again." });
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
        return Json(new { success = false, isAvailable = false, status = "Error", message = ex.Message });
  }
   }
   
        /// <summary>
 /// Ensures the fingerprint device is initialized and ready
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

         // Initialize if not already done
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

                _logger.LogInformation("Device is ready");
   }
    catch (Exception ex)
      {
    _logger.LogError(ex, "Error ensuring device is ready");
    throw;
            }
     }
    }
}
