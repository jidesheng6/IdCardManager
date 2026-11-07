using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Dialogs;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    public sealed class SettingsPage : UIPanel
    {
        private readonly AppComposition _app;
        private readonly UITextBox _terminal;
        private readonly UITextBox _userKey;
        private readonly UITextBox _station;
        private readonly UITextBox _area;
        private readonly UITextBox _operator;
        private readonly UITextBox _lrPath;
        private readonly UICheckBox _fake;
        private readonly Label _hint;
        private readonly UIButton _importBtn;

        public SettingsPage(AppComposition app)
        {
            _app = app;
            Text = "参数";
            FillColor = Color.White;
            RectColor = Color.White;

            var s = app.SettingsService.Get();
            var form = FormLayout.CreateFormHost(this);

            _terminal = FormLayout.AddField(form, "终端号", s.TerminalNo, 360);
            _userKey = FormLayout.AddField(form, "系统密钥", s.UserKey, 480);
            _station = FormLayout.AddField(form, "站点号", s.StationNo, 360);
            _area = FormLayout.AddField(form, "区域号", s.AreaNo, 200);
            _operator = FormLayout.AddField(form, "操作员", s.OperatorName, 360);
            _lrPath = FormLayout.AddField(form, "原系统目录", s.LrCenterPath, 520);

            _fake = new UICheckBox
            {
                Text = "调试：模拟读卡器",
                Checked = s.UseFakeCard,
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular, GraphicsUnit.Point),
                Visible = s.UseFakeCard || System.Diagnostics.Debugger.IsAttached,
                Margin = new Padding(0, 8, 0, 0)
            };
            if (_fake.Visible)
            {
                var row = form.RowCount;
                form.RowCount = row + 1;
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                form.Controls.Add(_fake, 1, row);
            }

            _hint = FormLayout.AddHint(form);

            var buttons = FormLayout.AddButtonRow(form);
            _importBtn = FormLayout.BigButton("从原系统导入", 160);
            _importBtn.Click += (_, __) => Import();
            buttons.Controls.Add(_importBtn);

            var save = FormLayout.BigButton("保存", 120);
            save.Click += (_, __) => Save();
            buttons.Controls.Add(save);

            var resetPin = FormLayout.BigButton("重置用户卡密码", 180);
            resetPin.Click += async (_, __) => await ResetPinAsync();
            buttons.Controls.Add(resetPin);

            if (string.IsNullOrWhiteSpace(s.UserKey))
            {
                ShownOnce();
            }
        }

        private void ShownOnce()
        {
            try
            {
                Import(silentIfFail: true);
            }
            catch
            {
                // ignore
            }
        }

        private void Save()
        {
            var current = _app.SettingsService.Get();
            var s = new AppSettings
            {
                TerminalNo = _terminal.Text.Trim(),
                UserKey = _userKey.Text.Trim(),
                StationNo = _station.Text.Trim(),
                AreaNo = _area.Text.Trim(),
                OperatorName = _operator.Text.Trim(),
                LrCenterPath = _lrPath.Text.Trim(),
                UseFakeCard = _fake.Visible ? _fake.Checked : current.UseFakeCard,
                UserCardNad = current.UserCardNad,
                AuthCardNad = current.AuthCardNad,
                ReaderType = current.ReaderType,
                ReaderPort = current.ReaderPort,
                ReaderPollMs = current.ReaderPollMs
            };
            _app.SettingsService.Save(s);
            _hint.ForeColor = Color.FromArgb(0, 128, 0);
            _hint.Text = (_fake.Visible && _fake.Checked) ? "已保存，模拟读卡器需重启后生效" : "已保存";
        }

        private void Import(bool silentIfFail = false)
        {
            var result = _app.SettingsService.Import(_lrPath.Text.Trim());
            var s = result.Settings ?? _app.SettingsService.Get();
            _terminal.Text = s.TerminalNo ?? string.Empty;
            _userKey.Text = s.UserKey ?? string.Empty;
            _station.Text = s.StationNo ?? string.Empty;
            _area.Text = s.AreaNo ?? string.Empty;
            _operator.Text = s.OperatorName ?? string.Empty;
            _lrPath.Text = s.LrCenterPath ?? string.Empty;
            if (_fake.Visible)
            {
                _fake.Checked = s.UseFakeCard;
            }

            if (silentIfFail && result.Partial && string.IsNullOrWhiteSpace(s.UserKey))
            {
                _hint.ForeColor = Color.FromArgb(200, 80, 0);
                _hint.Text = "请点「从原系统导入」";
                return;
            }

            _hint.ForeColor = result.Partial ? Color.FromArgb(200, 80, 0) : Color.FromArgb(0, 128, 0);
            _hint.Text = string.IsNullOrWhiteSpace(result.Message) ? "导入完成" : ShortImportMessage(result.Message);
        }

        private static string ShortImportMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return "导入完成";
            return message.Length > 48 ? message.Substring(0, 48) + "…" : message;
        }

        private async Task ResetPinAsync()
        {
            if (!PinInputDialog.TryAsk(FindForm(), "重置密码", "请输入新密码", out var newPin))
            {
                return;
            }

            if (!PinInputDialog.TryAsk(FindForm(), "确认密码", "请再输入一次", out var confirm) ||
                confirm != newPin)
            {
                _hint.ForeColor = Color.FromArgb(200, 80, 0);
                _hint.Text = "两次密码不一致";
                return;
            }

            var settings = _app.SettingsService.Get();
            var result = await UiBusy.RunAsync(
                FindForm(),
                _app.StatusHub,
                "正在重置密码，请勿拔卡",
                () => _app.Card.ResetPin(newPin, settings));

            if (result.Success)
            {
                _app.Session.Lock();
                _hint.ForeColor = Color.FromArgb(0, 128, 0);
                _hint.Text = "密码已重置";
            }
            else
            {
                _hint.ForeColor = Color.FromArgb(200, 80, 0);
                var err = result.ErrorMessage ?? string.Empty;
                if (err.Contains("授权")) _hint.Text = "请插入授权卡";
                else if (err.Contains("用户")) _hint.Text = "请插入用户卡";
                else _hint.Text = string.IsNullOrEmpty(err) ? "重置失败" : err;
            }
        }
    }
}
