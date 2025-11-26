using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// Custom health check for fingerprint scanner device
    /// </summary>
    public class FingerprintScannerHealthCheck : IHealthCheck
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly ILogger<FingerprintScannerHealthCheck> _logger;

        public FingerprintScannerHealthCheck(
            IFingerprintService fingerprintService,
            ILogger<FingerprintScannerHealthCheck> logger)
        {
            _fingerprintService = fingerprintService;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isConnected = _fingerprintService.IsDeviceConnected;
                var status = _fingerprintService.GetStatus();

                if (isConnected)
                {
                    return Task.FromResult(HealthCheckResult.Healthy(
                        $"Fingerprint scanner is connected. Status: {status}",
                        new Dictionary<string, object>
                        {
                            { "status", status },
                            { "connected", true },
                            { "timestamp", DateTime.UtcNow }
                        }));
                }
                else
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Fingerprint scanner is not connected. Status: {status}",
                        data: new Dictionary<string, object>
                        {
                            { "status", status },
                            { "connected", false },
                            { "timestamp", DateTime.UtcNow }
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for fingerprint scanner");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Fingerprint scanner health check failed: {ex.Message}",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "error", ex.Message },
                        { "timestamp", DateTime.UtcNow }
                    }));
            }
        }
    }

    /// <summary>
    /// Custom health check for database connectivity
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(
            DatabaseHelper db,
            ILogger<DatabaseHealthCheck> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple query to verify database connectivity
                var result = await _db.ExecuteQueryAsync(
                    "SELECT 1 as health_check, NOW() as server_time",
                    new Dictionary<string, object>());

                if (result.Rows.Count > 0)
                {
                    var serverTime = result.Rows[0]["server_time"];
                    return HealthCheckResult.Healthy(
                        "Database connection is healthy",
                        new Dictionary<string, object>
                        {
                            { "connected", true },
                            { "serverTime", serverTime?.ToString() ?? "N/A" },
                            { "timestamp", DateTime.UtcNow }
                        });
                }

                return HealthCheckResult.Degraded(
                    "Database query returned no results",
                    data: new Dictionary<string, object>
                    {
                        { "connected", false },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy(
                    $"Database connection failed: {ex.Message}",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "error", ex.Message },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
        }
    }

    /// <summary>
    /// Comprehensive system health status DTO
    /// </summary>
    public class SystemHealthStatus
    {
        public string Status { get; set; } = "Unknown";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan Uptime { get; set; }
        public string Version { get; set; } = "1.0.0";
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
        public SystemMetrics Metrics { get; set; } = new();
    }

    public class ComponentHealth
    {
        public string Status { get; set; } = "Unknown";
        public string? Message { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class SystemMetrics
    {
        public long MemoryUsedBytes { get; set; }
        public double MemoryUsedMB { get; set; }
        public int ThreadCount { get; set; }
        public int ProcessorCount { get; set; }
    }
}
