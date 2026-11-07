using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Dialogs;

namespace IdCardManager.UI.Helpers
{
    internal static class SessionGate
    {
        public static async Task<bool> EnsureUnlockedAsync(IWin32Window owner, AppComposition app)
        {
            if (app.Session.IsUnlocked)
            {
                return true;
            }

            var block = DeviceGate.BlockMessage(app.StatusHub.Current);
            if (!string.IsNullOrEmpty(block))
            {
                return false;
            }

            // 整段验密流程锁住主界面，避免弹窗期间点首页
            using (NavigationLock.Scope())
            {
                if (!PinInputDialog.TryAsk(owner, "校验密码", "请输入用户卡密码", out var pin))
                {
                    return false;
                }

                var result = await UiBusy.RunAsync(owner, app.StatusHub, "正在校验密码", () => app.Card.Unlock(pin));
                if (!result.Success)
                {
                    MessageBox.Show(
                        owner,
                        string.IsNullOrEmpty(result.ErrorMessage) ? "密码错误" : result.ErrorMessage,
                        "校验失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                app.Session.Unlock(pin, result.CardSerialNo, result.BalanceFen, result.OilLimitText, result.OilLimitCodes);
                app.StatusHub.PublishSession(result.BalanceFen, result.OilLimitText);
                return true;
            }
        }

        public static async Task RefreshBalanceOilAsync(IWin32Window owner, AppComposition app)
        {
            if (!DeviceGate.IsReady(app))
            {
                return;
            }

            if (!app.Session.IsUnlocked)
            {
                await EnsureUnlockedAsync(owner, app);
                return;
            }

            using (NavigationLock.Scope())
            {
                var pin = app.Session.Pin;
                var result = await UiBusy.RunAsync(owner, app.StatusHub, "正在查询", () => app.Card.Unlock(pin));
                if (!result.Success)
                {
                    MessageBox.Show(
                        owner,
                        string.IsNullOrEmpty(result.ErrorMessage) ? "查询失败" : result.ErrorMessage,
                        "查询失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    app.Session.Lock();
                    return;
                }

                app.Session.Unlock(pin, result.CardSerialNo, result.BalanceFen, result.OilLimitText, result.OilLimitCodes);
                app.StatusHub.PublishSession(result.BalanceFen, result.OilLimitText);
            }
        }
    }
}
