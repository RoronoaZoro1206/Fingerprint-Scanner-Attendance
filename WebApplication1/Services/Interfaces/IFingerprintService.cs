namespace Fingerprint_Attendance_System.Services.Interfaces
{
    /// <summary>
    /// Interface for fingerprint scanner operations.
    /// Enables unit testing with mock implementations and allows swapping scanner brands.
    /// Follows Dependency Inversion Principle (SOLID).
    /// </summary>
    public interface IFingerprintService : IDisposable
    {
        // ============================
        //  DEVICE STATE PROPERTIES
        // ============================
        
        /// <summary>
        /// Indicates whether the fingerprint device is currently connected and ready
        /// </summary>
        bool IsDeviceConnected { get; }

        // ============================
        //  INITIALIZATION & DEVICE CONTROL
        // ============================
        
        /// <summary>
        /// Initialize the fingerprint SDK
        /// </summary>
        /// <returns>True if initialization successful</returns>
        bool Initialize();

        /// <summary>
        /// Turn on/open the fingerprint device
        /// </summary>
        /// <param name="index">Device index (default: 0)</param>
        /// <returns>True if device opened successfully</returns>
        bool TurnOnDevice(int index = 0);

        /// <summary>
        /// Turn off/close the fingerprint device
        /// </summary>
        void TurnOffDevice();

        /// <summary>
        /// Get current device status as string
        /// </summary>
        string GetStatus();

        // ============================
        //  FINGERPRINT CAPTURE
        // ============================
        
        /// <summary>
        /// Capture fingerprint template from scanner
        /// </summary>
        /// <returns>Template bytes or null if capture failed</returns>
        byte[]? CaptureTemplate();

        /// <summary>
        /// Capture fingerprint template and return as Base64 string
        /// </summary>
        /// <returns>Base64 encoded template or null if capture failed</returns>
        string? CaptureTemplateBase64();

        /// <summary>
        /// Capture fingerprint with image data and detailed error information
        /// </summary>
        /// <returns>Tuple containing image data, dimensions, template, error code, and error message</returns>
        (byte[]? imageData, int width, int height, byte[]? template, int errorCode, string errorMessage) CaptureFingerprintWithImage();

        // ============================
        //  FINGERPRINT MATCHING
        // ============================
        
        /// <summary>
        /// Match two fingerprint templates
        /// </summary>
        /// <param name="template1">First template</param>
        /// <param name="template2">Second template</param>
        /// <param name="threshold">Match threshold (default: 50)</param>
        /// <returns>True if templates match above threshold</returns>
        bool MatchFingerprints(byte[] template1, byte[] template2, int threshold = 50);

        /// <summary>
        /// Get match score between two fingerprint templates
        /// </summary>
        /// <param name="template1">First template</param>
        /// <param name="template2">Second template</param>
        /// <returns>Similarity score (0-100+)</returns>
        int GetMatchScore(byte[] template1, byte[] template2);

        /// <summary>
        /// Identify fingerprint against database
        /// </summary>
        /// <param name="template">Template to identify</param>
        /// <param name="fid">Output: matched fingerprint ID</param>
        /// <param name="score">Output: match score</param>
        /// <returns>Result code</returns>
        int IdentifyFingerprint(byte[] template, ref int fid, ref int score);

        /// <summary>
        /// Add fingerprint template to internal database
        /// </summary>
        /// <param name="fid">Fingerprint ID</param>
        /// <param name="template">Template data</param>
        void AddTemplateToDB(int fid, byte[] template);

        // ============================
        //  IMAGE CONVERSION
        // ============================
        
        /// <summary>
        /// Convert raw fingerprint image data to Base64 BMP string
        /// </summary>
        /// <param name="imageData">Raw image data</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>Base64 encoded BMP image or null</returns>
        string? ConvertImageToBase64(byte[] imageData, int width, int height);

        // ============================
        //  LED & BUZZER CONTROL
        // ============================
        
        /// <summary>
        /// Set LED light state
        /// </summary>
        /// <param name="color">LED color: "white", "green", or "red"</param>
        /// <param name="on">True to turn on, false to turn off</param>
        /// <returns>True if command successful</returns>
        bool SetLedLight(string color, bool on);

        /// <summary>
        /// Activate success LED (green) with automatic timeout
        /// </summary>
        void ActivateSuccessLED();

        /// <summary>
        /// Activate error LED (red) with automatic timeout
        /// </summary>
        void ActivateErrorLED();

        /// <summary>
        /// Turn off all LED lights
        /// </summary>
        void TurnOffAllLeds();

        /// <summary>
        /// Control buzzer/beeper
        /// </summary>
        /// <param name="on">True to turn on, false to turn off</param>
        /// <returns>True if command successful</returns>
        bool Beep(bool on);
    }
}
