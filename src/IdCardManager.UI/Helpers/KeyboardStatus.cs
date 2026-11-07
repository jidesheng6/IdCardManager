using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace IdCardManager.UI.Helpers
{
    /// <summary>
    /// 键盘/鼠标在位检测（Raw Input）。
    /// 键盘：排除虚拟设备，以及与鼠标同 VID/PID 的复合键鼠接口。
    /// </summary>
    internal static class KeyboardStatus
    {
        private const int VkNumLock = 0x90;
        private const uint KeyeventfExtendedkey = 0x0001;
        private const uint KeyeventfKeyup = 0x0002;
        private const uint RimTypeMouse = 0;
        private const uint RimTypeKeyboard = 1;
        private const uint RidiDeviceName = 0x20000007;

        private static readonly Regex VidPidRegex = new Regex(
            @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDeviceList
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputDeviceList(
            [Out] RawInputDeviceList[] pRawInputDeviceList,
            ref uint puiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice,
            uint uiCommand,
            StringBuilder pData,
            ref uint pcbSize);

        public static bool IsKeyboardConnected()
        {
            try
            {
                return CountStandaloneKeyboards() > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMouseConnected()
        {
            try
            {
                return CountPhysicalMice() > 0;
            }
            catch
            {
                return false;
            }
        }

        public static int CountStandaloneKeyboards()
        {
            Enumerate(out var mouseVidPid, out var keyboards, out _);
            var standalone = 0;
            foreach (var name in keyboards)
            {
                if (IsStandaloneKeyboard(name, mouseVidPid))
                {
                    standalone++;
                }
            }

            return standalone;
        }

        public static int CountPhysicalMice()
        {
            Enumerate(out _, out _, out var mice);
            var n = 0;
            foreach (var name in mice)
            {
                if (!IsVirtualDevice(name) && IsHidOrUsb(name))
                {
                    n++;
                }
            }

            return n;
        }

        private static void Enumerate(
            out HashSet<string> mouseVidPid,
            out List<string> keyboards,
            out List<string> mice)
        {
            mouseVidPid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            keyboards = new List<string>();
            mice = new List<string>();

            var size = (uint)Marshal.SizeOf(typeof(RawInputDeviceList));
            uint count = 0;
            GetRawInputDeviceList(null, ref count, size);
            if (count == 0)
            {
                return;
            }

            var list = new RawInputDeviceList[count];
            if (GetRawInputDeviceList(list, ref count, size) == unchecked((uint)-1))
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var name = GetDeviceName(list[i].hDevice);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (list[i].dwType == RimTypeMouse)
                {
                    mice.Add(name);
                    var vp = ExtractVidPid(name);
                    if (vp != null)
                    {
                        mouseVidPid.Add(vp);
                    }
                }
                else if (list[i].dwType == RimTypeKeyboard)
                {
                    keyboards.Add(name);
                }
            }
        }

        private static bool IsStandaloneKeyboard(string deviceName, HashSet<string> mouseVidPid)
        {
            if (IsVirtualDevice(deviceName) || !IsHidOrUsb(deviceName))
            {
                return false;
            }

            var vp = ExtractVidPid(deviceName);
            if (vp != null && mouseVidPid.Contains(vp))
            {
                return false;
            }

            return true;
        }

        private static bool IsHidOrUsb(string deviceName)
        {
            var n = deviceName.ToUpperInvariant();
            return n.IndexOf(@"\\?\HID#", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf(@"\\?\USB#", StringComparison.Ordinal) >= 0;
        }

        private static bool IsVirtualDevice(string deviceName)
        {
            var n = deviceName.ToUpperInvariant();
            return n.IndexOf("VIRTUAL", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("RDP", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("TERMINAL", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("ROOT#", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("GVINPUT", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("UMIDUMMY", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("VID_0000", StringComparison.Ordinal) >= 0 ||
                   n.IndexOf("PID_0000", StringComparison.Ordinal) >= 0;
        }

        private static string ExtractVidPid(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                return null;
            }

            var m = VidPidRegex.Match(deviceName);
            if (!m.Success)
            {
                return null;
            }

            return m.Groups[1].Value.ToUpperInvariant() + ":" + m.Groups[2].Value.ToUpperInvariant();
        }

        private static string GetDeviceName(IntPtr device)
        {
            uint len = 0;
            GetRawInputDeviceInfo(device, RidiDeviceName, null, ref len);
            if (len == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder((int)len);
            if (GetRawInputDeviceInfo(device, RidiDeviceName, sb, ref len) == unchecked((uint)-1))
            {
                return string.Empty;
            }

            return sb.ToString();
        }

        public static bool IsNumLockOn()
        {
            return (GetKeyState(VkNumLock) & 1) == 1;
        }

        public static void TurnNumLockOn()
        {
            if (IsNumLockOn())
            {
                return;
            }

            keybd_event(VkNumLock, 0x45, KeyeventfExtendedkey, UIntPtr.Zero);
            keybd_event(VkNumLock, 0x45, KeyeventfExtendedkey | KeyeventfKeyup, UIntPtr.Zero);
        }
    }
}
