using System.Runtime.InteropServices;

namespace Fingerprint_Attendance_System.Services
{
    /// <summary>
    /// P/Invoke wrapper for ZKTeco Fingerprint SDK
 /// Based on ZKFinger Standard SDK documentation
    /// </summary>
    public static class ZKFPNative
    {
        // Error codes
        public const int ZKFP_ERR_OK = 0;
 public const int ZKFP_ERR_ALREADY_INIT = 1;
        public const int ZKFP_ERR_INITLIB = -1;
        public const int ZKFP_ERR_INIT = -2;
     public const int ZKFP_ERR_NO_DEVICE = -3;
    public const int ZKFP_ERR_NOT_SUPPORT = -4;
      public const int ZKFP_ERR_INVALID_PARAM = -5;
      public const int ZKFP_ERR_OPEN = -6;
        public const int ZKFP_ERR_INVALID_HANDLE = -7;
        public const int ZKFP_ERR_CAPTURE = -8;
     public const int ZKFP_ERR_EXTRACT_FP = -9;
        public const int ZKFP_ERR_ABSORT = -10;
        public const int ZKFP_ERR_MEMORY_NOT_ENOUGH = -11;
        public const int ZKFP_ERR_BUSY = -12;
        public const int ZKFP_ERR_ADD_FINGER = -13;
        public const int ZKFP_ERR_DEL_FINGER = -14;
      public const int ZKFP_ERR_FAIL = -17;
        public const int ZKFP_ERR_CANCEL = -18;
        public const int ZKFP_ERR_VERIFY_FP = -20;
        public const int ZKFP_ERR_MERGE = -22;
        public const int ZKFP_ERR_NOT_OPENED = -23;
        public const int ZKFP_ERR_NOT_INIT = -24;
  public const int ZKFP_ERR_ALREADY_OPENED = -25;

        // Parameter codes
        public const int ZKFP_PARAM_WIDTH = 1;
        public const int ZKFP_PARAM_HEIGHT = 2;
     public const int ZKFP_PARAM_DPI = 3;
        public const int ZKFP_PARAM_IMG_SIZE = 106;
     public const int ZKFP_PARAM_WHITE_LED = 101;
        public const int ZKFP_PARAM_GREEN_LED = 102;
   public const int ZKFP_PARAM_RED_LED = 103;
        public const int ZKFP_PARAM_BEEP = 104;

        // SDK Initialization and Cleanup
        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_Init", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Init();

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_Terminate", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Terminate();

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_GetDeviceCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetDeviceCount();

        // Device Management
        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_OpenDevice", CallingConvention = CallingConvention.Cdecl)]
      public static extern IntPtr OpenDevice(int index);

 [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_CloseDevice", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseDevice(IntPtr handle);

        // Parameters
        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_SetParameters", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetParameters(IntPtr handle, int code, byte[] value, int size);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_GetParameters", CallingConvention = CallingConvention.Cdecl)]
      public static extern int GetParameters(IntPtr handle, int code, byte[] value, ref int size);

// Fingerprint Capture
   [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_AcquireFingerprint", CallingConvention = CallingConvention.Cdecl)]
        public static extern int AcquireFingerprint(IntPtr handle, byte[] imgBuffer, byte[] template, ref int size);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_AcquireFingerprintImage", CallingConvention = CallingConvention.Cdecl)]
    public static extern int AcquireFingerprintImage(IntPtr handle, byte[] imgBuffer);

        // Database Management
  [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBInit", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DBInit();

     [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBFree", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DBFree(IntPtr dbHandle);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern int DBAdd(IntPtr dbHandle, int fid, byte[] template);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBDel", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DBDel(IntPtr dbHandle, int fid);

     [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBClear", CallingConvention = CallingConvention.Cdecl)]
     public static extern int DBClear(IntPtr dbHandle);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBMatch", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DBMatch(IntPtr dbHandle, byte[] template1, byte[] template2);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBIdentify", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DBIdentify(IntPtr dbHandle, byte[] template, ref int fid, ref int score);

        [DllImport("libzkfp.dll", EntryPoint = "ZKFPM_DBMerge", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DBMerge(IntPtr dbHandle, byte[] temp1, byte[] temp2, byte[] temp3, byte[] regTemp, ref int size);

        // Helper functions
        public static void ByteArray2Int(byte[] buf, ref int value)
        {
     if (buf != null && buf.Length >= 4)
       {
                value = BitConverter.ToInt32(buf, 0);
     }
        }

        public static void Int2ByteArray(int value, byte[] buf)
     {
     if (buf != null && buf.Length >= 4)
        {
     byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, buf, 4);
            }
        }

      public static string Blob2Base64String(byte[] blob, int len)
        {
            if (blob == null || len <= 0)
        return string.Empty;
            
        byte[] trimmed = new byte[len];
 Array.Copy(blob, trimmed, len);
       return Convert.ToBase64String(trimmed);
        }

        public static byte[]? Base64String2Blob(string base64)
        {
     if (string.IsNullOrEmpty(base64))
         return null;
       
 return Convert.FromBase64String(base64);
        }
    }
}
