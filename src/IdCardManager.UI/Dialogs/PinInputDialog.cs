using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace IdCardManager.UI.Dialogs
{
    /// <summary>密码框：加高输入框与按钮，大字易读。</summary>
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
            ClientSize = new Size(480, 280);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel);

            Controls.Add(new Label
            {
                Text = message,
                Location = new Point(24, 18),
                Size = new Size(432, 32),
                Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Regular, GraphicsUnit.Pixel),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });

            _pin = new UITextBox
            {
                Location = new Point(24, 60),
                Size = new Size(432, 56),
                Font = new Font("Microsoft YaHei UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel),
                PasswordChar = '*',
                Watermark = "请输入密码"
            };
            Controls.Add(_pin);

            _hint = new Label
            {
                Location = new Point(24, 128),
                Size = new Size(432, 28),
                ForeColor = Color.FromArgb(200, 80, 0),
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel),
                BackColor = Color.Transparent
            };
            Controls.Add(_hint);

            var ok = new UIButton
            {
                Text = "确定",
                Location = new Point(100, 180),
                Size = new Size(130, 56),
                Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel)
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
                Location = new Point(250, 180),
                Size = new Size(130, 56),
                Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Regular, GraphicsUnit.Pixel)
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
