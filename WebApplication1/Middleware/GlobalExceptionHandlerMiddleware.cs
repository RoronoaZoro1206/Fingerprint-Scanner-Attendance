using System.Net;
using System.Text.Json;

namespace Fingerprint_Attendance_System.Middleware
{
    /// <summary>
    /// Global exception handler middleware
    /// Catches all unhandled exceptions and returns standardized error responses
    /// STEP 13: Comprehensive Error Handling
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var errorResponse = new ErrorResponse
            {
                Success = false,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case InvalidOperationException invalidOpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = invalidOpEx.Message;
                    errorResponse.ErrorCode = "INVALID_OPERATION";
                    break;

                case UnauthorizedAccessException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "You are not authorized to perform this action.";
                    errorResponse.ErrorCode = "UNAUTHORIZED";
                    break;

                case ArgumentNullException argNullEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = $"Required parameter is missing: {argNullEx.ParamName}";
                    errorResponse.ErrorCode = "MISSING_PARAMETER";
                    break;

                case TimeoutException timeoutEx:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.Message = "The operation timed out. Please try again.";
                    errorResponse.ErrorCode = "TIMEOUT";
                    break;

                case Npgsql.NpgsqlException dbEx:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Database error occurred. Please contact administrator.";
                    errorResponse.ErrorCode = "DATABASE_ERROR";
                    errorResponse.Details = _logger.IsEnabled(LogLevel.Debug) ? dbEx.Message : null;
                    _logger.LogError(dbEx, "Database error occurred");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "An unexpected error occurred. Please try again or contact support.";
                    errorResponse.ErrorCode = "INTERNAL_ERROR";
                    errorResponse.Details = _logger.IsEnabled(LogLevel.Debug) ? exception.Message : null;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
