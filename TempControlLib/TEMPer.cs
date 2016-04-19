using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Diagnostics;
using log4net;

/// <summary>
/// TEMPer device types.
/// </summary>
public enum TEMPerDeviceType
{
    None,
    TEMPer,
    TEMPerHUM
}

/// <summary>
/// Provides access to a PCsensor TEMPer and TEMPerHUM device for reading temperature and humidity
/// and setting calibration values.
/// </summary>
/// <remarks>
/// <para>
/// This class uses native functions from the RDingUSB.dll library for TEMPerHUM and HidFTDll.dll
/// for TEMPer devices and should therefore be built for the x86 target. Only device types for
/// which the DLL file is present are supported.
/// </para>
/// <para>
/// Multiple indexed devices can only addressed for the TEMPer device type.
/// </para>
/// </remarks>
public class TEMPer : IDisposable
{
    #region Native methods
    /// <summary>
    /// Native methods for TEMPer access.
    /// </summary>
    private static class EntryAPI
    {
        [DllImport("HidFTDll.dll")]
        public static extern void EMyCloseDevice();
        [DllImport("HidFTDll.dll")]
        public static extern int EMyDetectDevice(long myhwnd);
        [DllImport("HidFTDll.dll")]
        public static extern bool EMyInitConfig(bool dOrc);
        [DllImport("HidFTDll.dll")]
        public static extern bool EMyReadEP(ref byte up1, ref byte up2, ref byte up3, ref byte up4, ref byte up5, ref byte up6);
        [DllImport("HidFTDll.dll")]
        public static extern bool EMyReadHid([MarshalAs(UnmanagedType.AnsiBStr)] ref string pcBuffer, byte btUrlIndex, int btUrlLen);
        [DllImport("HidFTDll.dll")]
        public static extern double EMyReadTemp(bool flag);
        [DllImport("HidFTDll.dll")]
        public static extern void EMySetCurrentDev(int nCurDev);
        [DllImport("HidFTDll.dll")]
        public static extern bool EMyWriteEP(ref byte fd1, ref byte fd2, ref byte fd3, ref byte fd4, ref byte fd5);
        [DllImport("HidFTDll.dll")]
        public static extern bool EMyWriteHid(ref char[] pcBuffer, byte btUrlIndex, int btUrlLen);
        [DllImport("HidFTDll.dll")]
        public static extern bool EMyWriteTempText(bool flag);
    }

    /// <summary>
    /// Native methods for TEMPerHUM access.
    /// </summary>
    private static class RDing
    {
        [DllImport("RDingUSB.dll")]
        public static extern IntPtr CloseUSBDevice(IntPtr deviceHandle);
        [DllImport("RDingUSB.dll")]
        public static extern uint GetErrorMsg(ref string[] lpErrorMsg, uint dwErrorMsgSize);
        [DllImport("RDingUSB.dll")]
        public static extern ushort GetInputLength(IntPtr deviceHandle);
        [DllImport("RDingUSB.dll")]
        public static extern ushort GetOutputLength(IntPtr deviceHandle);
        [DllImport("RDingUSB.dll")]
        public static extern IntPtr OpenUSBDevice(int vid, int pid);
        [DllImport("RDingUSB.dll")]
        public static extern bool ReadUSB(IntPtr deviceHandle, byte[] buffer, int bytesCount, ref ulong numberOfBytesRead);
        [DllImport("RDingUSB.dll")]
        public static extern bool WriteUSB(IntPtr deviceHandle, byte[] buffer, int bytesCount, ref ulong numberOfBytesWritten);
    }
    #endregion Native methods

    #region USB data
    private static int vid = 0xC45;
    private static int pid = 0x7401;
    private static byte[] readTemperCmd = new byte[] { 0, 1, 0x80, 0x33, 1, 0, 0, 0, 0 };
    private static byte[] getCalibrationCmd = new byte[] { 0, 1, 130, 0x77, 1, 0, 0, 0, 0 };
    private static byte[] getVersionCmd = new byte[] { 0, 1, 0x86, 0xff, 1, 0, 0, 0, 0 };
    private static byte[] eraseFlashCmd = new byte[] { 0, 1, 0x85, 0xdd, 1, 1, 0, 0, 0 };
    private static byte[] writeDataCmd = new byte[] { 0, 1, 0x81, 0x55, 1, 0, 0, 0, 0 };
    private static byte[] saveFlashCmd = new byte[] { 0, 1, 0x85, 0x11, 0, 0, 0, 0, 0 };
    #endregion USB data

    #region Private fields
    private object accessLock = new object();
    private TEMPerDeviceType deviceType;
    private TEMPerDeviceType preferredDeviceType;
    private int preferredDeviceNum;
    private IntPtr deviceHandle;
    private string deviceVersion;
    private double currentTemperature;
    private double currentHumidity;
    private double temperatureCorrection;
    private double humidityCorrection;
    #endregion Private fields
    private static readonly ILog logger =
           LogManager.GetLogger(typeof(TEMPer));

    #region Static methods
    /// <summary>
    /// Detects all present and supported devices.
    /// </summary>
    /// <returns>Array of device types.</returns>
    public static TEMPerDeviceType[] DetectDevices()
    {
        List<TEMPerDeviceType> deviceList = new List<TEMPerDeviceType>();

        try
        {
            IntPtr deviceHandle = RDing.OpenUSBDevice(vid, pid);
            if (deviceHandle.ToInt64() != -1)
            {
                deviceList.Add(TEMPerDeviceType.TEMPerHUM);
                RDing.CloseUSBDevice(deviceHandle);
            }
        }
        catch
        {
            // No error if DLL is missing
        }

        try
        {
            int deviceCount = EntryAPI.EMyDetectDevice(0);
            while (deviceCount-- > 0)
            {
                deviceList.Add(TEMPerDeviceType.TEMPer);
            }
        }
        catch
        {
            // No error if DLL is missing
        }

        return deviceList.ToArray();
    }
    #endregion Static methods

    #region Properties
    /// <summary>
    /// Gets the current temperature value in Â°C.
    /// </summary>
    /// <remarks>
    /// Call ReadValues() before accessing this value.
    /// </remarks>
    public double Temperature
    {
        get
        {
            if (!SupportsTemperature)
                throw new NotSupportedException("This operation is not supported with the current device");

            return currentTemperature;
        }
    }

    /// <summary>
    /// Gets the current humidity value in %.
    /// </summary>
    /// <remarks>
    /// Call ReadValues() before accessing this value.
    /// </remarks>
    public double Humidity
    {
        get
        {
            if (!SupportsHumidity)
                throw new NotSupportedException("This operation is not supported with the current device");

            return currentHumidity;
        }
    }

    /// <summary>
    /// Gets the current dew point value in Â°C.
    /// </summary>
    /// <remarks>
    /// Call ReadValues() before accessing this value.
    /// </remarks>
    public double DewPoint
    {
        get
        {
            if (!SupportsTemperature || !SupportsHumidity)
                throw new NotSupportedException("This operation is not supported with the current device");

            // Source: http://www.wetterochs.de/wetter/feuchte.html
            double a = currentTemperature >= 0 ? 7.5 : 7.6;
            double b = currentTemperature >= 0 ? 237.3 : 240.7;
            double SDD = 6.1078 * Math.Pow(10, ((a * currentTemperature) / (b + currentTemperature)));
            double DD = currentHumidity / 100.0 * SDD;
            double v = Math.Log10(DD / 6.1078);
            double TD = b * v / (a - v);
            return Math.Round(TD, 2);

            // Unvalidated code from PCsensor:
            //double d = (0.66077 + ((7.5 * currentTemperature) / (237.3 + currentTemperature))) + (Math.Log10(currentHumidity) - 2.0);
            //return Math.Round((double) (((d - 0.66077) * 237.3) / (8.16077 - d)), 2);
        }
    }

    /// <summary>
    /// Gets the current absolute humidity value in g/mÂ³.
    /// </summary>
    /// <remarks>
    /// Call ReadValues() before accessing this value.
    /// </remarks>
    public double AbsoluteHumidity
    {
        get
        {
            if (!SupportsTemperature || !SupportsHumidity)
                throw new NotSupportedException("This operation is not supported with the current device");

            // Source: http://www.wetterochs.de/wetter/feuchte.html
            double a = currentTemperature >= 0 ? 7.5 : 7.6;
            double b = currentTemperature >= 0 ? 237.3 : 240.7;
            double mw = 18.016;
            double R = 8314.3;
            double TK = currentTemperature + 273.15;
            double SDD = 6.1078 * Math.Pow(10, ((a * currentTemperature) / (b + currentTemperature)));
            double DD = currentHumidity / 100.0 * SDD;
            double AF = 1E5 * mw / R * DD / TK;
            return Math.Round(AF, 2);
        }
    }

    /// <summary>
    /// Gets the temperature correction value.
    /// </summary>
    /// <remarks>
    /// Call ReadCalibration() before accessing this value.
    /// </remarks>
    public double TemperatureCorrection
    {
        get
        {
            if (!SupportsCorrection)
                throw new NotSupportedException("This operation is not supported with the current device");

            return temperatureCorrection;
        }
    }

    /// <summary>
    /// Gets the humidity correction value.
    /// </summary>
    /// <remarks>
    /// Call ReadCalibration() before accessing this value.
    /// </remarks>
    public double HumidityCorrection
    {
        get
        {
            if (!SupportsCorrection)
                throw new NotSupportedException("This operation is not supported with the current device");

            return humidityCorrection;
        }
    }

    /// <summary>
    /// Gets the device version.
    /// </summary>
    /// <remarks>
    /// Call ReadVersion() before accessing this value.
    /// </remarks>
    public string DeviceVersion
    {
        get
        {
            return deviceVersion;
        }
    }

    /// <summary>
    /// Gets the current device type.
    /// </summary>
    public TEMPerDeviceType DeviceType
    {
        get
        {
            return deviceType;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current device supports temperature measurement.
    /// </summary>
    public bool SupportsTemperature
    {
        get
        {
            return deviceType == TEMPerDeviceType.TEMPer || deviceType == TEMPerDeviceType.TEMPerHUM;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current device supports humidity measurement.
    /// </summary>
    public bool SupportsHumidity
    {
        get
        {
            return deviceType == TEMPerDeviceType.TEMPerHUM;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current device supports internal value correction.
    /// </summary>
    public bool SupportsCorrection
    {
        get
        {
            return deviceType == TEMPerDeviceType.TEMPer || deviceType == TEMPerDeviceType.TEMPerHUM;
        }
    }
    #endregion Properties

    #region Constructors
    /// <summary>
    /// Initialises a new instance of the TEMPer class.
    /// </summary>
    public TEMPer()
    {
        this.preferredDeviceType = TEMPerDeviceType.None;
        this.preferredDeviceNum = 0;
        OpenDevice(preferredDeviceType, 0);
    }

    /// <summary>
    /// Initialises a new instance of the TEMPer class.
    /// </summary>
    /// <param name="preferredDeviceType">Preferred device type.</param>
    public TEMPer(TEMPerDeviceType preferredDeviceType)
    {
        this.preferredDeviceType = preferredDeviceType;
        this.preferredDeviceNum = 0;
        OpenDevice(preferredDeviceType, 0);
    }

    /// <summary>
    /// Initialises a new instance of the TEMPer class.
    /// </summary>
    /// <param name="preferredDeviceType">Preferred device type.</param>
    /// <param name="deviceNum">Zero-based index of the device.</param>
    public TEMPer(TEMPerDeviceType preferredDeviceType, int deviceNum)
    {
        this.preferredDeviceType = preferredDeviceType;
        this.preferredDeviceNum = deviceNum;
        OpenDevice(preferredDeviceType, deviceNum);
    }
    #endregion Constructors

    /// <summary>
    /// Frees resources.
    /// </summary>
    public void Dispose()
    {
        switch (deviceType)
        {
            case TEMPerDeviceType.TEMPer:
                EntryAPI.EMyCloseDevice();
                break;
            case TEMPerDeviceType.TEMPerHUM:
                if (deviceHandle.ToInt64() != -1)
                {
                    RDing.CloseUSBDevice(deviceHandle);
                }
                break;
        }
        deviceType = TEMPerDeviceType.None;
    }

    /// <summary>
    /// Opens the device.
    /// </summary>
    /// <param name="preferredDeviceType">Preferred device type.</param>
    /// <param name="deviceNum">Zero-based index of the device.</param>
    private void OpenDevice(TEMPerDeviceType preferredDeviceType, int deviceNum)
    {
        //logger.Debug("Entered OpenDevice");
        deviceType = TEMPerDeviceType.None;
        deviceHandle = new IntPtr(-1L);

        switch (preferredDeviceType)
        {
            case TEMPerDeviceType.None:
                if (!OpenTEMPerHUMDevice())
                {
                    OpenTEMPerDevice(deviceNum);
                }
                break;
            case TEMPerDeviceType.TEMPer:
                OpenTEMPerDevice(deviceNum);
                break;
            case TEMPerDeviceType.TEMPerHUM:
                OpenTEMPerHUMDevice();
                break;
        }

        if (deviceType == TEMPerDeviceType.None)
            throw new Exception("No supported TEMPer device found.");

        //logger.Debug("Device type: " + deviceType);

        ReadValues();
    }

    /// <summary>
    /// Opens the TEMPer device.
    /// </summary>
    /// <returns>true if successful, false otherwise.</returns>
    private bool OpenTEMPerDevice(int deviceNum)
    {
        try
        {
            int deviceCount = EntryAPI.EMyDetectDevice(0);
            if (deviceCount > 0 && deviceNum < deviceCount)
            {
                deviceType = TEMPerDeviceType.TEMPer;
                EntryAPI.EMySetCurrentDev(deviceNum);
                Thread.Sleep(50);
                EntryAPI.EMyInitConfig(true);
                ReadCalibration();
                return true;
            }
        }
        catch
        {
            // No error if DLL is missing
        }
        return false;
    }

    /// <summary>
    /// Opens the TEMPerHUM device.
    /// </summary>
    /// <returns>true if successful, false otherwise.</returns>
    private bool OpenTEMPerHUMDevice()
    {
        try
        {
            //Debug.WriteLine("calling RDing.OpenUSBDevice");
            deviceHandle = RDing.OpenUSBDevice(vid, pid);
            //Debug.WriteLine("returned from RDing.OpenUSBDevice");
            if (deviceHandle.ToInt64() != -1)
            {
                Debug.WriteLine("TEMPerHUM device opened.");
                deviceType = TEMPerDeviceType.TEMPerHUM;
                Thread.Sleep(50);
                ReadVersion();
                ReadCalibration();
                return true;
            }
            else
            {
                Debug.WriteLine("RDing.OpenUSBDevice failed.");
            }
        }
        catch
        {
            // No error if DLL is missing
        }
        return false;
    }

    /// <summary>
    /// Reads the stored correction values from the device.
    /// </summary>
    public void ReadCalibration()
    {
        if (!SupportsCorrection)
            throw new NotSupportedException("This operation is not supported with the current device");

        lock (accessLock)
        {
            switch (deviceType)
            {
                case TEMPerDeviceType.TEMPer:
                    byte[] buffer6 = new byte[6];
                    EntryAPI.EMyReadEP(ref buffer6[0], ref buffer6[1], ref buffer6[2], ref buffer6[3], ref buffer6[4], ref buffer6[5]);

                    if (buffer6[2] >= 0 & buffer6[2] <= 100)
                    {
                        double offset = buffer6[4] + (double)buffer6[5] / 10.0;
                        temperatureCorrection = buffer6[2] + (double)buffer6[3] / 10.0 - offset;
                    }
                    else
                    {
                        temperatureCorrection = 0.0;
                    }

                    break;

                case TEMPerDeviceType.TEMPerHUM:
                    byte[] buffer = new byte[9];
                    ulong count = 0L;

                    RDing.WriteUSB(deviceHandle, getCalibrationCmd, getCalibrationCmd.Length, ref count);
                    Thread.Sleep(50);
                    RDing.ReadUSB(deviceHandle, buffer, buffer.Length, ref count);
                    if (buffer[1] == 130)
                    {
                        if (buffer[3] < 128)
                        {
                            temperatureCorrection = (double)(buffer[3] * 0x100 + buffer[4]) / 100.0;
                        }
                        else
                        {
                            temperatureCorrection = (double)-(0x10000 - buffer[3] * 0x100 - buffer[4]) / 100.0;
                        }
                        if (buffer[5] < 128)
                        {
                            humidityCorrection = (double)(buffer[5] * 0x100 + buffer[6]) / 100.0;
                        }
                        else
                        {
                            humidityCorrection = (double)-(0x10000 - buffer[5] * 0x100 - buffer[6]) / 100.0;
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Reads the hardware version from the device.
    /// </summary>
    public void ReadVersion()
    {
        if (deviceType != TEMPerDeviceType.TEMPerHUM)
            throw new NotSupportedException("This operation is not supported with the current device");

        lock (accessLock)
        {
            byte[] buffer = new byte[9];
            ulong count = 0L;

            RDing.WriteUSB(deviceHandle, getVersionCmd, getVersionCmd.Length, ref count);
            Thread.Sleep(50);
            RDing.ReadUSB(deviceHandle, buffer, buffer.Length, ref count);
            deviceVersion = Encoding.ASCII.GetString(buffer, 1, 8);
            Thread.Sleep(50);
            RDing.ReadUSB(deviceHandle, buffer, buffer.Length, ref count);
            string str = Encoding.ASCII.GetString(buffer, 1, 8);
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != deviceVersion[i])
                {
                    deviceVersion += str[i];
                }
                else
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Reads the current sensor values from the device.
    /// </summary>
    public void ReadValues()
    {

        lock (accessLock)
        {
            if (deviceType == TEMPerDeviceType.None)
            {
                // Currently no device opened to read from
                Debug.WriteLine("Currently no device opened to read from");
                OpenDevice(preferredDeviceType, preferredDeviceNum);
            }

            switch (deviceType)
            {
                case TEMPerDeviceType.TEMPer:
                    currentTemperature = EntryAPI.EMyReadTemp(true) + temperatureCorrection;
                    Debug.WriteLine("Read current value " + currentTemperature);

                    if (currentTemperature < -80 || currentTemperature > 160)
                    {
                        // Must be a reading error, don't use that value.
                        currentTemperature = double.NaN;
                    }
                    break;

                case TEMPerDeviceType.TEMPerHUM:
                    bool b = false;
                    byte[] buffer = new byte[9];
                    ulong count = 0L;

                    if (RDing.WriteUSB(deviceHandle, readTemperCmd, readTemperCmd.Length, ref count))
                    {
                        b = RDing.ReadUSB(deviceHandle, buffer, buffer.Length, ref count);
                    }
                    else
                    {
                        OpenDevice(TEMPerDeviceType.TEMPerHUM, 0);

                        RDing.WriteUSB(deviceHandle, readTemperCmd, readTemperCmd.Length, ref count);
                        b = RDing.ReadUSB(deviceHandle, buffer, buffer.Length, ref count);
                        if (!b)
                            Debug.WriteLine("RDing.ReadUSB failed, device still not available.");
                    }

                    if (b && buffer[1] == 0x80)
                    {

                        int RawReading = (buffer[4] & 0xFF) + (buffer[3] << 8);

                        currentTemperature = RawReading * (125.0 / 32000.0);
                        //logger.Debug("Currrent Temp in C " + currentTemperature);

                        if (currentTemperature < -80 || currentTemperature > 160)
                        {
                            // Must be a reading error, don't use that value.
                            // (Never seen it with this device though.)
                            currentTemperature = double.NaN;
                        }

                        double rh = buffer[5] * 0x100 + buffer[6];
                        double rh_lin = -2.8E-6 * rh * rh + 0.0405 * rh - 4.0;
                        double rh_true = (currentTemperature - 25.0) * (0.01 + 8E-5 * rh) + rh_lin + humidityCorrection;
                        if (rh_true < -50 || rh_true > 150)
                        {
                            // Must be a reading error, don't use that value.
                            // (Never seen it with this device though.)
                            currentHumidity = double.NaN;
                        }
                        else
                        {
                            if (rh_true > 100.0)
                            {
                                rh_true = 100.0;
                            }
                            if (rh_true < 0.0)
                            {
                                rh_true = 0.0;
                            }
                            currentHumidity = Math.Round(rh_true, 2);
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Writes new correction values to the device's flash memory.
    /// </summary>
    /// <param name="temp">New temperature correction offset.</param>
    /// <param name="hum">New humidity correction offset.</param>
    /// <returns>true on success, false otherwise.</returns>
    public bool WriteCalibration(double temp, double hum)
    {
        if (!SupportsCorrection)
            throw new NotSupportedException("This operation is not supported with the current device");

        if (temp < -50.0 || temp > 50.0) throw new ArgumentOutOfRangeException("temp", temp, "Temperature correction value must be between -50 and 50.");
        if (hum < -50.0 || hum > 50.0) throw new ArgumentOutOfRangeException("hum", hum, "Humidity correction value must be between -50 and 50.");

        lock (accessLock)
        {
            switch (deviceType)
            {
                case TEMPerDeviceType.TEMPer:
                    byte[] buffer6 = new byte[6];
                    EntryAPI.EMyReadEP(ref buffer6[0], ref buffer6[1], ref buffer6[2], ref buffer6[3], ref buffer6[4], ref buffer6[5]);

                    byte offset = 50;
                    double d1 = temp + offset;
                    byte[] buffer5 = new byte[5];
                    buffer5[0] = buffer6[1];
                    buffer5[1] = (byte)Math.Floor(d1);
                    buffer5[2] = (byte)Math.Floor((d1 - Math.Floor(d1)) * 10.0);
                    buffer5[3] = offset;
                    buffer5[4] = 0;
                    if (EntryAPI.EMyWriteEP(ref buffer5[0], ref buffer5[1], ref buffer5[2], ref buffer5[3], ref buffer5[4]))
                    {
                        // Save new calibration values for this instance
                        temperatureCorrection = temp;

                        // Success
                        return true;
                    }
                    break;

                case TEMPerDeviceType.TEMPerHUM:
                    IntPtr localDeviceHandle = RDing.OpenUSBDevice(vid, pid);
                    if (localDeviceHandle.ToInt64() == -1)
                    {
                        throw new Exception("Device error");
                    }

                    byte[] buffer = new byte[9];
                    ulong count = 0L;

                    double Tx = 0.0;
                    double Tx1 = 0.0;
                    double Tx0 = 0.0;
                    double Hx = 0.0;
                    double Hx1 = 0.0;
                    double Hx0 = 0.0;
                    Tx = temp * 100.0;
                    Hx = hum * 100.0;
                    if (Tx < 0.0)
                    {
                        Tx = 65536.0 + Tx;
                        Tx1 = Math.Floor(Tx / 256.0);
                        Tx0 = Tx - Tx1 * 256.0;
                        writeDataCmd[5] = Convert.ToByte(Tx1);
                        writeDataCmd[6] = Convert.ToByte(Tx0);
                    }
                    else
                    {
                        Tx1 = Math.Floor(Tx / 256.0);
                        Tx0 = Tx - Tx1 * 256.0;
                    }
                    writeDataCmd[5] = Convert.ToByte(Tx1);
                    writeDataCmd[6] = Convert.ToByte(Tx0);
                    if (Hx < 0.0)
                    {
                        Hx = 65536.0 + Hx;
                        Hx1 = Math.Floor(Hx / 256.0);
                        Hx0 = Hx - Hx1 * 256.0;
                    }
                    else
                    {
                        Hx1 = Math.Floor(Hx / 256.0);
                        Hx0 = Hx - Hx1 * 256.0;
                    }
                    writeDataCmd[7] = Convert.ToByte(Hx1);
                    writeDataCmd[8] = Convert.ToByte(Hx0);
                    RDing.WriteUSB(localDeviceHandle, writeDataCmd, writeDataCmd.Length, ref count);
                    Thread.Sleep(50);
                    RDing.ReadUSB(localDeviceHandle, buffer, buffer.Length, ref count);
                    if (buffer[4] == 0x55)
                    {
                        Thread.Sleep(50);
                        RDing.WriteUSB(localDeviceHandle, saveFlashCmd, saveFlashCmd.Length, ref count);
                        Thread.Sleep(50);
                        RDing.ReadUSB(localDeviceHandle, buffer, buffer.Length, ref count);
                        if (buffer[4] == 0x55)
                        {
                            // Save new calibration values for this instance
                            temperatureCorrection = temp;
                            humidityCorrection = hum;

                            // Success
                            return true;
                        }
                    }
                    break;
            }

            // Error
            return false;
        }
    }
}