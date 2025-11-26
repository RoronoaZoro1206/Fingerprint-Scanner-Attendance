using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Background service that periodically checks for stale sessions and logs auto-logouts.
    /// This ensures that even if the browser beacon fails, we still detect closed tabs.
    /// </summary>
    public class SessionCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<SessionCleanupBackgroundService> _logger;
        private readonly SessionHeartbeatService _heartbeatService;
        private readonly IServiceScopeFactory _scopeFactory;
        
        // Check for stale sessions every 5 seconds
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
        
        public SessionCleanupBackgroundService(
            ILogger<SessionCleanupBackgroundService> logger,
            SessionHeartbeatService heartbeatService,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _heartbeatService = heartbeatService;
            _scopeFactory = scopeFactory;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Session Cleanup Background Service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessStaleSessions();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stale sessions");
                }
                
                await Task.Delay(CheckInterval, stoppingToken);
            }
            
            _logger.LogInformation("🛑 Session Cleanup Background Service stopped");
        }

        private async Task ProcessStaleSessions()
        {
            var staleSessions = _heartbeatService.GetAndRemoveStaleSessions();
            
            if (staleSessions.Count == 0)
            {
                return;
            }
            
            _logger.LogInformation($"🔍 Found {staleSessions.Count} stale session(s) to process");
            
            // Create a scope to get scoped services (like repositories)
            using var scope = _scopeFactory.CreateScope();
            var authAuditService = scope.ServiceProvider.GetRequiredService<AuthenticationAuditService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            
            var successCount = 0;
            var failCount = 0;
            
            foreach (var session in staleSessions)
            {
                try
                {
                    // Get user role from database
                    var user = await userRepository.GetByIdAsync(session.UserId);
                    var roleName = user?.RoleType.ToString().ToUpper() ?? "UNKNOWN";
                    
                    var details = $"User {session.UserEmail} (ID: {session.UserId}, Role: {roleName}) automatically logged out after {session.GetSessionDuration()} session - Browser/tab closed (Last activity: {session.GetLastActivityTime()}, {session.BrowserInfo})";
                    
                    await authAuditService.LogAutoLogoutAsync(
                        session.UserId,
                        session.UserEmail,
                        roleName,
                        details
                    );
                    
                    successCount++;
                    _logger.LogInformation($"✅ Auto logout logged for stale session - User: {session.UserEmail}");
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, $"Failed to log auto logout for session - User: {session.UserEmail}");
                    // Continue processing other sessions even if one fails
                }
            }
            
            if (failCount > 0)
            {
                _logger.LogWarning($"⚠️ Session cleanup completed: {successCount} succeeded, {failCount} failed");
            }
        }
    }
}
