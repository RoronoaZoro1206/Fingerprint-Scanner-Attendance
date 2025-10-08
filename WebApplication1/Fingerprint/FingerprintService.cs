using System;
using libzkfpcsharp;

namespace WebApplication1.Fingerprint;

public class FingerprintService
{
    private IntPtr _deviceHandle = IntPtr.Zero;
    private IntPtr _dbHandle = IntPtr.Zero;
    private int _imageWidth, _imageHeight;

    private string _lastError = string.Empty;
    public string LastError => _lastError;
    public int DeviceCount { get; private set; }
    public int ImageWidth => _imageWidth;
    public int ImageHeight => _imageHeight;
    public bool IsInitialized => _deviceHandle != IntPtr.Zero;

    public bool Initialize()
    {
        int ret = zkfp2.Init();
        if (ret != zkfp.ZKFP_ERR_OK)
        {
            _lastError = $"zkfp2.Init failed: {ret}";
            return false;
        }

        DeviceCount = zkfp2.GetDeviceCount();
        if (DeviceCount <= 0)
        {
            _lastError = "No fingerprint devices found.";
            return false;
        }

        _deviceHandle = zkfp2.OpenDevice(0);
        if (_deviceHandle == IntPtr.Zero)
        {
            _lastError = "Failed to open fingerprint device.";
            return false;
        }
        _dbHandle = zkfp2.DBInit();

        byte[] paramValue = new byte[4];
        int size = 4;
        zkfp2.GetParameters(_deviceHandle, 1, paramValue, ref size);
        zkfp2.ByteArray2Int(paramValue, ref _imageWidth);

        size = 4;
        zkfp2.GetParameters(_deviceHandle, 2, paramValue, ref size);
        zkfp2.ByteArray2Int(paramValue, ref _imageHeight);

        _lastError = string.Empty;
        return true;
    }

    public bool CloseAndReinitialize()
    {
        Close();
        return Initialize();
    }

    public bool CloseDeviceOnly()
    {
        if (_deviceHandle != IntPtr.Zero)
        {
            zkfp2.CloseDevice(_deviceHandle);
            _deviceHandle = IntPtr.Zero;
            return true;
        }
        return false;
    }

    public byte[] CaptureFingerprintTemplate()
    {
        if (_deviceHandle == IntPtr.Zero) return null;

        byte[] imgBuffer = new byte[_imageWidth * _imageHeight];
        byte[] template = new byte[2048];
        int size = 2048;

        int ret = zkfp2.AcquireFingerprint(_deviceHandle, imgBuffer, template, ref size);
        return ret == zkfp.ZKFP_ERR_OK ? template : null;
    }

    public void Close()
    {
        if (_deviceHandle != IntPtr.Zero)
        {
            zkfp2.CloseDevice(_deviceHandle);
            _deviceHandle = IntPtr.Zero;
        }

        if (_dbHandle != IntPtr.Zero)
        {
            zkfp2.DBFree(_dbHandle);
            _dbHandle = IntPtr.Zero;
        }

        zkfp2.Terminate();
    }
}