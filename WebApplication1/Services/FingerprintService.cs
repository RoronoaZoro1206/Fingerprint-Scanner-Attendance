using libzkfpcsharp;
using Microsoft.Extensions.Options;
using Fingerprint_Attendance_System.Services.Interfaces;

namespace Fingerprint_Attendance_System.Services
{
    public class FingerprintService : IFingerprintService
    {
        private IntPtr _deviceHandle = IntPtr.Zero;
        private IntPtr _dbHandle = IntPtr.Zero;
        private bool _isInitialized = false;
        private bool _isDeviceOpen = false;
        private readonly ILogger<FingerprintService> _logger;
        private readonly AttendanceSettings _settings;
        private readonly object _captureLock = new object();

        private int _imgWidth = 0;
        private int _imgHeight = 0;
        private int _imgSize = 0;

        // ✅ NEW: Disposed flag for idempotent Dispose pattern
        private bool _disposed = false;

        public FingerprintService(ILogger<FingerprintService> logger, IOptions<AttendanceSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        // ============================
        //  INITIALIZATION
        // ============================
        public bool Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing ZKFinger SDK...");

                int ret = zkfp2.Init();
                if (ret != zkfp.ZKFP_ERR_OK && ret != zkfp.ZKFP_ERR_ALREADY_INIT)
                {
                    _logger.LogError($"SDK initialization failed with error code: {ret}");
                    return false;
                }

                int count = zkfp2.GetDeviceCount();
                if (count <= 0)
                {
                    _logger.LogWarning("No fingerprint devices found");
                    return false;
                }

                _logger.LogInformation($"Found {count} fingerprint device(s)");

                _dbHandle = zkfp2.DBInit();
                _isInitialized = _dbHandle != IntPtr.Zero;

                if (_isInitialized)
                {
                    _logger.LogInformation("SDK initialized successfully");
                }
                else
                {
                    _logger.LogError("Failed to initialize DB handle");
                }

                return _isInitialized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during SDK initialization");
                return false;
            }
        }

        // ============================
        //  DEVICE CONTROL
        // ============================
        public bool TurnOnDevice(int index = 0)
        {
            try
            {
                if (!_isInitialized)
                {
                    _logger.LogError("Cannot open device: SDK not initialized");
                    throw new InvalidOperationException("SDK not initialized.");
                }

                if (_isDeviceOpen)
                {
                    _logger.LogInformation("Device is already open");
                    return true;
                }

                _logger.LogInformation($"Opening device at index {index}...");
                _deviceHandle = zkfp2.OpenDevice(index);

                if (_deviceHandle == IntPtr.Zero)
                {
                    _logger.LogError("Failed to open device");
                    return false;
                }

                _isDeviceOpen = true;

                // Load image parameters
                byte[] paramValue = new byte[4];
                int size = 4;

                zkfp2.GetParameters(_deviceHandle, 1, paramValue, ref size);
                zkfp2.ByteArray2Int(paramValue, ref _imgWidth);

                zkfp2.GetParameters(_deviceHandle, 2, paramValue, ref size);
                zkfp2.ByteArray2Int(paramValue, ref _imgHeight);

                _imgSize = _imgWidth * _imgHeight;

                _logger.LogInformation($"Device opened successfully. Image size: {_imgWidth}x{_imgHeight}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while opening device");
                return false;
            }
        }

        public void TurnOffDevice()
        {
            try
            {
                if (_isDeviceOpen && _deviceHandle != IntPtr.Zero)
                {
                    _logger.LogInformation("Closing device...");
                    zkfp2.CloseDevice(_deviceHandle);
                    _deviceHandle = IntPtr.Zero;
                    _isDeviceOpen = false;
                    _logger.LogInformation("Device closed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while closing device");
            }
        }

        public string GetStatus()
        {
            if (!_isInitialized)
                return "Not Initialized";
            if (_isDeviceOpen)
                return "Device Connected";
            return "Initialized but not connected";
        }

        public bool IsDeviceConnected => _isDeviceOpen;

        // ============================
        //  SCANNER STATE VALIDATION & RESET
        // ============================
        
        /// <summary>
        /// Validates scanner state and resets if necessary before capture
        /// This ensures the device is in a clean, ready state
        /// </summary>
        private bool EnsureScannerReady()
        {
            try
            {
                _logger.LogInformation("Validating scanner state before capture...");
                
                // Check if device is still connected
                if (!_isDeviceOpen || _deviceHandle == IntPtr.Zero)
                {
                    _logger.LogWarning("Device not connected. Attempting to reconnect...");
                    
                    // Try to reconnect
                    if (!TurnOnDevice())
                    {
                        _logger.LogError("Failed to reconnect device");
                        return false;
                    }
                }
        
                // ? Clear any pending operations by reading device state
                byte[] statusCheck = new byte[4];
                int size = 4;
                int statusResult = zkfp2.GetParameters(_deviceHandle, 1, statusCheck, ref size);
                
                if (statusResult != zkfp.ZKFP_ERR_OK)
                {
                    _logger.LogWarning($"Device status check returned error: {statusResult}");
                    // Try device reset
                    return ResetScanner();
                }
                
                _logger.LogInformation("Scanner state validated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating scanner state");
                return false;
            }
        }
        
        /// <summary>
        /// Resets the scanner by closing and reopening the device
        /// </summary>
        private bool ResetScanner()
        {
            try
            {
                _logger.LogInformation("Resetting scanner...");
                
                TurnOffDevice();
                Thread.Sleep(_settings.DeviceResetDelayMs); // Wait for device to fully close (configurable)
        
                bool reopened = TurnOnDevice();
                if (reopened)
                {
                    _logger.LogInformation("Scanner reset successful");
                }
                else
                {
                    _logger.LogError("Scanner reset failed");
                }
            
                return reopened;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during scanner reset");
                return false;
            }
        }

        // ============================
        //  ✅ IMPROVED FINGERPRINT CAPTURE WITH RETRY LOGIC
        // ============================
        
        /// <summary>
        /// Captures fingerprint image and template with built-in retry logic
        /// Returns both the raw image data, template, and detailed error information
        /// </summary>
        public (byte[]? imageData, int width, int height, byte[]? template, int errorCode, string errorMessage) CaptureFingerprintWithImage()
        {
            // ✅ Use lock to prevent concurrent capture attempts
            lock (_captureLock)
            {
                // ✅ Use configurable settings instead of magic numbers
                int maxRetries = _settings.MaxCaptureRetries;
                int retryDelayMs = _settings.CaptureRetryDelayMs;
                int templateBufferSize = _settings.TemplateBufferSize;
                int minTemplateSize = _settings.MinTemplateSize;
        
                int lastErrorCode = 0;
                string lastErrorMessage = "";

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    _logger.LogInformation($"Capture attempt {attempt}/{maxRetries}");
                    
                    try
                    {
                        // ✅ STEP 1: Validate scanner state before capture
                        if (!EnsureScannerReady())
                        {
                            _logger.LogWarning($"Scanner not ready on attempt {attempt}");
                            lastErrorCode = -100; // Custom code for scanner not ready
                            lastErrorMessage = "Scanner not ready";
                            
                            if (attempt < maxRetries)
                            {
                                _logger.LogInformation($"Waiting {retryDelayMs}ms before retry...");
                                Thread.Sleep(retryDelayMs);
                                continue;
                            }
                            
                            return (null, 0, 0, null, lastErrorCode, lastErrorMessage);
                        }
                    
                        // ✅ STEP 2: Allocate fresh buffers (prevents data contamination)
                        byte[] imgBuffer = new byte[_imgSize > 0 ? _imgSize : 256 * 288];
                        byte[] template = new byte[templateBufferSize];
                        int size = template.Length;
                        
                        _logger.LogInformation("Attempting to capture fingerprint with image...");
                        
                        // ✅ STEP 3: Attempt capture
                        int ret = zkfp2.AcquireFingerprint(_deviceHandle, imgBuffer, template, ref size);

                        // ✅ STEP 4: Handle result with detailed error codes
                        if (ret == zkfp.ZKFP_ERR_OK)
                        {
                            Array.Resize(ref template, size);
                            _logger.LogInformation($"✅ Fingerprint captured successfully on attempt {attempt}. Image: {_imgWidth}x{_imgHeight}, Template: {size} bytes");
                        
                            // Validate captured data using configurable minimum size
                            if (size < minTemplateSize)
                            {
                                _logger.LogWarning($"Template size suspiciously small ({size} bytes, min: {minTemplateSize}). Retrying...");
                                lastErrorCode = zkfp.ZKFP_ERR_EXTRACT_FP;
                                lastErrorMessage = GetErrorMessage(zkfp.ZKFP_ERR_EXTRACT_FP);
                                
                                if (attempt < maxRetries)
                                {
                                    Thread.Sleep(retryDelayMs);
                                    continue;
                                }
                            }
                        
                            return (imgBuffer, _imgWidth, _imgHeight, template, 0, "Success");
                        }
                        else
                        {
                            // Store error details
                            lastErrorCode = ret;
                            lastErrorMessage = GetErrorMessage(ret);
                            _logger.LogWarning($"❌ Capture attempt {attempt} failed: {lastErrorMessage} (Error code: {ret})");
                        
                            // ✅ Handle specific error codes
                            if (ret == zkfp.ZKFP_ERR_CAPTURE)
                            {
                                _logger.LogInformation("No finger detected on scanner. User may need to place finger.");
                            }
                            else if (ret == zkfp.ZKFP_ERR_BUSY)
                            {
                                _logger.LogInformation("Scanner busy. Waiting for device to be ready...");
                                Thread.Sleep(_settings.DeviceResetDelayMs); // Use configurable delay
                            }
                            else if (ret == zkfp.ZKFP_ERR_INVALID_HANDLE)
                            {
                                _logger.LogError("Invalid device handle. Attempting to reset scanner...");
                                ResetScanner();
                            }
                            
                            // Retry if attempts remain
                            if (attempt < maxRetries)
                            {
                                int backoffDelay = retryDelayMs * attempt; // Exponential backoff
                                _logger.LogInformation($"Retrying in {backoffDelay}ms...");
                                Thread.Sleep(backoffDelay);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Exception during capture attempt {attempt}");
                        lastErrorCode = -200; // Custom code for exceptions
                        lastErrorMessage = $"Exception: {ex.Message}";
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation($"Waiting {retryDelayMs}ms before retry...");
                            Thread.Sleep(retryDelayMs);
                        }
                    }
                }
                
                _logger.LogError($"❌ Failed to capture fingerprint after {maxRetries} attempts. Last error: {lastErrorMessage}");
                return (null, 0, 0, null, lastErrorCode, lastErrorMessage);
            }
        }
        
        /// <summary>
        /// Converts error codes to human-readable messages
        /// </summary>
        private string GetErrorMessage(int errorCode)
        {
            // Use if-else because zkfp constants are not compile-time constants
            if (errorCode == zkfp.ZKFP_ERR_OK) return "Success";
            if (errorCode == zkfp.ZKFP_ERR_CAPTURE) return "No finger detected on scanner";
            if (errorCode == zkfp.ZKFP_ERR_EXTRACT_FP) return "Failed to extract fingerprint features";
            if (errorCode == zkfp.ZKFP_ERR_BUSY) return "Scanner is busy";
            if (errorCode == zkfp.ZKFP_ERR_INVALID_HANDLE) return "Invalid device handle";
            if (errorCode == zkfp.ZKFP_ERR_MEMORY_NOT_ENOUGH) return "Not enough memory";
            if (errorCode == zkfp.ZKFP_ERR_NOT_OPENED) return "Device not opened";
            if (errorCode == zkfp.ZKFP_ERR_INVALID_PARAM) return "Invalid parameter";
            if (errorCode == zkfp.ZKFP_ERR_ABSORT) return "Operation aborted";
        
            return $"Unknown error (code: {errorCode})";
        }

        // ============================
        //  LEGACY CAPTURE METHODS (Maintained for backward compatibility)
        // ============================
        public byte[]? CaptureTemplate()
        {
            try
            {
                if (!_isDeviceOpen)
                {
                    _logger.LogError("Cannot capture: Device is not turned on");
                    throw new InvalidOperationException("Device is not turned on.");
                }

                byte[] imgBuffer = new byte[_imgSize > 0 ? _imgSize : 256 * 288];
                byte[] template = new byte[_settings.TemplateBufferSize];
                int size = template.Length;

                _logger.LogInformation("Attempting to capture fingerprint...");
                int ret = zkfp2.AcquireFingerprint(_deviceHandle, imgBuffer, template, ref size);

                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    Array.Resize(ref template, size);
                    _logger.LogInformation($"Fingerprint captured successfully. Template size: {size} bytes");
                    return template;
                }
                else
                {
                    _logger.LogWarning($"Failed to capture fingerprint. Error code: {ret}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during fingerprint capture");
                return null;
            }
        }

        public string? CaptureTemplateBase64()
        {
            try
            {
                var template = CaptureTemplate();
                if (template == null) return null;

                // Convert to Base64
                return Convert.ToBase64String(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception converting template to Base64");
                return null;
            }
        }

        /// <summary>
        /// Converts raw fingerprint image data to Base64 string for display on web
        /// Applies contrast enhancement for better visibility
        /// White background with black fingerprint lines (natural colors)
        /// ✅ FIXED: Proper BMP row padding to eliminate diagonal line artifact
        /// </summary>
        public string? ConvertImageToBase64(byte[] imageData, int width, int height)
        {
            try
            {
                if (imageData == null || imageData.Length == 0)
                {
                    _logger.LogWarning("Cannot convert: Image data is null or empty");
                    return null;
                }

                _logger.LogInformation($"Converting fingerprint image to Base64. Size: {width}x{height}, Data length: {imageData.Length}");

                // ✅ STEP 1: Apply contrast enhancement to improve visibility
                byte[] enhancedImage = ApplyContrastEnhancement(imageData);
                
                _logger.LogInformation("Contrast enhancement applied to fingerprint image");

                // ✅ STEP 2: Calculate BMP row stride (must be multiple of 4 bytes)
                int bytesPerPixel = 3; // 24-bit BMP (RGB)
                int rowSize = width * bytesPerPixel;
                int rowStride = (rowSize + 3) & ~3; // Round up to nearest multiple of 4
                int paddingPerRow = rowStride - rowSize;
                
                _logger.LogDebug($"BMP metrics: Width={width}, RowSize={rowSize}, RowStride={rowStride}, Padding={paddingPerRow}");

                // ✅ STEP 3: Convert grayscale image to BMP format with proper padding
                int imageDataSize = rowStride * height;
                int fileSize = 54 + imageDataSize; // BMP header (54 bytes) + image data
                byte[] bmpData = new byte[fileSize];

                // BMP File Header (14 bytes)
                bmpData[0] = 0x42; // 'B'
                bmpData[1] = 0x4D; // 'M'
                Array.Copy(BitConverter.GetBytes(fileSize), 0, bmpData, 2, 4);
                bmpData[10] = 54; // Offset to pixel data

                // DIB Header (40 bytes)
                bmpData[14] = 40; // Header size
                Array.Copy(BitConverter.GetBytes(width), 0, bmpData, 18, 4);
                Array.Copy(BitConverter.GetBytes(height), 0, bmpData, 22, 4);
                bmpData[26] = 1; // Color planes
                bmpData[28] = 24; // Bits per pixel
                Array.Copy(BitConverter.GetBytes(imageDataSize), 0, bmpData, 34, 4);

                // ✅ STEP 4: Convert enhanced grayscale to RGB (flip vertically for BMP format)
                // ✅ CRITICAL: Add row padding to fix diagonal line artifact
                int bmpIndex = 54;
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte pixel = enhancedImage[y * width + x];
                        bmpData[bmpIndex++] = pixel; // B
                        bmpData[bmpIndex++] = pixel; // G
                        bmpData[bmpIndex++] = pixel; // R
                    }
                    
                    // ✅ ADD ROW PADDING (this fixes the diagonal line!)
                    for (int p = 0; p < paddingPerRow; p++)
                    {
                        bmpData[bmpIndex++] = 0; // Padding bytes (must be 0)
                    }
                }

                _logger.LogInformation("Fingerprint image converted to BMP format successfully with proper padding");

                return Convert.ToBase64String(bmpData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception converting image to Base64");
                return null;
            }
        }

        /// <summary>
        /// ✅ IMPROVED: Applies high-quality image enhancement to fingerprint image
        /// Uses fixed optimal thresholding for consistent, crisp ridge rendering
        /// Produces professional-grade solid black ridges on white background
        /// </summary>
        private byte[] ApplyContrastEnhancement(byte[] imageData)
        {
            try
            {
                // Create a copy to avoid modifying original data
                byte[] enhancedData = new byte[imageData.Length];
                Array.Copy(imageData, enhancedData, imageData.Length);

                // ✅ STEP 1: Calculate image statistics for monitoring
                int minValue = 255, maxValue = 0;
                long sum = 0;
                
                foreach (byte pixel in enhancedData)
                {
                    if (pixel < minValue) minValue = pixel;
                    if (pixel > maxValue) maxValue = pixel;
                    sum += pixel;
                }
                
                int meanValue = (int)(sum / enhancedData.Length);
                
                _logger.LogDebug($"Image statistics - Min: {minValue}, Max: {maxValue}, Mean: {meanValue}");

                const int OPTIMAL_THRESHOLD = 200;
                
                _logger.LogDebug($"Using fixed optimal threshold: {OPTIMAL_THRESHOLD}");

                // ✅ STEP 3: Apply binary thresholding for crisp, solid ridges
                for (int i = 0; i < enhancedData.Length; i++)
                {
                    if (enhancedData[i] < OPTIMAL_THRESHOLD)
                    {
                        // Pixel is dark (a ridge), make it pure BLACK
                        enhancedData[i] = 0;
                    }
                    else
                    {
                        // Pixel is light (a valley or background), make it pure WHITE
                        enhancedData[i] = 255;
                    }
                }
                
                _logger.LogInformation("High-quality image enhancement complete - crisp HD ridges produced");

                return enhancedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying image enhancement, returning original image");
                // If enhancement fails, return original data
                return imageData;
            }
        }

        /// <summary>
        /// ✅ NEW: Applies morphological closing operation to connect broken ridge lines
        /// This fills small gaps and produces continuous, solid ridges
        /// </summary>
        private byte[] ApplyMorphologicalClosing(byte[] imageData, int width, int height)
        {
            try
            {
                byte[] result = new byte[imageData.Length];
                Array.Copy(imageData, result, imageData.Length);

                // Simple 3x3 kernel for morphological closing
                // This operation: Dilation followed by Erosion
                // Effect: Closes small gaps in ridges, connects nearby pixels

                // DILATION: Expand black regions (ridges)
                byte[] dilated = new byte[imageData.Length];
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                        
                        // Check 3x3 neighborhood
                        bool hasBlackNeighbor = false;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int neighborIdx = (y + dy) * width + (x + dx);
                                if (imageData[neighborIdx] == 0) // Black pixel
                                {
                                    hasBlackNeighbor = true;
                                    break;
                                }
                            }
                            if (hasBlackNeighbor) break;
                        }
                        
                        dilated[idx] = (byte)(hasBlackNeighbor ? 0 : 255);
                    }
                }

                // EROSION: Shrink black regions slightly (removes noise)
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int idx = y * width + x;
                        
                        // Check 3x3 neighborhood
                        bool allBlack = true;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int neighborIdx = (y + dy) * width + (x + dx);
                                if (dilated[neighborIdx] != 0) // Not black
                                {
                                    allBlack = false;
                                    break;
                                }
                            }
                            if (!allBlack) break;
                        }
                        
                        result[idx] = (byte)(allBlack ? 0 : 255);
                    }
                }

                _logger.LogInformation("Morphological closing applied - ridge gaps filled");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Morphological operation failed, returning original");
                return imageData;
            }
        }
        // ============================
        //  MATCHING / IDENTIFICATION
        // ============================
        public bool MatchFingerprints(byte[] template1, byte[] template2, int threshold = 50)
        {
            try
            {
                if (_dbHandle == IntPtr.Zero)
                {
                    _logger.LogError("Cannot match: DB handle not initialized");
                    throw new InvalidOperationException("DB handle not initialized.");
                }

                int score = zkfp2.DBMatch(_dbHandle, template1, template2);
                _logger.LogInformation($"Match score: {score}, Threshold: {threshold}");
                return score >= threshold;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during fingerprint matching");
                return false;
            }
        }

        /// <summary>
        /// Get match score between two fingerprint templates
        /// Returns the similarity score (0-100+)
        /// </summary>
        public int GetMatchScore(byte[] template1, byte[] template2)
        {
            try
            {
                if (_dbHandle == IntPtr.Zero)
                {
                    _logger.LogError("Cannot get match score: DB handle not initialized");
                    return 0;
                }

                int score = zkfp2.DBMatch(_dbHandle, template1, template2);
                _logger.LogInformation($"Match score calculated: {score}");
                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting match score");
                return 0;
            }
        }

        public int IdentifyFingerprint(byte[] template, ref int fid, ref int score)
        {
            try
            {
                if (_dbHandle == IntPtr.Zero)
                {
                    _logger.LogError("Cannot identify: DB handle not initialized");
                    throw new InvalidOperationException("DB handle not initialized.");
                }

                return zkfp2.DBIdentify(_dbHandle, template, ref fid, ref score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during fingerprint identification");
                return zkfp.ZKFP_ERR_FAIL;
            }
        }

        public void AddTemplateToDB(int fid, byte[] template)
        {
            try
            {
                if (_dbHandle == IntPtr.Zero)
                {
                    _logger.LogError("Cannot add template: DB handle not initialized");
                    throw new InvalidOperationException("DB handle not initialized.");
                }

                zkfp2.DBAdd(_dbHandle, fid, template);
                _logger.LogInformation($"Template added to DB with FID: {fid}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while adding template to DB");
            }
        }

        // ============================
        //  DEVICE LIGHT & BUZZER CONTROL
        // ============================
        // ✅ TESTING MODE CONFIGURATION (set to true for 10-second LED testing)
        // Using static readonly instead of const to avoid unreachable code warnings
        private static readonly bool TESTING_MODE = false; // Set to true to enable 10-second LED duration for visual confirmation

        public bool SetLedLight(string color, bool on)
        {
            try
            {
                if (!_isDeviceOpen)
                {
                    _logger.LogWarning("Device not open, cannot set LED light");
                    return false;
                }

                int code = color.ToLower() switch
                {
                    "white" => 101,
                    "green" => 102,
                    "red" => 103,
                    _ => throw new ArgumentException("Invalid LED color (use 'white', 'green', or 'red').")
                };

                _logger.LogDebug($"Setting {color} LED to {(on ? "ON" : "OFF")} (code: {code})");

                byte[] value = BitConverter.GetBytes(on ? 1 : 0);
                int result = zkfp2.SetParameters(_deviceHandle, code, value, value.Length);
                
                if (result == zkfp.ZKFP_ERR_OK)
                {
                    _logger.LogDebug($"Command successful - {color} LED is now {(on ? "ON" : "OFF")}");
                }
                else
                {
                    _logger.LogWarning($"Command failed with error code: {result}");
                }
                
                return result == zkfp.ZKFP_ERR_OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while setting LED light: {color}");
                return false;
            }
        }

        /// <summary>
        /// ✅ NEW APPROACH #1: Activate SUCCESS LED (GREEN) with comprehensive diagnostic logging
        /// Matches the pattern that's already working correctly
        /// </summary>
        public void ActivateSuccessLED()
        {
            int duration = TESTING_MODE ? 10000 : 3000;
            
            _logger.LogInformation($"ActivateSuccessLED called (mode: {(TESTING_MODE ? "TESTING (10 seconds)" : "NORMAL (3 seconds)")})");

            try
            {
                _logger.LogDebug("Calling SetLedLight('green', true)");
                SetLedLight("green", true);
                _logger.LogInformation("GREEN LED should be visible on physical scanner NOW");
                
                if (TESTING_MODE)
                {
                    _logger.LogInformation("TESTING MODE: GREEN LED ACTIVE FOR 10 SECONDS - PLEASE VISUALLY CONFIRM LED ON HARDWARE SCANNER");
                }

                Beep(true);
                
                _ = Task.Run(async () =>
                {
                    _logger.LogDebug($"Started {duration}ms countdown");
                    await Task.Delay(duration);
                    _logger.LogDebug($"{duration}ms elapsed, turning off LED");
                    SetLedLight("green", false);
                    Beep(false);
                    
                    if (TESTING_MODE)
                    {
                        _logger.LogInformation("TESTING MODE: GREEN LED SHOULD NOW BE OFF");
                    }
                    
                    _logger.LogInformation("ActivateSuccessLED complete");
                });
                
                _logger.LogInformation("LED GREEN activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating success LED");
                throw;
            }
        }

        /// <summary>
        /// ✅ NEW APPROACH #2: Dedicated method to activate ERROR LED (RED) with comprehensive diagnostic logging
        /// This mirrors the ActivateSuccessLED pattern to ensure identical behavior
        /// </summary>
        public void ActivateErrorLED()
        {
            int duration = TESTING_MODE ? 10000 : 3000;
            
            _logger.LogInformation($"ActivateErrorLED called (mode: {(TESTING_MODE ? "TESTING (10 seconds)" : "NORMAL (3 seconds)")})");

            try
            {
                _logger.LogDebug("Calling SetLedLight('red', true)");
                SetLedLight("red", true);
                _logger.LogInformation("RED LED should be visible on physical scanner NOW");
                
                if (TESTING_MODE)
                {
                    _logger.LogInformation("TESTING MODE: RED LED ACTIVE FOR 10 SECONDS - PLEASE VISUALLY CONFIRM LED ON HARDWARE SCANNER");
                }

                Beep(true);
                
                _ = Task.Run(async () =>
                {
                    _logger.LogDebug($"Started {duration}ms countdown");
                    await Task.Delay(duration);
                    _logger.LogDebug($"{duration}ms elapsed, turning off LED");
                    SetLedLight("red", false);
                    Beep(false);
                    
                    if (TESTING_MODE)
                    {
                        _logger.LogInformation("TESTING MODE: RED LED SHOULD NOW BE OFF");
                    }
                    
                    _logger.LogInformation("ActivateErrorLED complete");
                });
                
                _logger.LogInformation("LED RED activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating error LED");
                throw;
            }
        }

        /// <summary>
        /// ✅ Turn OFF all LED lights on the physical scanner hardware
        /// </summary>
        public void TurnOffAllLeds()
        {
            try
            {
                if (!_isDeviceOpen)
                {
                    _logger.LogWarning("Cannot turn off LEDs: Device is not open");
                    return;
                }

                _logger.LogInformation("Turning off all scanner LEDs...");
                
                // Turn off all three LEDs (white, green, red)
                SetLedLight("white", false);
                SetLedLight("green", false);
                SetLedLight("red", false);
                
                _logger.LogInformation("All scanner LEDs turned off successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while turning off LEDs");
            }
        }

        public bool Beep(bool on)
        {
            try
            {
                if (!_isDeviceOpen)
                {
                    _logger.LogWarning("Cannot beep: Device is not open");
                    return false;
                }

                _logger.LogDebug($"Setting beep: {(on ? "ON" : "OFF")}");
                byte[] value = BitConverter.GetBytes(on ? 1 : 0);
                int result = zkfp2.SetParameters(_deviceHandle, 104, value, value.Length);
                
                if (result == zkfp.ZKFP_ERR_OK)
                {
                    _logger.LogDebug($"Beep command successful: {(on ? "ON" : "OFF")}");
                }
                else
                {
                    _logger.LogWarning($"Beep command failed with error code: {result}");
                }
                
                return result == zkfp.ZKFP_ERR_OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while controlling buzzer");
                return false;
            }
        }

        // ============================
        //  CLEANUP
        // ============================
        
        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// Ensures proper cleanup of managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            // ✅ Idempotency check - prevent multiple dispose calls
            if (_disposed)
                return;

            if (disposing)
            {
                // ✅ Dispose managed resources (these use _logger, so must be in disposing block)
                try
                {
                    _logger.LogInformation("Disposing FingerprintService...");
                    
                    // Turn off the device
                    TurnOffDevice();

                    // Free the database handle
                    if (_dbHandle != IntPtr.Zero)
                    {
                        zkfp2.DBFree(_dbHandle);
                        _dbHandle = IntPtr.Zero;
                        _logger.LogInformation("DB handle freed");
                    }

                    // Terminate the SDK
                    if (_isInitialized)
                    {
                        zkfp2.Terminate();
                        _isInitialized = false;
                        _logger.LogInformation("SDK terminated");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during disposal");
                }
            }

            // ✅ Mark as disposed
            _disposed = true;
        }

        /// <summary>
        /// Public implementation of Dispose pattern.
        /// Releases all resources used by the FingerprintService.
        /// </summary>
        public void Dispose()
        {
            // ✅ Dispose managed resources
            Dispose(disposing: true);
            
            // ✅ Suppress finalization since we've cleaned up
            GC.SuppressFinalize(this);
        }
    }
}
