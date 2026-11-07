using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.Application.Services;
using Sunny.UI;

namespace IdCardManager.UI.Helpers
{
    internal static class UiBusy
    {
        public static async Task<T> RunAsync<T>(
            IWin32Window owner,
            ReaderStatusHub hub,
            string message,
            Func<T> work)
        {
            hub.Pause();
            NavigationLock.Enter();
            UIWaitForm wait = null;
            try
            {
                wait = new UIWaitForm(string.IsNullOrWhiteSpace(message) ? "请稍候" : message);
                wait.Show(owner);
                System.Windows.Forms.Application.DoEvents();

                return await Task.Run(work).ConfigureAwait(true);
            }
            finally
            {
                try
                {
                    if (wait != null && !wait.IsDisposed)
                    {
                        wait.Close();
                        wait.Dispose();
                    }
                }
                catch
                {
                    // ignore
                }

                NavigationLock.Exit();
                hub.Resume();
            }
        }
    }
}
