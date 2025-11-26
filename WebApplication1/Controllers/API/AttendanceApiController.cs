using Microsoft.AspNetCore.Mvc;
using Fingerprint_Attendance_System.Services;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Exceptions;

namespace Fingerprint_Attendance_System.Controllers.Api
{
    /// <summary>
    /// RESTful API for attendance operations
    /// Provides endpoints for Time In/Time Out and attendance management
    /// PHASE 6: STEP 15 - REST API Endpoints
    /// </summary>
    [Route("api/v1/attendance")]
    [ApiController]
    public class AttendanceApiController : ControllerBase
    {
        private readonly AttendanceProcessingService _processingService;
        private readonly DailyAttendanceService _dailyAttendanceService;
        private readonly MissedTimeOutService _missedTimeOutService;
        private readonly ILogger<AttendanceApiController> _logger;

        public AttendanceApiController(
            AttendanceProcessingService processingService,
            DailyAttendanceService dailyAttendanceService,
            MissedTimeOutService missedTimeOutService,
            ILogger<AttendanceApiController> logger)
        {
            _processingService = processingService;
            _dailyAttendanceService = dailyAttendanceService;
            _missedTimeOutService = missedTimeOutService;
            _logger = logger;
        }

        #region Core Attendance Operations

        /// <summary>
        /// POST /api/v1/attendance/scan
        /// Main endpoint: Record attendance via fingerprint scan
        /// Handles Time In/Time Out toggle logic
        /// </summary>
        /// <response code="200">Attendance recorded successfully</response>
        /// <response code="400">Invalid request or already completed</response>
        /// <response code="503">Scanner unavailable</response>
        [HttpPost("scan")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceScanResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 503)]
        public async Task<IActionResult> RecordAttendance()
        {
            try
            {
                _logger.LogInformation("API: Attendance scan requested");

                var result = await _processingService.ProcessAttendanceAsync();

                if (result.Success && result.AttendanceResponse != null)
                {
                    return Ok(new ApiResponse<AttendanceScanResponseDto>
                    {
                        Success = true,
                        Message = "Attendance processed successfully",
                        Data = result.AttendanceResponse,
                        Metadata = new
                        {
                            processingTimeMs = result.ProcessingTimeMs,
                            timestamp = DateTime.Now
                        }
                    });
                }
                else
                {
                    var statusCode = result.ErrorType switch
                    {
                        nameof(ScannerNotAvailableException) => 503,
                        nameof(FingerprintNotRecognizedException) => 400,
                        nameof(NoScheduleException) => 400,
                        _ => 500
                    };

                    return StatusCode(statusCode, new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Error ?? "Attendance processing failed",
                        ErrorCode = result.ErrorType,
                        Data = new
                        {
                            scannerStatus = result.ScannerStatus,
                            processingTimeMs = result.ProcessingTimeMs
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error processing attendance");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// GET /api/v1/attendance/scanner/status
        /// Check fingerprint scanner device status
        /// </summary>
        /// <response code="200">Scanner status retrieved</response>
        [HttpGet("scanner/status")]
        [ProducesResponseType(typeof(ApiResponse<ScannerStatusDto>), 200)]
        public IActionResult GetScannerStatus()
        {
            try
            {
                var status = _processingService.GetScannerStatus();

                return Ok(new ApiResponse<ScannerStatusDto>
                {
                    Success = true,
                    Message = status.IsAvailable ? "Scanner online" : "Scanner offline",
                    Data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking scanner status");
                return Ok(new ApiResponse<ScannerStatusDto>
                {
                    Success = false,
                    Message = "Error checking scanner status",
                    Data = new ScannerStatusDto
                    {
                        IsAvailable = false,
                        Status = "Error",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// GET /api/v1/attendance/today/summary
        /// Get today's attendance summary statistics
        /// </summary>
        /// <response code="200">Summary retrieved successfully</response>
        [HttpGet("today/summary")]
        [ProducesResponseType(typeof(ApiResponse<DailyAttendanceSummaryDto>), 200)]
        public async Task<IActionResult> GetTodaySummary()
        {
            try
            {
                var summary = await _processingService.GetTodaySummaryAsync();

                return Ok(new ApiResponse<DailyAttendanceSummaryDto>
                {
                    Success = true,
                    Message = "Summary retrieved successfully",
                    Data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's summary");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving summary",
                    ErrorCode = "SUMMARY_ERROR"
                });
            }
        }

        #endregion

        #region Student-Specific Endpoints

        /// <summary>
        /// GET /api/v1/attendance/student/{studId}/today
        /// Get specific student's attendance for today
        /// </summary>
        /// <param name="studId">Student ID</param>
        /// <response code="200">Attendance record retrieved</response>
        /// <response code="404">No attendance record found</response>
        [HttpGet("student/{studId}/today")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetStudentTodayAttendance(int studId)
        {
            try
            {
                var record = await _dailyAttendanceService.GetDailyRecordAsync(studId, DateTime.Today);

                if (record == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No attendance record found for today"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Attendance record retrieved",
                    Data = new
                    {
                        dailyAttendId = record.DailyAttendId,
                        studId = record.StudId,
                        date = record.AttendDate,
                        timeIn = record.TimeIn,
                        timeOut = record.TimeOut,
                        status = record.Status,
                        forgotTimeout = record.ForgotTimeout,
                        remarks = record.Remarks
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving attendance for student {studId}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving attendance record"
                });
            }
        }

        /// <summary>
        /// GET /api/v1/attendance/student/{studId}/history
        /// Get student's attendance history (last 30 days)
        /// </summary>
        /// <param name="studId">Student ID</param>
        /// <param name="days">Number of days to retrieve (default: 30)</param>
        /// <response code="200">History retrieved successfully</response>
        [HttpGet("student/{studId}/history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetStudentAttendanceHistory(int studId, [FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.Today.AddDays(-days);
                var records = new List<object>();

                for (var date = startDate; date <= DateTime.Today; date = date.AddDays(1))
                {
                    var record = await _dailyAttendanceService.GetDailyRecordAsync(studId, date);
                    if (record != null)
                    {
                        records.Add(new
                        {
                            date = record.AttendDate,
                            timeIn = record.TimeIn,
                            timeOut = record.TimeOut,
                            status = record.Status,
                            forgotTimeout = record.ForgotTimeout
                        });
                    }
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Retrieved {records.Count} attendance records",
                    Data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving history for student {studId}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving attendance history"
                });
            }
        }

        #endregion

        #region Admin Endpoints

        /// <summary>
        /// GET /api/v1/attendance/incomplete
        /// Get all incomplete (forgot time out) records
        /// Requires Admin role
        /// </summary>
        /// <response code="200">Incomplete records retrieved</response>
        [HttpGet("incomplete")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<IncompleteRecordDto>>), 200)]
        public async Task<IActionResult> GetIncompleteRecords()
        {
            try
            {
                // TODO: Add role authorization check
                var records = await _missedTimeOutService.GetAllIncompleteRecordsAsync();

                return Ok(new ApiResponse<IEnumerable<IncompleteRecordDto>>
                {
                    Success = true,
                    Message = $"Found {records.Count()} incomplete records",
                    Data = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incomplete records");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error retrieving incomplete records"
                });
            }
        }

        /// <summary>
        /// POST /api/v1/attendance/incomplete/{dailyAttendId}/resolve
        /// Manually resolve an incomplete record
        /// Requires Admin role
        /// </summary>
        /// <param name="dailyAttendId">Daily attendance ID</param>
        /// <param name="request">Resolution details</param>
        /// <response code="200">Record resolved successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost("incomplete/{dailyAttendId}/resolve")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> ResolveIncompleteRecord(
            int dailyAttendId,
            [FromBody] ResolveIncompleteRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request body"
                    });
                }
                
                // TODO: Get admin user ID from session/token
                var adminUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (adminUserId == 0)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Not authenticated"
                    });
                }

                var resolved = await _missedTimeOutService.ResolveIncompleteRecordAsync(
                    dailyAttendId,
                    request.ManualTimeOut,
                    adminUserId);

                if (resolved)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Incomplete record resolved successfully"
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to resolve record"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving incomplete record {dailyAttendId}");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error resolving record"
                });
            }
        }

        /// <summary>
        /// POST /api/v1/attendance/absences/process
        /// Manually trigger absence detection for a specific date
        /// Requires Admin role
        /// </summary>
        /// <param name="date">Date to process (format: yyyy-MM-dd)</param>
        /// <response code="200">Absences processed successfully</response>
        [HttpPost("absences/process")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ProcessAbsences([FromQuery] string? date = null)
        {
            try
            {
                DateTime targetDate = string.IsNullOrEmpty(date)
                    ? DateTime.Today
                    : DateTime.Parse(date);

                await _processingService.ProcessAbsencesAsync(targetDate);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Absences processed for {targetDate:yyyy-MM-dd}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing absences");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error processing absences"
                });
            }
        }

        #endregion

        #region Health Check

        /// <summary>
        /// GET /api/v1/attendance/health
        /// Health check endpoint
        /// </summary>
        /// <response code="200">Service is healthy</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Attendance API is healthy",
                Data = new
                {
                    timestamp = DateTime.Now,
                    version = "1.0.0",
                    status = "operational"
                }
            });
        }

        #endregion
    }

    #region Request/Response Models

    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? ErrorCode { get; set; }
        public object? Metadata { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Request model for resolving incomplete records
    /// </summary>
    public class ResolveIncompleteRequest
    {
        public DateTime ManualTimeOut { get; set; }
    }

    #endregion
}
