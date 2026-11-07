using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace IdCardManager.UI.Dialogs
{
    /// <summary>充/扣确认：金额大字 + 加高按钮。</summary>
    internal sealed class AmountConfirmDialog : Form
    {
        public AmountConfirmDialog(string title, string actionText, string amountYuanText, Color accent)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(500, 340);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Regular, GraphicsUnit.Pixel);

            Controls.Add(new Label
            {
                Text = actionText,
                Location = new Point(24, 24),
                Size = new Size(452, 36),
                Font = new Font("Microsoft YaHei UI", 20f, FontStyle.Regular, GraphicsUnit.Pixel),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            Controls.Add(new Label
            {
                Text = amountYuanText,
                Location = new Point(24, 70),
                Size = new Size(452, 90),
                Font = new Font("Microsoft YaHei UI", 44f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = accent,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            Controls.Add(new Label
            {
                Text = "请确认金额无误后再继续",
                Location = new Point(24, 170),
                Size = new Size(452, 30),
                Font = new Font("Microsoft YaHei UI", 16f, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            var ok = new UIButton
            {
                Text = "确定",
                Location = new Point(90, 230),
                Size = new Size(140, 56),
                Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel),
                FillColor = accent,
                FillHoverColor = accent,
                ForeColor = Color.White
            };
            ok.Click += (_, __) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            var cancel = new UIButton
            {
                Text = "取消",
                Location = new Point(270, 230),
                Size = new Size(140, 56),
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
        }

        public static bool Confirm(IWin32Window owner, string title, string actionText, decimal yuan, Color accent)
        {
            using (var dlg = new AmountConfirmDialog(title, actionText, yuan.ToString("0.00") + " 元", accent))
            {
                return dlg.ShowDialog(owner) == DialogResult.OK;
            }
        }
    }
}
