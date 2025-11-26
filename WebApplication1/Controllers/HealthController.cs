using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Services.Interfaces;
using Fingerprint_Attendance_System.Data;
using System.Diagnostics;
using System.Reflection;

namespace Fingerprint_Attendance_System.Controllers
{
    /// <summary>
    /// Health check endpoints for monitoring system status.
    /// Supports Kubernetes/Docker health probes, load balancer checks, and monitoring dashboards.
    /// </summary>
    [Route("health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly DatabaseHelper _db;
        private readonly ILogger<HealthController> _logger;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public HealthController(
            IFingerprintService fingerprintService,
            DatabaseHelper db,
            ILogger<HealthController> logger)
        {
            _fingerprintService = fingerprintService;
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Basic liveness probe - returns 200 if the application is running
        /// Used by Kubernetes liveness probes
        /// GET /health/live
        /// </summary>
        [HttpGet("live")]
        [ProducesResponseType(200)]
        public IActionResult LivenessProbe()
        {
            return Ok(new
            {
                status = "Alive",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Readiness probe - returns 200 if the application is ready to accept traffic
        /// Checks database connectivity
        /// Used by Kubernetes readiness probes
        /// GET /health/ready
        /// </summary>
        [HttpGet("ready")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> ReadinessProbe()
        {
            try
            {
                // Check database connectivity
                var result = await _db.ExecuteQueryAsync(
                    "SELECT 1 as check",
                    new Dictionary<string, object>());

                if (result.Rows.Count > 0)
                {
                    return Ok(new
                    {
                        status = "Ready",
                        timestamp = DateTime.UtcNow,
                        database = "Connected"
                    });
                }

                return StatusCode(503, new
                {
                    status = "Not Ready",
                    timestamp = DateTime.UtcNow,
                    database = "Query failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Readiness probe failed");
                return StatusCode(503, new
                {
                    status = "Not Ready",
                    timestamp = DateTime.UtcNow,
                    database = "Disconnected",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Comprehensive health check - returns detailed system status
        /// Used by monitoring dashboards (Grafana, etc.)
        /// GET /health
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(SystemHealthStatus), 200)]
        [ProducesResponseType(typeof(SystemHealthStatus), 503)]
        public async Task<IActionResult> GetHealthStatus()
        {
            var health = new SystemHealthStatus
            {
                Timestamp = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - _startTime,
                Version = GetApplicationVersion(),
                Metrics = GetSystemMetrics()
            };

            // Check each component
            health.Components["database"] = await CheckDatabaseHealthAsync();
            health.Components["scanner"] = CheckScannerHealth();
            health.Components["memory"] = CheckMemoryHealth();

            // Determine overall status
            var hasUnhealthy = health.Components.Values.Any(c => c.Status == "Unhealthy");
            var hasDegraded = health.Components.Values.Any(c => c.Status == "Degraded");

            if (hasUnhealthy)
            {
                health.Status = "Unhealthy";
                return StatusCode(503, health);
            }
            else if (hasDegraded)
            {
                health.Status = "Degraded";
                return Ok(health);
            }
            else
            {
                health.Status = "Healthy";
                return Ok(health);
            }
        }

        /// <summary>
        /// Scanner-specific health check
        /// GET /health/scanner
        /// </summary>
        [HttpGet("scanner")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public IActionResult GetScannerHealth()
        {
            var health = CheckScannerHealth();
            
            if (health.Status == "Healthy")
            {
                return Ok(new
                {
                    status = health.Status,
                    message = health.Message,
                    data = health.Data,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = health.Status,
                message = health.Message,
                data = health.Data,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Database-specific health check
        /// GET /health/database
        /// </summary>
        [HttpGet("database")]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            var health = await CheckDatabaseHealthAsync();
            
            if (health.Status == "Healthy")
            {
                return Ok(new
                {
                    status = health.Status,
                    message = health.Message,
                    data = health.Data,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = health.Status,
                message = health.Message,
                data = health.Data,
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<ComponentHealth> CheckDatabaseHealthAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await _db.ExecuteQueryAsync(
                    "SELECT NOW() as server_time, version() as db_version",
                    new Dictionary<string, object>());
                stopwatch.Stop();

                if (result.Rows.Count > 0)
                {
                    return new ComponentHealth
                    {
                        Status = "Healthy",
                        Message = "Database connection is healthy",
                        Data = new Dictionary<string, object>
                        {
                            { "responseTimeMs", stopwatch.ElapsedMilliseconds },
                            { "serverTime", result.Rows[0]["server_time"]?.ToString() ?? "N/A" },
                            { "version", result.Rows[0]["db_version"]?.ToString()?.Split(' ')[0] ?? "N/A" }
                        }
                    };
                }

                return new ComponentHealth
                {
                    Status = "Degraded",
                    Message = "Database query returned no results"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    Message = $"Database connection failed: {ex.Message}",
                    Data = new Dictionary<string, object>
                    {
                        { "error", ex.Message }
                    }
                };
            }
        }

        private ComponentHealth CheckScannerHealth()
        {
            try
            {
                var isConnected = _fingerprintService.IsDeviceConnected;
                var status = _fingerprintService.GetStatus();

                if (isConnected)
                {
                    return new ComponentHealth
                    {
                        Status = "Healthy",
                        Message = "Fingerprint scanner is connected and ready",
                        Data = new Dictionary<string, object>
                        {
                            { "deviceStatus", status },
                            { "connected", true }
                        }
                    };
                }
                else
                {
                    return new ComponentHealth
                    {
                        Status = "Degraded",
                        Message = "Fingerprint scanner is not connected",
                        Data = new Dictionary<string, object>
                        {
                            { "deviceStatus", status },
                            { "connected", false }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scanner health check failed");
                return new ComponentHealth
                {
                    Status = "Unhealthy",
                    Message = $"Scanner check failed: {ex.Message}",
                    Data = new Dictionary<string, object>
                    {
                        { "error", ex.Message }
                    }
                };
            }
        }

        private ComponentHealth CheckMemoryHealth()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryUsedMB = process.WorkingSet64 / (1024.0 * 1024.0);
                
                // Consider unhealthy if using more than 1GB
                var status = memoryUsedMB > 1024 ? "Degraded" : "Healthy";
                var message = memoryUsedMB > 1024 
                    ? $"High memory usage: {memoryUsedMB:F2} MB" 
                    : $"Memory usage normal: {memoryUsedMB:F2} MB";

                return new ComponentHealth
                {
                    Status = status,
                    Message = message,
                    Data = new Dictionary<string, object>
                    {
                        { "usedMB", Math.Round(memoryUsedMB, 2) },
                        { "usedBytes", process.WorkingSet64 }
                    }
                };
            }
            catch (Exception ex)
            {
                return new ComponentHealth
                {
                    Status = "Unknown",
                    Message = $"Could not check memory: {ex.Message}"
                };
            }
        }

        private SystemMetrics GetSystemMetrics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return new SystemMetrics
                {
                    MemoryUsedBytes = process.WorkingSet64,
                    MemoryUsedMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                    ThreadCount = process.Threads.Count,
                    ProcessorCount = Environment.ProcessorCount
                };
            }
            catch
            {
                return new SystemMetrics();
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}
