using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace IdCardManager.UI.Helpers
{
    internal static class SingleInstance
    {
        private const string MutexName = "Local\\IdCardManager_SingleInstance_Mutex";
        private static Mutex _mutex;

        public static bool TryAcquire()
        {
            _mutex = new Mutex(true, MutexName, out var createdNew);
            if (createdNew)
            {
                return true;
            }

            _mutex.Dispose();
            _mutex = null;
            ActivateExisting();
            return false;
        }

        public static void Release()
        {
            if (_mutex == null)
            {
                return;
            }

            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // ignore
            }

            _mutex.Dispose();
            _mutex = null;
        }

        private static void ActivateExisting()
        {
            var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName))
            {
                try
                {
                    if (process.Id == current.Id)
                    {
                        continue;
                    }

                    var handle = process.MainWindowHandle;
                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    if (IsIconic(handle))
                    {
                        ShowWindow(handle, SwRestore);
                    }

                    ShowWindow(handle, SwShowMaximized);
                    SetForegroundWindow(handle);
                    return;
                }
                catch
                {
                    // ignore and try next
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private const int SwRestore = 9;
        private const int SwShowMaximized = 3;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}
