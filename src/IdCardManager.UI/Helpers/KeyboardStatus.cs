using System;
using System.Runtime.InteropServices;

namespace IdCardManager.UI.Helpers
{
    internal static class KeyboardStatus
    {
        private const int VkNumLock = 0x90;
        private const uint KeyeventfExtendedkey = 0x0001;
        private const uint KeyeventfKeyup = 0x0002;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetKeyboardType(int nTypeFlag);

        public static bool IsKeyboardConnected()
        {
            try
            {
                // 0 = keyboard type；失败时返回 0
                return GetKeyboardType(0) != 0;
            }
            catch
            {
                return true;
            }
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
