using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Forms;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            if (!SingleInstance.TryAcquire())
            {
                return;
            }

            UIWaitForm boot = null;
            try
            {
                System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                System.Windows.Forms.Application.ThreadException += (_, e) => LogFatal(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (_, e) => LogFatal(e.ExceptionObject as Exception);

                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                UiScale.Initialize();

                boot = new UIWaitForm("正在启动，请稍候…");
                boot.Show();
                System.Windows.Forms.Application.DoEvents();

                AppComposition app = null;
                var task = Task.Run(() =>
                {
                    app = new AppComposition();
                    return true;
                });

                while (!task.IsCompleted)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(30);
                }

                if (task.IsFaulted)
                {
                    throw task.Exception?.GetBaseException() ?? new Exception("启动失败");
                }

                boot.Close();
                boot.Dispose();
                boot = null;

                System.Windows.Forms.Application.Run(new MainForm(app));
            }
            catch (Exception ex)
            {
                LogFatal(ex);
                MessageBox.Show(ex.ToString(), "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (boot != null && !boot.IsDisposed)
                    {
                        boot.Close();
                        boot.Dispose();
                    }
                }
                catch
                {
                    // ignore
                }

                SingleInstance.Release();
            }
        }

        private static void LogFatal(Exception ex)
        {
            if (ex == null) return;
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IdCardManager");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "crash.log"), DateTime.Now.ToString("s") + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // ignore
            }
        }
    }
}
