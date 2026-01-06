using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BluetoothMonitor.Core.Utils
{
    internal static class SetupAPI
    {
        /// <summary>
        /// Returns a handle to a device information set that contains requested device information elements for a local computer.
        /// This is a thin P/Invoke for the native SetupDiGetClassDevsW function from setupapi.dll.
        /// </summary>
        /// <param name="ClassGuid">Optional pointer to a device setup class GUID (pass IntPtr.Zero to use the Enumerator/flags to filter).</param>
        /// <param name="Enumerator">Optional device instance ID or device enumerator (for example, "USB" or "Bluetooth"). Can be null or empty.</param>
        /// <param name="hwndParent">Reserved; must be <see cref="IntPtr.Zero"/> in most callers.</param>
        /// <param name="Flags">A combination of <see cref="DeviceFiter"/> values that control which devices are returned.</param>
        /// <returns>
        /// A handle to a device information set (HDEVINFO) on success; <see cref="IntPtr.Zero"/> on failure. Call <see cref="Marshal.GetLastWin32Error"/> to get the error code.
        /// </returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
           IntPtr ClassGuid,
           [MarshalAs(UnmanagedType.LPWStr)] string? Enumerator,
           IntPtr hwndParent,
           DeviceFiter Flags);

        /// <summary>
        /// Destroys a device information set and frees associated memory.
        /// This corresponds to the native SetupDiDestroyDeviceInfoList function.
        /// </summary>
        /// <param name="DeviceInfoSet">Handle returned by <see cref="SetupDiGetClassDevs"/>.</param>
        /// <returns>True on success; false on failure. Use <see cref="Marshal.GetLastWin32Error"/> to retrieve the Win32 error code when false is returned.</returns>
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        /// <summary>
        /// Enumerates the devices in a device information set.
        /// This maps to the native SetupDiEnumDeviceInfo function.
        /// </summary>
        /// <param name="DeviceInfoSet">Handle to the device information set returned by <see cref="SetupDiGetClassDevs"/>.</param>
        /// <param name="MemberIndex">Zero-based index of the device to retrieve.</param>
        /// <param name="DeviceInfoData">Receives an <see cref="SP_DEVINFO_DATA"/> structure for the device on success.</param>
        /// <returns>
        /// True if a device was retrieved. If the function returns false, call <see cref="Marshal.GetLastWin32Error"/>; when there are no more items the error will typically be ERROR_NO_MORE_ITEMS.
        /// </returns>
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(
           IntPtr DeviceInfoSet,
           int MemberIndex,
           ref SP_DEVINFO_DATA DeviceInfoData);

        /// <summary>
        /// Retrieves a device property identified by a DEVPROPKEY. This overload is used to retrieve a raw buffer.
        /// This maps to the SetupDiGetDevicePropertyW function in setupapi.dll.
        /// </summary>
        /// <param name="DeviceInfoSet">Handle to the device information set.</param>
        /// <param name="DeviceInfoData">Device information returned by <see cref="SetupDiEnumDeviceInfo"/>.</param>
        /// <param name="PropertyKey">Property key (DEVPROPKEY) identifying the property to retrieve.</param>
        /// <param name="PropertyType">Receives the DEVPROPTYPE (property type) on success.</param>
        /// <param name="PropertyBuffer">Pointer to a buffer that receives the property data. When querying the required size, pass <see cref="IntPtr.Zero"/> and <paramref name="PropertyBufferSize"/> should be 0.</param>
        /// <param name="PropertyBufferSize">Size of <paramref name="PropertyBuffer"/> in bytes.</param>
        /// <param name="RequiredSize">Receives the number of bytes required to store the property value (including any terminating null).</param>
        /// <param name="Flags">Reserved flags; normally 0.</param>
        /// <returns>True on success; false on failure.</returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDeviceProperty(
          IntPtr DeviceInfoSet,
          ref SP_DEVINFO_DATA DeviceInfoData,
          ref DEVPROPKEY PropertyKey,
          out int PropertyType,
          IntPtr PropertyBuffer,
          int PropertyBufferSize,
          out int RequiredSize,
          int Flags);

        /// <summary>
        /// Retrieves a device property into a 2-byte buffer (useful for properties that are a 16-bit integer).
        /// This is a convenience P/Invoke overload that maps the native call into an <c>out ushort</c> buffer.
        /// </summary>
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDeviceProperty(
          IntPtr DeviceInfoSet,
          ref SP_DEVINFO_DATA DeviceInfoData,
          ref DEVPROPKEY PropertyKey,
          out int PropertyType,
          out ushort PropertyBuffer,
          int PropertyBufferSize,
          out int RequiredSize,
          int Flags);

        /// <summary>
        /// Retrieves a device property into a 1-byte buffer (useful for properties that are a single byte).
        /// This is a convenience P/Invoke overload that maps the native call into an <c>out byte</c> buffer.
        /// </summary>
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDeviceProperty(
          IntPtr DeviceInfoSet,
          ref SP_DEVINFO_DATA DeviceInfoData,
          ref DEVPROPKEY PropertyKey,
          out int PropertyType,
          out byte PropertyBuffer,
          int PropertyBufferSize,
          out int RequiredSize,
          int Flags);

        /// <summary>
        /// Helper to read a Unicode string property from a device using <see cref="SetupDiGetDeviceProperty"/>.
        /// It first queries the required buffer size, allocates unmanaged memory with CoTaskMemAlloc, and then retrieves the string.
        /// </summary>
        /// <param name="hdevinfo">Device information set handle.</param>
        /// <param name="data">Device info data for the device.</param>
        /// <param name="pk">Property key to read.</param>
        /// <returns>Property string value, or null if the property is not present or an error occurs.</returns>
        public static string? GetStringProperty(
          IntPtr hdevinfo,
          ref SP_DEVINFO_DATA data,
          DEVPROPKEY pk)
        {
            try
            {
                SetupDiGetDeviceProperty(hdevinfo, ref data, ref pk, out int PropertyType, IntPtr.Zero, 0, out int RequiredSize, 0);
                if (RequiredSize <= 0)
                    return null;
                IntPtr num = Marshal.AllocCoTaskMem(RequiredSize);
                try
                {
                    if (!SetupDiGetDeviceProperty(hdevinfo, ref data, ref pk, out PropertyType, num, RequiredSize, out RequiredSize, 0))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    // RequiredSize is in bytes and includes the terminating null; convert to characters and remove terminator
                    return Marshal.PtrToStringUni(num, RequiredSize / 2 - 1);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(num);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Helper to read a 16-bit unsigned integer property from a device. Returns null if the property is not present or on error.
        /// Uses the 2-byte overload of <see cref="SetupDiGetDeviceProperty"/>.
        /// </summary>
        public static ushort? GetUshortProperty(
          IntPtr hdevinfo,
          ref SP_DEVINFO_DATA data,
          DEVPROPKEY pk)
        {
            try
            {
                if (SetupDiGetDeviceProperty(hdevinfo, ref data, ref pk, out int _, out ushort PropertyBuffer, 2, out int _, 0))
                {
                    return new ushort?(PropertyBuffer);
                }
                else
                {
                    return new ushort?();
                }
            }
            catch (Exception)
            {
                return new ushort?();
            }
        }

        /// <summary>
        /// Helper to read an 8-bit unsigned integer property from a device. Returns null if the property is not present or on error.
        /// Uses the 1-byte overload of <see cref="SetupDiGetDeviceProperty"/>.
        /// </summary>
        public static byte? GetByteProperty(
          IntPtr hdevinfo,
          ref SP_DEVINFO_DATA data,
          DEVPROPKEY pk)
        {
            try
            {
                if (SetupDiGetDeviceProperty(hdevinfo, ref data, ref pk, out int _, out byte PropertyBuffer, 1, out int _, 0))
                {
                    return new byte?(PropertyBuffer);
                }
                else
                {
                    return new byte?();
                }
            }
            catch (Exception)
            {
                return new byte?();
            }
        }
    }

    /// <summary>
    /// Structure that receives information about a device in a device information set.
    /// Mirrors the native SP_DEVINFO_DATA structure used by the SetupAPI.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVINFO_DATA
    {
        /// <summary>
        /// Size, in bytes, of this structure. Initialize this to <c>Marshal.SizeOf(typeof(SP_DEVINFO_DATA))</c> before calling enumeration functions.
        /// </summary>
        public int cbSize;
        /// <summary>
        /// Device setup class GUID for the device.
        /// </summary>
        public Guid ClassGuid;
        /// <summary>
        /// Device instance (DevInst) as maintained by the configuration manager.
        /// </summary>
        public int DevInst;
        /// <summary>
        /// Reserved. Do not use.
        /// </summary>
        public IntPtr Reserved;
    }

    /// <summary>
    /// Identifies a device property by property set GUID (fmtid) and property identifier (pid).
    /// Mirrors the native DEVPROPKEY structure.
    /// </summary>
    internal struct DEVPROPKEY
    {
        public Guid fmtid;
        public int pid;
    }

    /// <summary>
    /// Flags used by <see cref="SetupDiGetClassDevs"/> to filter the returned device set.
    /// </summary>
    [Flags]
    public enum DeviceFiter
    {
        Default = 1,
        Present = 2,
        AllClasses = 4,
        Profile = 8,
        DeviceInterface = 16, // 0x00000010
    }
}
