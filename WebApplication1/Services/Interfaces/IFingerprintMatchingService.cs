using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Services.Interfaces
{
    /// <summary>
    /// Interface for fingerprint matching and validation operations.
    /// Enables unit testing with mock implementations.
    /// Follows Dependency Inversion Principle (SOLID).
    /// </summary>
    public interface IFingerprintMatchingService
    {
        /// <summary>
        /// Get configured attendance settings
        /// </summary>
        AttendanceSettings Settings { get; }

        /// <summary>
        /// Check if scanner device is connected and ready
        /// </summary>
        bool IsDeviceReady { get; }

        /// <summary>
        /// Capture fingerprint template from scanner
        /// </summary>
        /// <returns>Template bytes or null if capture failed</returns>
        byte[]? CaptureFingerprint();

        /// <summary>
        /// Capture fingerprint with image data
        /// </summary>
        /// <returns>Tuple containing image data, dimensions, template, error code, and error message</returns>
        (byte[]? imageData, int width, int height, byte[]? template, int errorCode, string errorMessage) CaptureFingerprintWithImage();

        /// <summary>
        /// Initialize and turn on the fingerprint device
        /// </summary>
        /// <returns>True if device is ready</returns>
        bool EnsureDeviceReady();

        /// <summary>
        /// Match captured fingerprint against database and validate user role.
        /// This is the SINGLE source of truth for fingerprint matching logic.
        /// </summary>
        /// <param name="capturedTemplate">The captured fingerprint template bytes</param>
        /// <param name="requireStudentRole">Whether to validate that the user is a student</param>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        /// <returns>Match result with user/student information</returns>
        Task<FingerprintMatchResult> MatchAndValidateFingerprintAsync(
            byte[] capturedTemplate,
            bool requireStudentRole = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Log scan attempt to database
        /// </summary>
        /// <param name="userId">User ID (null if unknown)</param>
        /// <param name="success">Whether scan was successful</param>
        /// <param name="message">Log message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LogScanAttemptAsync(int? userId, bool success, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Provide LED feedback for scan result
        /// </summary>
        /// <param name="success">Whether scan was successful</param>
        /// <param name="roleViolation">Whether there was a role violation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProvideLedFeedbackAsync(bool success, bool roleViolation = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determine attendance status based on scan time and class start time
        /// </summary>
        /// <param name="currentTime">Current time of day</param>
        /// <param name="classStartTime">Class start time</param>
        /// <returns>Attendance status (Present or Late)</returns>
        AttendanceStatus DetermineAttendanceStatus(TimeSpan currentTime, TimeSpan classStartTime);
    }
}
