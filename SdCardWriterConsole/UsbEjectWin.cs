using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SDCardCreatorConsole;

public class UsbEjectWin : IEject
{
    private const int INVALID_HANDLE_VALUE = -1;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const int FILE_SHARE_READ = 0x1;
    private const int FILE_SHARE_WRITE = 0x2;
    private const int OPEN_EXISTING = 3;
    private const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
        uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    public bool Unmount(string[] drivePaths)
    {
        bool success = true;
  
        foreach (var drivePath in drivePaths)
        {
            try
            {
                EjectDrive(drivePath);
            }
            catch (Exception ex)
            {
                success = false;
                Console.WriteLine($"Failed to eject drive {drivePath}: {ex.Message}");
            }
        }
        return success;
    }

    private void EjectDrive(string drivePath)
    {
        var dosDevicePath = $"\\\\.\\{drivePath.TrimEnd('\\')}";
        using var handle = CreateFile(dosDevicePath,
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to get handle for drive {drivePath}");
        }

        uint bytesReturned;
        bool result = DeviceIoControl(handle,
            IOCTL_STORAGE_EJECT_MEDIA,
            IntPtr.Zero,
            0,
            IntPtr.Zero,
            0,
            out bytesReturned,
            IntPtr.Zero);

        if (!result)
        {
            throw new InvalidOperationException($"Failed to eject drive {drivePath}");
        }
    }
}