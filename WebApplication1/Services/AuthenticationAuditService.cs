using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Authentication Audit Service - Logs ALL authentication events for ALL user roles
    /// Replaces UniversalAuditService for authentication-specific logging
    /// </summary>
    public class AuthenticationAuditService
    {
        private readonly IAuthenticationAuditRepository _authAuditRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthenticationAuditService> _logger;

        public AuthenticationAuditService(
            IAuthenticationAuditRepository authAuditRepo,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthenticationAuditService> logger)
        {
            _authAuditRepo = authAuditRepo;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Logs a password login event
        /// </summary>
        public async Task LogPasswordLoginAsync(int userId, string userEmail, string role)
        {
            await LogAuthenticationEventAsync(
                userId,
                "Password Login",
                $"User {userEmail} (ID: {userId}, Role: {role}) logged in via password authentication"
            );
        }

        /// <summary>
        /// Logs a biometric login event
        /// </summary>
        public async Task LogBiometricLoginAsync(int userId, string userEmail, string role, int matchScore)
        {
            await LogAuthenticationEventAsync(
                userId,
                "Biometric Login",
                $"User {userEmail} (ID: {userId}, Role: {role}) logged in using fingerprint authentication (Match Score: {matchScore})"
            );
        }

        /// <summary>
        /// Logs a failed login attempt
        /// </summary>
        public async Task LogFailedLoginAsync(int? userId, string email, string reason)
        {
            await LogAuthenticationEventAsync(
                userId ?? 0,
                "Failed Login",
                $"Failed login attempt for {email} - {reason}"
            );
        }

        /// <summary>
        /// Logs a logout event
        /// </summary>
        public async Task LogLogoutAsync(int userId, string userEmail, string role)
        {
            await LogAuthenticationEventAsync(
                userId,
                "User Logout",
                $"User {userEmail} (ID: {userId}, Role: {role}) logged out"
            );
        }

        /// <summary>
        /// Logs an auto logout event (when user closes browser/tab without clicking logout)
        /// </summary>
        public async Task LogAutoLogoutAsync(int userId, string userEmail, string role, string reason)
        {
            _logger.LogInformation($"🚨 LogAutoLogoutAsync called - UserId: {userId}, Email: {userEmail}, Role: {role}");
            
            try
            {
                await LogAuthenticationEventAsync(
                    userId,
                    "Auto Logout",
                    reason
                );
                
                _logger.LogInformation($"✅ Auto logout event logged successfully for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to log auto logout for user {userId}");
                throw; // Re-throw to ensure error is visible
            }
        }

        /// <summary>
        /// Core method to log authentication events
        /// </summary>
        private async Task LogAuthenticationEventAsync(int userId, string action, string details)
        {
            try
            {
                _logger.LogInformation($"📝 LogAuthenticationEventAsync - Action: {action}, UserId: {userId}");
                
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

                _logger.LogInformation($"   IP: {ipAddress}, UserAgent: {userAgent?.Substring(0, Math.Min(50, userAgent?.Length ?? 0))}...");

                var audit = new AuthenticationAudit
                {
                    UserId = userId,
                    AuthAction = action,
                    AuthDetails = details,
                    AuthActionTime = DateTime.Now,
                    AuthIpAddress = ipAddress,
                    AuthUserAgent = userAgent
                };

                _logger.LogInformation($"📦 Audit object created, calling repository...");
                var result = await _authAuditRepo.AddAsync(audit);

                if (result)
                {
                    _logger.LogInformation($"✅ Authentication audit logged successfully: {action} for user ID {userId}");
                }
                else
                {
                    _logger.LogError($"❌ Failed to log authentication audit: {action} for user ID {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Exception in LogAuthenticationEventAsync: {action} for user ID {userId}");
                // Silently fail to not disrupt the main authentication flow
            }
        }

        /// <summary>
        /// Get recent authentication events for a specific user
        /// </summary>
        public async Task<IEnumerable<AuthenticationAudit>> GetUserAuthenticationHistoryAsync(int userId, int count = 10)
        {
            try
            {
                var allUserEvents = await _authAuditRepo.GetByUserIdAsync(userId);
                return allUserEvents.Take(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving authentication history for user {userId}");
                return Enumerable.Empty<AuthenticationAudit>();
            }
        }

        /// <summary>
        /// Get recent authentication events across all users
        /// </summary>
        public async Task<IEnumerable<AuthenticationAudit>> GetRecentAuthenticationEventsAsync(int count = 50)
        {
            try
            {
                return await _authAuditRepo.GetRecentAsync(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent authentication events");
                return Enumerable.Empty<AuthenticationAudit>();
            }
        }

        /// <summary>
        /// Get authentication events by action type
        /// </summary>
        public async Task<IEnumerable<AuthenticationAudit>> GetEventsByActionAsync(string action)
        {
            try
            {
                return await _authAuditRepo.GetByActionAsync(action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving authentication events for action: {action}");
                return Enumerable.Empty<AuthenticationAudit>();
            }
        }
    }
}
