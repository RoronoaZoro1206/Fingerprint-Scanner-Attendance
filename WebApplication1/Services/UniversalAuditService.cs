using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.Enums;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Universal Audit Service - Logs actions for ALL user roles (Admin, Faculty, Student)
    /// All logs are stored in administration_audit table for centralized tracking
    /// </summary>
    public class UniversalAuditService
    {
        private readonly IAdministrationAuditRepository _auditRepo;
        private readonly IAdministrationRepository _adminRepo;
        private readonly IFacultyRepository _facultyRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IUserRepository _userRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
      private readonly ILogger<UniversalAuditService> _logger;

        public UniversalAuditService(
       IAdministrationAuditRepository auditRepo,
            IAdministrationRepository adminRepo,
            IFacultyRepository facultyRepo,
            IStudentRepository studentRepo,
            IUserRepository userRepo,
    IHttpContextAccessor httpContextAccessor,
            ILogger<UniversalAuditService> logger)
        {
   _auditRepo = auditRepo;
 _adminRepo = adminRepo;
            _facultyRepo = facultyRepo;
            _studentRepo = studentRepo;
  _userRepo = userRepo;
            _httpContextAccessor = httpContextAccessor;
 _logger = logger;
        }

   /// <summary>
        /// Logs an action for any user role (Admin, Faculty, or Student)
        /// Automatically determines the admin_id based on user role
        /// </summary>
        public async Task LogActionAsync(string action, string? details = null)
        {
            try
 {
          var userId = GetCurrentUserId();
         if (!userId.HasValue)
         {
            _logger.LogWarning("Audit log skipped: No user ID found in session");
  return;
            }

       // Get user to determine role
     var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null)
     {
                 _logger.LogWarning($"Audit log skipped: User not found for ID {userId.Value}");
      return;
       }

                // Determine admin_id based on user role
int? adminId = await GetAdminIdForUser(userId.Value, user.RoleType);

        if (!adminId.HasValue)
                {
         _logger.LogWarning($"Audit log skipped: Could not determine admin_id for user {userId.Value} (Role: {user.RoleType})");
           return;
      }

            // Create audit log entry
                var audit = new AdministrationAudit
         {
         AdminId = adminId.Value,
      AudAction = action,
      AudDetails = details,
      AudActionTime = DateTime.Now
        };

      var result = await _auditRepo.AddAsync(audit);

                if (result)
       {
  _logger.LogInformation($"? Audit logged: [{user.RoleType}] {action} by user {user.UserEmail}");
         }
     else
                {
       _logger.LogError($"? Failed to log audit: {action}");
        }
    }
            catch (Exception ex)
            {
       _logger.LogError(ex, $"Error logging audit action: {action}");
 // Silently fail to not disrupt the main operation
     }
     }

    /// <summary>
        /// Gets the admin_id for a user based on their role
      /// - Admin: Use their admin_id from administration table
        /// - Faculty/Student: Use a system admin_id (first admin) as proxy
        /// </summary>
        private async Task<int?> GetAdminIdForUser(int userId, USER_ROLES role)
  {
            try
        {
                switch (role)
      {
   case USER_ROLES.ADMIN:
        // For admins, get their admin_id directly
        var admins = await _adminRepo.GetAllAsync();
   var admin = admins.FirstOrDefault(a => a.UserId == userId);
 return admin?.AdminId;

     case USER_ROLES.FACULTY:
   case USER_ROLES.STUDENT:
       // For faculty and students, use the first system admin as proxy
         // This ensures all logs are tracked centrally
    var systemAdmins = await _adminRepo.GetAllAsync();
       var firstAdmin = systemAdmins.OrderBy(a => a.AdminId).FirstOrDefault();
    
            if (firstAdmin == null)
     {
          _logger.LogError("No system admin found to log faculty/student actions");
         return null;
   }

            _logger.LogInformation($"Using system admin (ID: {firstAdmin.AdminId}) as proxy for {role} user (ID: {userId})");
   return firstAdmin.AdminId;

      default:
     _logger.LogWarning($"Unknown role: {role}");
          return null;
       }
          }
catch (Exception ex)
    {
                _logger.LogError(ex, $"Error getting admin_id for user {userId}");
           return null;
            }
      }

        /// <summary>
        /// Logs an action with detailed user information (for enhanced tracking)
        /// </summary>
        public async Task LogActionWithUserDetailsAsync(string action, int userId, USER_ROLES role, string? additionalDetails = null)
        {
            try
            {
           var user = await _userRepo.GetByIdAsync(userId);
    if (user == null)
        {
          _logger.LogWarning($"User not found for ID {userId}");
     return;
          }

        string details = $"User: {user.UserEmail} (ID: {userId}, Role: {role})";
              if (!string.IsNullOrEmpty(additionalDetails))
  {
    details += $" | {additionalDetails}";
      }

 await LogActionAsync(action, details);
            }
   catch (Exception ex)
   {
  _logger.LogError(ex, $"Error logging action with user details for user {userId}");
       }
        }

        /// <summary>
        /// Logs a fingerprint-related action
        /// </summary>
   public async Task LogFingerprintActionAsync(string actionType, string templateName, bool isUpdate = false)
   {
          var userId = GetCurrentUserId();
     if (!userId.HasValue) return;

         var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null) return;

          string action = isUpdate ? $"Fingerprint Update ({user.RoleType})" : $"Fingerprint Registration ({user.RoleType})";
     string details = $"{user.UserEmail} {actionType} fingerprint: {templateName}";

       await LogActionAsync(action, details);
        }

        /// <summary>
        /// Logs a profile submission action (for students)
        /// </summary>
        public async Task LogProfileSubmissionAsync(int studentId, string programName, int yearLevel)
        {
            var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

            var user = await _userRepo.GetByIdAsync(userId.Value);
      if (user == null) return;

    string details = $"Student {user.UserEmail} (ID: {studentId}) submitted profile - Program: {programName}, Year: {yearLevel}";
            await LogActionAsync("Student Profile Submission", details);
        }

        /// <summary>
        /// Logs an enrollment action
    /// </summary>
        public async Task LogEnrollmentActionAsync(string courseCode, string courseName, bool isEnrollment)
        {
          var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

      var user = await _userRepo.GetByIdAsync(userId.Value);
     if (user == null) return;

         string action = isEnrollment ? "Class Enrollment" : "Class Unenrollment";
            string details = $"Student {user.UserEmail} {(isEnrollment ? "enrolled in" : "unenrolled from")} {courseCode}: {courseName}";

        await LogActionAsync(action, details);
        }

     /// <summary>
    /// Logs an attendance marking action (for faculty)
      /// </summary>
        public async Task LogAttendanceActionAsync(string courseCode, DateTime date, int studentCount, string statusCounts)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

  var user = await _userRepo.GetByIdAsync(userId.Value);
    if (user == null) return;

            string details = $"Faculty {user.UserEmail} marked attendance for {courseCode} on {date:yyyy-MM-dd} - {studentCount} students | {statusCounts}";
      await LogActionAsync("Attendance Marking", details);
        }

        /// <summary>
        /// Logs a class management action (admin)
    /// </summary>
        public async Task LogClassManagementAsync(string action, string courseCode, string facultyName, string roomCode)
        {
            var details = $"{action} class: {courseCode} | Faculty: {facultyName} | Room: {roomCode}";
     await LogActionAsync($"Class {action}", details);
     }

        private int? GetCurrentUserId()
  {
            return _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
        }
    }
}
