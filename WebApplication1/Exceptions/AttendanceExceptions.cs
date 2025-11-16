namespace Fingerprint_Attendance_System.Exceptions
{
    /// <summary>
    /// Exception thrown when fingerprint scanner is not available
    /// </summary>
    public class ScannerNotAvailableException : Exception
    {
        public ScannerNotAvailableException() 
            : base("Fingerprint scanner is not available. Please check device connection.") { }

        public ScannerNotAvailableException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when fingerprint is not recognized
    /// </summary>
    public class FingerprintNotRecognizedException : Exception
    {
        public FingerprintNotRecognizedException() 
            : base("Fingerprint not recognized. Please ensure your fingerprint is registered.") { }

        public FingerprintNotRecognizedException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when student is not enrolled in any classes
    /// </summary>
    public class NoScheduleException : Exception
    {
        public int StudentId { get; }

        public NoScheduleException(int studentId) 
            : base("Student is not enrolled in any classes for this term.")
        {
            StudentId = studentId;
        }

        public NoScheduleException(int studentId, string message) 
            : base(message)
        {
            StudentId = studentId;
        }
    }

    /// <summary>
    /// Exception thrown when attendance record already exists
    /// </summary>
    public class DuplicateAttendanceException : Exception
    {
        public int ClassId { get; }
        public int StudentId { get; }
        public DateTime Date { get; }

        public DuplicateAttendanceException(int classId, int studentId, DateTime date)
            : base($"Attendance record already exists for this class and date.")
        {
            ClassId = classId;
            StudentId = studentId;
            Date = date;
        }
    }

    /// <summary>
    /// Exception thrown when database connection fails
    /// </summary>
    public class DatabaseConnectionException : Exception
    {
        public DatabaseConnectionException() 
            : base("Unable to connect to database. Please try again later.") { }

        public DatabaseConnectionException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when concurrent attendance recording attempts detected
    /// </summary>
    public class ConcurrentScanException : Exception
    {
        public ConcurrentScanException() 
            : base("Another scan is currently in progress. Please wait.") { }
    }
}
