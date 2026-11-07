using System;
using System.Drawing;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.Domain.Helpers;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Controls
{
    public sealed class StatusHeader : UIPanel
    {
        private readonly Label _l1;
        private readonly Label _l2;
        private readonly Label _l3;
        private readonly UIButton _numOnBtn;
        private readonly UIButton _reconnectBtn;
        private readonly Timer _kbTimer;
        private readonly TableLayoutPanel _root;
        private bool _connected;

        public event Action ReconnectRequested;

        public StatusHeader()
        {
            Dock = DockStyle.Top;
            Text = string.Empty;
            Padding = new Padding(12, 8, 12, 8);
            FillColor = Color.FromArgb(248, 249, 251);
            RectColor = Color.FromArgb(230, 232, 236);
            Height = 96;

            _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var row1 = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            _l1 = MakeLine();
            _l1.AutoSize = true;
            _l1.Dock = DockStyle.None;
            row1.Controls.Add(_l1);

            _reconnectBtn = new UIButton
            {
                Text = "重新连接读卡器",
                Size = new Size(140, 28),
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(12, 0, 0, 0),
                FillColor = Color.FromArgb(230, 126, 34),
                FillHoverColor = Color.FromArgb(240, 150, 60),
                ForeColor = Color.White,
                Visible = false
            };
            _reconnectBtn.Click += (_, __) => ReconnectRequested?.Invoke();
            row1.Controls.Add(_reconnectBtn);
            _root.Controls.Add(row1, 0, 0);

            _l2 = MakeLine();
            _root.Controls.Add(_l2, 0, 1);

            var row3 = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0),
                Padding = new Padding(0)
            };
            _l3 = MakeLine();
            _l3.AutoSize = true;
            _l3.Dock = DockStyle.None;
            row3.Controls.Add(_l3);

            _numOnBtn = new UIButton
            {
                Text = "开启数字键盘",
                Size = new Size(110, 26),
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
                Visible = false,
                Margin = new Padding(12, 0, 0, 0)
            };
            _numOnBtn.Click += (_, __) =>
            {
                KeyboardStatus.TurnNumLockOn();
                RefreshKeyboard();
            };
            row3.Controls.Add(_numOnBtn);
            _root.Controls.Add(row3, 0, 2);

            Controls.Add(_root);

            _kbTimer = new Timer { Interval = 800 };
            _kbTimer.Tick += (_, __) => RefreshKeyboard();
            _kbTimer.Start();
            Disposed += (_, __) =>
            {
                _kbTimer.Stop();
                _kbTimer.Dispose();
            };

            Apply(new CardSlotStatus { ReaderState = ReaderConnectionState.Disconnected });
            RefreshKeyboard();
        }

        private static Label MakeLine()
        {
            return new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
                Margin = new Padding(0, 2, 0, 2),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
        }

        public void Apply(CardSlotStatus status)
        {
            if (status == null) return;

            _connected = status.ReaderState == ReaderConnectionState.Connected;
            var reader = _connected ? "读卡器状态：【已连接】" : "读卡器状态：【未连接】";
            var auth = status.AuthCardType == AuthCardType.None
                ? "授权卡状态：请插入授权卡"
                : "授权卡状态：已插入【" + AuthName(status.AuthCardType) + "】";
            var user = !status.UserCardPresent
                ? "用户卡状态：请插入用户卡"
                : "用户卡状态：已插入用户卡【" +
                  (string.IsNullOrEmpty(status.UserCardSerialNo) ? "-" : status.UserCardSerialNo) + "】";

            _l1.Text = reader + "　　" + auth + "　　" + user;
            _l1.ForeColor = _connected ? Color.Black : Color.FromArgb(180, 40, 40);

            // 未连接时显示大按钮，连上自动隐藏——老人只点这一下
            _reconnectBtn.Visible = !_connected;
            _reconnectBtn.Enabled = !NavigationLock.IsLocked;

            var bal = !status.UserCardPresent || !status.BalanceFen.HasValue
                ? "余额：-"
                : "余额：" + AmountHelper.FenToYuan(status.BalanceFen.Value).ToString("0.00") + "元";
            var oil = !status.UserCardPresent || string.IsNullOrWhiteSpace(status.OilLimitText)
                ? "限加油品：-"
                : "限加油品：" + status.OilLimitText.Replace("#", "号");
            _l2.Text = bal + "　　" + oil;
            _l2.ForeColor = Color.FromArgb(0, 90, 160);

            FitHeight();
        }

        private void FitHeight()
        {
            _root.PerformLayout();
            var content = _l1.PreferredSize.Height + _l2.PreferredSize.Height + _l3.PreferredSize.Height
                          + Padding.Vertical + 20;
            var need = Math.Max(78, content);
            if (Height != need) Height = need;
        }

        private void RefreshKeyboard()
        {
            if (IsDisposed) return;
            var kb = KeyboardStatus.IsKeyboardConnected();
            var num = KeyboardStatus.IsNumLockOn();
            _l3.Text = (kb ? "键盘状态：【已连接】" : "键盘状态：【未连接】")
                       + "　　"
                       + (num ? "数字小键盘状态：【已开启】" : "数字小键盘状态：【已关闭】");
            _l3.ForeColor = (kb && num) ? Color.FromArgb(0, 128, 0) : Color.FromArgb(180, 90, 0);
            _numOnBtn.Visible = !num;
            FitHeight();
        }

        private static string AuthName(AuthCardType type)
        {
            switch (type)
            {
                case AuthCardType.ValueCard: return "授权卡";
                case AuthCardType.MotherCard: return "母卡";
                case AuthCardType.UserMotherCard: return "用户母卡";
                case AuthCardType.LrKeyCard: return "密钥卡";
                case AuthCardType.PsamCard: return "PSAM卡";
                case AuthCardType.OilCard: return "用户卡";
                case AuthCardType.Unknown: return "非充值授权卡";
                default: return "未知卡";
            }
        }
    }
}
