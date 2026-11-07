using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace IdCardManager.UI.Dialogs
{
    /// <summary>小巧密码框：固定像素尺寸 + Pixel 字体，避免 150% DPI 叠乘撑爆。</summary>
    internal sealed class PinInputDialog : Form
    {
        private readonly UITextBox _pin;
        private readonly Label _hint;

        public string Pin => _pin.Text ?? string.Empty;

        public PinInputDialog(string title, string message)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(360, 168);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);

            var msg = new Label
            {
                Text = message,
                Location = new Point(16, 12),
                Size = new Size(328, 22),
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Regular, GraphicsUnit.Pixel),
                BackColor = Color.Transparent
            };
            Controls.Add(msg);

            _pin = new UITextBox
            {
                Location = new Point(16, 40),
                Size = new Size(328, 32),
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel),
                PasswordChar = '*',
                Watermark = "密码"
            };
            Controls.Add(_pin);

            _hint = new Label
            {
                Location = new Point(16, 78),
                Size = new Size(328, 18),
                ForeColor = Color.FromArgb(200, 80, 0),
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel),
                BackColor = Color.Transparent
            };
            Controls.Add(_hint);

            var ok = new UIButton
            {
                Text = "确定",
                Location = new Point(96, 108),
                Size = new Size(88, 32),
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel)
            };
            ok.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(_pin.Text))
                {
                    _hint.Text = "请输入密码";
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            var cancel = new UIButton
            {
                Text = "取消",
                Location = new Point(196, 108),
                Size = new Size(88, 32),
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Regular, GraphicsUnit.Pixel)
            };
            cancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
            Shown += (_, __) => _pin.Focus();
        }

        public static bool TryAsk(IWin32Window owner, string title, string message, out string pin)
        {
            using (var dlg = new PinInputDialog(title, message))
            {
                var result = dlg.ShowDialog(owner);
                pin = dlg.Pin;
                return result == DialogResult.OK && !string.IsNullOrWhiteSpace(pin);
            }
        }
    }
}
