using System.Collections.Concurrent;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Service to track active user sessions via heartbeats.
    /// When heartbeats stop, the session is considered closed (browser/tab closed).
    /// </summary>
    public class SessionHeartbeatService
    {
        private readonly ILogger<SessionHeartbeatService> _logger;
        
        // Thread-safe dictionary to track active sessions
        // Key: TabId, Value: SessionInfo
        private readonly ConcurrentDictionary<string, SessionInfo> _activeSessions = new();
        
        // How long before a session is considered stale (no heartbeat received)
        public static readonly TimeSpan SessionTimeout = TimeSpan.FromSeconds(10);
        
        public SessionHeartbeatService(ILogger<SessionHeartbeatService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Record a heartbeat from a client tab
        /// </summary>
        public void RecordHeartbeat(string tabId, int userId, string userEmail, string browserInfo)
        {
            var sessionInfo = new SessionInfo
            {
                TabId = tabId,
                UserId = userId,
                UserEmail = userEmail,
                BrowserInfo = browserInfo,
                LastHeartbeat = DateTime.UtcNow,
                SessionStartTime = DateTime.UtcNow
            };
            
            _activeSessions.AddOrUpdate(
                tabId,
                sessionInfo,
                (key, existing) =>
                {
                    existing.LastHeartbeat = DateTime.UtcNow;
                    existing.BrowserInfo = browserInfo;
                    return existing;
                }
            );
            
            _logger.LogDebug($"💓 Heartbeat recorded - Tab: {tabId}, User: {userEmail}");
        }

        /// <summary>
        /// Remove a session (called when user explicitly logs out)
        /// </summary>
        public void RemoveSession(string tabId)
        {
            if (_activeSessions.TryRemove(tabId, out var session))
            {
                _logger.LogInformation($"🚪 Session removed - Tab: {tabId}, User: {session.UserEmail}");
            }
        }
        
        /// <summary>
        /// Remove all sessions for a user (called when user logs out from any tab)
        /// </summary>
        public void RemoveUserSessions(int userId)
        {
            var userSessions = _activeSessions.Where(s => s.Value.UserId == userId).ToList();
            foreach (var session in userSessions)
            {
                _activeSessions.TryRemove(session.Key, out _);
                _logger.LogInformation($"🚪 Session removed for user logout - Tab: {session.Key}, User: {session.Value.UserEmail}");
            }
        }
        
        /// <summary>
        /// Get all stale sessions (sessions that haven't sent a heartbeat within the timeout period)
        /// Thread-safe implementation using snapshot of keys
        /// </summary>
        public List<SessionInfo> GetAndRemoveStaleSessions()
        {
            var staleSessions = new List<SessionInfo>();
            var cutoffTime = DateTime.UtcNow - SessionTimeout;
            
            // Take a snapshot of keys to avoid collection modification during iteration
            var keys = _activeSessions.Keys.ToList();
            
            foreach (var key in keys)
            {
                if (_activeSessions.TryGetValue(key, out var sessionInfo) && 
                    sessionInfo.LastHeartbeat < cutoffTime)
                {
                    if (_activeSessions.TryRemove(key, out var removedSession))
                    {
                        staleSessions.Add(removedSession);
                        _logger.LogInformation($"⏰ Stale session detected - Tab: {key}, User: {removedSession.UserEmail}, Last heartbeat: {removedSession.LastHeartbeat:HH:mm:ss}");
                    }
                }
            }
            
            return staleSessions;
        }
        
        /// <summary>
        /// Get count of active sessions
        /// </summary>
        public int GetActiveSessionCount() => _activeSessions.Count;
        
        /// <summary>
        /// Check if a specific tab has an active session
        /// </summary>
        public bool HasActiveSession(string tabId) => _activeSessions.ContainsKey(tabId);
    }
    
    /// <summary>
    /// Information about an active user session
    /// </summary>
    public class SessionInfo
    {
        public string TabId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string BrowserInfo { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
        public DateTime SessionStartTime { get; set; }
        
        public string GetSessionDuration()
        {
            var duration = DateTime.UtcNow - SessionStartTime;
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        }
        
        public string GetLastActivityTime()
        {
            return LastHeartbeat.ToLocalTime().ToString("HH:mm:ss");
        }
    }
}
