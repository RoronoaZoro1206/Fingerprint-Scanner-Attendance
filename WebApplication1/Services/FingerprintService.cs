using libzkfpcsharp;

namespace Fingerprint_Attendance_System.Services
{
    public class FingerprintService : IDisposable
    {
     private IntPtr _deviceHandle = IntPtr.Zero;
 private IntPtr _dbHandle = IntPtr.Zero;
        private bool _isInitialized = false;
        private bool _isDeviceOpen = false;
        private readonly ILogger<FingerprintService> _logger;

  private int _imgWidth = 0;
      private int _imgHeight = 0;
    private int _imgSize = 0;

        public FingerprintService(ILogger<FingerprintService> logger)
     {
        _logger = logger;
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
        //  FINGERPRINT CAPTURE
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
     byte[] template = new byte[2048];
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

    // ============================
    //  FINGERPRINT CAPTURE
    // ============================
    
    /// <summary>
/// Captures fingerprint image and template together
    /// Returns both the raw image data and template
    /// </summary>
    public (byte[]? imageData, int width, int height, byte[]? template) CaptureFingerprintWithImage()
    {
        try
        {
            if (!_isDeviceOpen)
            {
                _logger.LogError("Cannot capture: Device is not turned on");
      throw new InvalidOperationException("Device is not turned on.");
            }

         byte[] imgBuffer = new byte[_imgSize > 0 ? _imgSize : 256 * 288];
byte[] template = new byte[2048];
         int size = template.Length;

            _logger.LogInformation("Attempting to capture fingerprint with image...");
     int ret = zkfp2.AcquireFingerprint(_deviceHandle, imgBuffer, template, ref size);

            if (ret == zkfp.ZKFP_ERR_OK)
            {
 Array.Resize(ref template, size);
        _logger.LogInformation($"Fingerprint captured successfully. Image: {_imgWidth}x{_imgHeight}, Template: {size} bytes");
       
    // Return image data, dimensions, and template
           return (imgBuffer, _imgWidth, _imgHeight, template);
            }
            else
          {
 _logger.LogWarning($"Failed to capture fingerprint. Error code: {ret}");
    return (null, 0, 0, null);
            }
    }
        catch (Exception ex)
        {
         _logger.LogError(ex, "Exception during fingerprint capture with image");
return (null, 0, 0, null);
    }
    }

  /// <summary>
    /// Converts raw fingerprint image data to Base64 string for display on web
    /// White background with black fingerprint lines (natural colors)
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

      // Convert grayscale image to BMP format for web display
      // BMP Header (54 bytes) + Image Data
          int fileSize = 54 + (width * height * 3); // 3 bytes per pixel for 24-bit BMP
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
            Array.Copy(BitConverter.GetBytes(width * height * 3), 0, bmpData, 34, 4);

 // Convert grayscale to RGB (flip vertically for BMP format)
     // Keep natural colors: white background (255), black lines (0)
   int bmpIndex = 54;
            for (int y = height - 1; y >= 0; y--)
            {
        for (int x = 0; x < width; x++)
   {
        byte pixel = imageData[y * width + x];
        bmpData[bmpIndex++] = pixel; // B
       bmpData[bmpIndex++] = pixel; // G
     bmpData[bmpIndex++] = pixel; // R
     }
       }

            return Convert.ToBase64String(bmpData);
        }
    catch (Exception ex)
        {
            _logger.LogError(ex, "Exception converting image to Base64");
            return null;
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
        public bool SetLedLight(string color, bool on)
        {
       try
 {
 if (!_isDeviceOpen) return false;

      int code = color.ToLower() switch
        {
            "white" => 101,
             "green" => 102,
        "red" => 103,
      _ => throw new ArgumentException("Invalid LED color (use 'white', 'green', or 'red').")
  };

        byte[] value = BitConverter.GetBytes(on ? 1 : 0);
    int result = zkfp2.SetParameters(_deviceHandle, code, value, value.Length);
     return result == zkfp.ZKFP_ERR_OK;
     }
            catch (Exception ex)
{
        _logger.LogError(ex, $"Exception while setting LED light: {color}");
    return false;
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

             _logger.LogInformation($"Setting beep: {(on ? "ON" : "OFF")}");
        byte[] value = BitConverter.GetBytes(on ? 1 : 0);
     int result = zkfp2.SetParameters(_deviceHandle, 104, value, value.Length);
           
         if (result == zkfp.ZKFP_ERR_OK)
     {
  _logger.LogInformation($"Beep command successful: {(on ? "ON" : "OFF")}");
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
        public void Dispose()
   {
  try
          {
    _logger.LogInformation("Disposing FingerprintService...");
  TurnOffDevice();

     if (_dbHandle != IntPtr.Zero)
     {
     zkfp2.DBFree(_dbHandle);
        _dbHandle = IntPtr.Zero;
           _logger.LogInformation("DB handle freed");
      }

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
    }
}
