using System.Diagnostics;
using System.Text;

namespace Fingerprint_Attendance_System.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses.
    /// Provides audit trail, performance metrics, and debugging capabilities.
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly RequestLoggingOptions _options;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            RequestLoggingOptions? options = null)
        {
            _next = next;
            _logger = logger;
            _options = options ?? new RequestLoggingOptions();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for excluded paths
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            // Store request ID for correlation
            context.Items["RequestId"] = requestId;

            // Log request
            await LogRequestAsync(context, requestId);

            // Capture response body if enabled
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            
            if (_options.LogResponseBody)
            {
                context.Response.Body = responseBody;
            }

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                if (_options.LogResponseBody)
                {
                    await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds, responseBody);
                    
                    // Copy response body back to original stream
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;
                }
                else
                {
                    LogResponseSummary(context, requestId, stopwatch.ElapsedMilliseconds);
                }

                // Log slow requests as warnings
                if (stopwatch.ElapsedMilliseconds > _options.SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "[{RequestId}] ⚠️ SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                        requestId,
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds,
                        _options.SlowRequestThresholdMs);
                }
            }
        }

        private bool ShouldSkipLogging(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant() ?? "";
            
            foreach (var excludedPath in _options.ExcludedPaths)
            {
                if (pathValue.StartsWith(excludedPath.ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;
            
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"[{requestId}] ➡️ REQUEST: {request.Method} {request.Path}{request.QueryString}");
            logBuilder.AppendLine($"[{requestId}]   Host: {request.Host}");
            logBuilder.AppendLine($"[{requestId}]   ContentType: {request.ContentType ?? "N/A"}");
            logBuilder.AppendLine($"[{requestId}]   ContentLength: {request.ContentLength ?? 0}");
            
            // Log user info if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                logBuilder.AppendLine($"[{requestId}]   User: {context.User.Identity.Name}");
            }
            
            // Log session user ID if available (safely - session may not be configured for all requests)
            try
            {
                if (context.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>() != null)
                {
                    var userId = context.Session?.GetInt32("UserId");
                    if (userId.HasValue)
                    {
                        logBuilder.AppendLine($"[{requestId}]   SessionUserId: {userId.Value}");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Session not configured for this request - skip session logging
            }

            // Log client IP
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            logBuilder.AppendLine($"[{requestId}]   ClientIP: {clientIp}");

            // Log request body for POST/PUT/PATCH (if enabled and not too large)
            if (_options.LogRequestBody && 
                (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH") &&
                request.ContentLength > 0 && 
                request.ContentLength < _options.MaxBodyLogSize)
            {
                request.EnableBuffering();
                
                using var reader = new StreamReader(
                    request.Body, 
                    Encoding.UTF8, 
                    detectEncodingFromByteOrderMarks: false, 
                    leaveOpen: true);
                
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                // Mask sensitive data
                body = MaskSensitiveData(body);
                
                if (!string.IsNullOrWhiteSpace(body))
                {
                    logBuilder.AppendLine($"[{requestId}]   Body: {TruncateBody(body)}");
                }
            }

            _logger.LogInformation(logBuilder.ToString().TrimEnd());
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs, MemoryStream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            var statusEmoji = context.Response.StatusCode switch
            {
                >= 200 and < 300 => "✅",
                >= 300 and < 400 => "↪️",
                >= 400 and < 500 => "⚠️",
                >= 500 => "❌",
                _ => "❓"
            };

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"[{requestId}] {statusEmoji} RESPONSE: {context.Response.StatusCode} in {elapsedMs}ms");
            logBuilder.AppendLine($"[{requestId}]   ContentType: {context.Response.ContentType ?? "N/A"}");
            
            if (_options.LogResponseBody && !string.IsNullOrWhiteSpace(responseText) && responseText.Length < _options.MaxBodyLogSize)
            {
                responseText = MaskSensitiveData(responseText);
                logBuilder.AppendLine($"[{requestId}]   Body: {TruncateBody(responseText)}");
            }

            var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(logLevel, logBuilder.ToString().TrimEnd());
        }

        private void LogResponseSummary(HttpContext context, string requestId, long elapsedMs)
        {
            var statusEmoji = context.Response.StatusCode switch
            {
                >= 200 and < 300 => "✅",
                >= 300 and < 400 => "↪️",
                >= 400 and < 500 => "⚠️",
                >= 500 => "❌",
                _ => "❓"
            };

            var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            
            _logger.Log(logLevel,
                "[{RequestId}] {StatusEmoji} {Method} {Path} => {StatusCode} in {ElapsedMs}ms",
                requestId,
                statusEmoji,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs);
        }

        private string MaskSensitiveData(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Mask common sensitive fields
            foreach (var field in _options.SensitiveFields)
            {
                // JSON format: "fieldName": "value"
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
                content = System.Text.RegularExpressions.Regex.Replace(
                    content, 
                    pattern, 
                    $"\"{field}\": \"***MASKED***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return content;
        }

        private string TruncateBody(string body)
        {
            if (body.Length <= _options.MaxBodyLogSize)
                return body;

            return body[.._options.MaxBodyLogSize] + "... [TRUNCATED]";
        }
    }

    /// <summary>
    /// Configuration options for request/response logging
    /// </summary>
    public class RequestLoggingOptions
    {
        /// <summary>
        /// Whether to log request body content
        /// </summary>
        public bool LogRequestBody { get; set; } = true;

        /// <summary>
        /// Whether to log response body content
        /// </summary>
        public bool LogResponseBody { get; set; } = false;

        /// <summary>
        /// Maximum body size to log (in characters)
        /// </summary>
        public int MaxBodyLogSize { get; set; } = 4096;

        /// <summary>
        /// Threshold in milliseconds for slow request warnings
        /// </summary>
        public int SlowRequestThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Paths to exclude from logging (e.g., health checks, static files, heartbeats)
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/lib/",
            "/css/",
            "/js/",
            "/images/",
            "/favicon",
            "/_framework",
            "/signalr",
            "/Auth/Heartbeat"  // Exclude heartbeat to prevent log flooding (called every 5s)
        };

        /// <summary>
        /// Field names to mask in logs (passwords, tokens, etc.)
        /// </summary>
        public List<string> SensitiveFields { get; set; } = new()
        {
            "password",
            "token",
            "secret",
            "apikey",
            "api_key",
            "authorization",
            "creditcard",
            "credit_card",
            "ssn",
            "fingerprint"
        };
    }
}
