using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Dialogs;

namespace IdCardManager.UI.Helpers
{
    internal static class ResetPinFlow
    {
        public static async Task RunAsync(IWin32Window owner, AppComposition app)
        {
            var block = DeviceGate.BlockMessage(app.StatusHub.Current);
            if (!string.IsNullOrEmpty(block))
            {
                return;
            }

            using (NavigationLock.Scope())
            {
                if (!Confirm(owner, "确定要重置用户卡密码吗？（1/3）"))
                {
                    return;
                }

                if (!Confirm(owner, "请再次确认：将覆盖卡上原密码。（2/3）"))
                {
                    return;
                }

                if (!Confirm(owner, "最后确认：重置后请用新密码刷卡。（3/3）"))
                {
                    return;
                }

                if (!PinInputDialog.TryAsk(owner, "设置新密码", "请输入新密码", out var newPin))
                {
                    return;
                }

                if (!PinInputDialog.TryAsk(owner, "确认新密码", "请再输入一次", out var confirm) ||
                    confirm != newPin)
                {
                    MessageBox.Show(owner, "两次密码不一致", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var settings = app.Settings;
                var result = await UiBusy.RunAsync(
                    owner,
                    app.StatusHub,
                    "正在重置密码，请勿拔卡",
                    () => app.Card.ResetPin(newPin, settings));

                if (result.Success)
                {
                    app.Session.Lock();
                    MessageBox.Show(owner, "密码已重置", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var msg = string.IsNullOrEmpty(result.ErrorMessage) ? "重置失败" : result.ErrorMessage;
                    MessageBox.Show(owner, msg, "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static bool Confirm(IWin32Window owner, string text)
        {
            return MessageBox.Show(owner, text, "重置密码", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                   DialogResult.Yes;
        }
    }
}
