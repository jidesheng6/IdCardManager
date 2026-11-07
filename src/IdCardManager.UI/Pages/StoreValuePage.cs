using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.Domain.Helpers;
using IdCardManager.Domain.Rules;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    public sealed class StoreValuePage : UIPanel
    {
        private readonly AppComposition _app;
        private readonly UITextBox _amount;
        private readonly Label _balance;
        private readonly Label _hint;
        private readonly UIButton _submit;
        private readonly UIButton _queryBalance;
        private readonly FlowLayoutPanel _quickRow;
        private bool _canSubmit;
        private bool _busy;

        public StoreValuePage(AppComposition app)
        {
            _app = app;
            Text = string.Empty;
            FillColor = Color.White;
            RectColor = Color.White;

            var form = FormLayout.CreateMoneyHost(this, "＋", "充钱 · 增加卡内余额", Color.FromArgb(0, 150, 136));
            _amount = FormLayout.AddAmountField(form, "请输入充值金额", 260);
            VisibleChanged += (_, __) =>
            {
                if (Visible && _amount.Enabled) BeginInvoke(new Action(() => _amount.Focus()));
            };
            _balance = FormLayout.AddBalanceRow(form, "当前余额", "-");
            _quickRow = FormLayout.AddQuickAmounts(form, AmountLimits.StoreQuickAmounts, v =>
            {
                if (!_amount.Enabled) return;
                _amount.Text = v.ToString("0.##");
                _amount.Focus();
            });
            _hint = FormLayout.AddHint(form);
            _hint.Text = "单次最多 " + AmountLimits.MaxSingleText + " 元";
            _hint.ForeColor = Color.FromArgb(90, 90, 90);

            var buttons = FormLayout.AddButtonRow(form);
            _queryBalance = FormLayout.BigButton("查询余额", 140);
            _queryBalance.Click += async (_, __) => await QueryBalanceAsync();
            buttons.Controls.Add(_queryBalance);

            _submit = FormLayout.BigButton("确认充钱", 160);
            _submit.FillColor = Color.FromArgb(0, 150, 136);
            _submit.FillHoverColor = Color.FromArgb(0, 170, 150);
            _submit.Click += async (_, __) => await SubmitAsync();
            buttons.Controls.Add(_submit);
        }

        public void ApplyDeviceState(bool connected, CardSlotStatus status)
        {
            if (_busy) return;

            if (status?.BalanceFen != null) SetBalance(status.BalanceFen);
            else if (status != null && !status.UserCardPresent) _balance.Text = "-";

            if (!DeviceGate.IsReady(status))
            {
                SetHint(DeviceGate.BlockMessage(status) ?? "设备未就绪", false);
                DisableAllInput();
                return;
            }

            var block = AuthCardRules.GetBlockMessage(TradeType.Store, status?.AuthCardType ?? AuthCardType.None);
            if (!string.IsNullOrEmpty(block))
            {
                SetHint(block, false);
                DisableAllInput();
                return;
            }

            if (!_app.Session.IsUnlocked)
            {
                SetHint("请校验密码", false);
                DisableAllInput();
                return;
            }

            SetHint("单次最多 " + AmountLimits.MaxSingleText + " 元", true);
            _hint.ForeColor = Color.FromArgb(90, 90, 90);
            SetInputEnabled(true);
            _canSubmit = true;
            _submit.Enabled = true;
            _queryBalance.Enabled = true;
        }

        private void DisableAllInput()
        {
            _canSubmit = false;
            SetInputEnabled(false);
            _submit.Enabled = false;
            _queryBalance.Enabled = false;
        }

        private void SetInputEnabled(bool enabled)
        {
            _amount.Enabled = enabled;
            if (_quickRow == null) return;
            foreach (Control c in _quickRow.Controls)
            {
                c.Enabled = enabled;
            }
        }

        private async Task QueryBalanceAsync()
        {
            if (NavigationLock.IsLocked || _busy) return;
            await SessionGate.RefreshBalanceOilAsync(FindForm(), _app);
            if (_app.Session.IsUnlocked && _app.Session.BalanceFen.HasValue)
            {
                SetBalance(_app.Session.BalanceFen);
                SetHint("已更新", true);
            }
        }

        private async Task SubmitAsync()
        {
            if (NavigationLock.IsLocked || _busy) return;

            if (!AmountHelper.TryParseYuan(_amount.Text, out var yuan))
            {
                SetHint("请输入金额", false);
                return;
            }

            var err = AmountHelper.ValidateSingleAmount(yuan);
            if (!string.IsNullOrEmpty(err))
            {
                SetHint(err, false);
                return;
            }

            if (_app.Session.BalanceFen.HasValue)
            {
                var after = AmountHelper.FenToYuan(_app.Session.BalanceFen.Value) + yuan;
                if (after > AmountLimits.MaxBalanceYuan)
                {
                    SetHint("充后余额不能超过 " + AmountLimits.MaxBalanceYuan.ToString("0.00") + " 元", false);
                    return;
                }
            }

            if (!await SessionGate.EnsureUnlockedAsync(FindForm(), _app))
            {
                return;
            }

            using (NavigationLock.Scope())
            {
                var ask = "确认给用户卡充钱 " + yuan.ToString("0.00") + " 元？";
                if (MessageBox.Show(FindForm(), ask, "确认充钱", MessageBoxButtons.YesNo, MessageBoxIcon.Question) !=
                    DialogResult.Yes)
                {
                    return;
                }

                var fen = AmountHelper.YuanToFen(yuan);
                var pin = _app.Session.Pin;
                await RunBusyAsync("正在充钱，请勿拔卡", () => _app.StoreValue.Execute(fen, pin), result =>
                {
                    if (result.Success)
                    {
                        SetHint("充钱成功", true);
                        if (result.BalanceAfterFen.HasValue)
                        {
                            SetBalance(result.BalanceAfterFen);
                            _app.Session.UpdateBalance(result.BalanceAfterFen);
                            _app.StatusHub.PublishSession(result.BalanceAfterFen, _app.Session.OilLimitText);
                        }
                    }
                    else
                    {
                        SetHint(string.IsNullOrEmpty(result.ErrorMessage) ? "充钱失败" : result.ErrorMessage, false);
                    }
                });
            }
        }

        private async Task RunBusyAsync<T>(string message, System.Func<T> work, System.Action<T> onOk)
        {
            _busy = true;
            _submit.Enabled = false;
            _queryBalance.Enabled = false;
            try
            {
                var result = await UiBusy.RunAsync(FindForm(), _app.StatusHub, message, work);
                onOk(result);
            }
            finally
            {
                _busy = false;
                _submit.Enabled = _canSubmit;
                _queryBalance.Enabled = _canSubmit;
                SetInputEnabled(_canSubmit);
            }
        }

        private void SetBalance(uint? fen)
        {
            _balance.Text = fen.HasValue
                ? AmountHelper.FenToYuan(fen.Value).ToString("0.00") + "元"
                : "-";
        }

        private void SetHint(string text, bool ok)
        {
            _hint.ForeColor = ok ? Color.FromArgb(0, 128, 0) : Color.FromArgb(200, 80, 0);
            _hint.Text = text ?? string.Empty;
        }
    }
}
