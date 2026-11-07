using System;
using System.Drawing;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Controls
{
    public sealed class StatusHeader : UIPanel
    {
        private static readonly Color OkGreen = Color.FromArgb(0, 140, 40);
        private static readonly Color BadRed = Color.FromArgb(200, 40, 40);

        private readonly Label _readerLab;
        private readonly Label _authLab;
        private readonly Label _userLab;
        private readonly Label _kbLab;
        private readonly Label _mouseLab;
        private readonly Label _numLab;
        private readonly UIButton _numOnBtn;
        private readonly UIButton _reconnectBtn;
        private readonly Timer _kbTimer;
        private readonly TableLayoutPanel _root;
        private bool _connected;
        private bool _lastKeyboardConnected;

        public event Action ReconnectRequested;

        public StatusHeader()
        {
            Dock = DockStyle.Top;
            Text = string.Empty;
            Padding = new Padding(12, 8, 12, 8);
            FillColor = Color.FromArgb(248, 249, 251);
            RectColor = Color.FromArgb(230, 232, 236);
            Height = 88;

            _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var row1 = MakeFlow();
            _readerLab = MakeChip();
            _authLab = MakeChip();
            _userLab = MakeChip();
            row1.Controls.Add(_readerLab);
            row1.Controls.Add(_authLab);
            row1.Controls.Add(_userLab);

            _reconnectBtn = new UIButton
            {
                Text = "重新连接读卡器",
                Size = new Size(140, 30),
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(10, 0, 0, 0),
                FillColor = Color.FromArgb(230, 126, 34),
                FillHoverColor = Color.FromArgb(240, 150, 60),
                ForeColor = Color.White,
                Visible = false
            };
            _reconnectBtn.Click += (_, __) => ReconnectRequested?.Invoke();
            row1.Controls.Add(_reconnectBtn);
            _root.Controls.Add(row1, 0, 0);

            var row2 = MakeFlow();
            _kbLab = MakeChip();
            _mouseLab = MakeChip();
            _numLab = MakeChip();
            row2.Controls.Add(_kbLab);
            row2.Controls.Add(_mouseLab);
            row2.Controls.Add(_numLab);

            _numOnBtn = new UIButton
            {
                Text = "开启数字键盘",
                Size = new Size(120, 28),
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
                Visible = false,
                Margin = new Padding(10, 0, 0, 0)
            };
            _numOnBtn.Click += (_, __) =>
            {
                KeyboardStatus.TurnNumLockOn();
                RefreshPeripherals();
            };
            row2.Controls.Add(_numOnBtn);
            _root.Controls.Add(row2, 0, 1);

            Controls.Add(_root);

            _kbTimer = new Timer { Interval = 800 };
            _kbTimer.Tick += (_, __) => RefreshPeripherals();
            _kbTimer.Start();
            Disposed += (_, __) =>
            {
                _kbTimer.Stop();
                _kbTimer.Dispose();
            };

            Apply(new CardSlotStatus { ReaderState = ReaderConnectionState.Disconnected });
            RefreshPeripherals();
        }

        private static FlowLayoutPanel MakeFlow()
        {
            return new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2),
                Padding = new Padding(0)
            };
        }

        private static Label MakeChip()
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(0, 2, 18, 2),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
        }

        public void Apply(CardSlotStatus status)
        {
            if (status == null) return;

            _connected = status.ReaderState == ReaderConnectionState.Connected;
            var authOk = status.AuthCardType == AuthCardType.ValueCard;
            var authPresent = status.AuthCardType != AuthCardType.None;
            var userOk = status.UserCardPresent;

            SetChip(
                _readerLab,
                _connected ? "读卡器状态：【已连接】" : "读卡器状态：【未连接】",
                _connected);

            if (!authPresent)
            {
                SetChip(_authLab, "授权卡状态：请插入授权卡", false);
            }
            else if (authOk)
            {
                SetChip(_authLab, "授权卡状态：已插入【授权卡】", true);
            }
            else
            {
                SetChip(_authLab, "授权卡状态：已插入【" + AuthName(status.AuthCardType) + "】", false);
            }

            if (!userOk)
            {
                SetChip(_userLab, "用户卡状态：请插入用户卡", false);
            }
            else
            {
                var sn = string.IsNullOrEmpty(status.UserCardSerialNo) ? "-" : status.UserCardSerialNo;
                SetChip(_userLab, "用户卡状态：已插入用户卡【" + sn + "】", true);
            }

            _reconnectBtn.Visible = !_connected;
            _reconnectBtn.Enabled = !NavigationLock.IsLocked;
            FitHeight();
        }

        private static void SetChip(Label lab, string text, bool ok)
        {
            lab.Text = text;
            lab.ForeColor = ok ? OkGreen : BadRed;
        }

        private void FitHeight()
        {
            _root.PerformLayout();
            var need = Padding.Vertical + 56;
            foreach (Control c in _root.Controls)
            {
                need += c.PreferredSize.Height + 4;
            }

            need = Math.Max(72, need);
            if (Height != need) Height = need;
        }

        private void RefreshPeripherals()
        {
            if (IsDisposed) return;

            var kb = KeyboardStatus.IsKeyboardConnected();
            var mouse = KeyboardStatus.IsMouseConnected();

            SetChip(_kbLab, kb ? "键盘状态：【已连接】" : "键盘状态：【未连接】", kb);
            SetChip(_mouseLab, mouse ? "鼠标状态：【已连接】" : "鼠标状态：【未连接】", mouse);

            if (!kb)
            {
                _numLab.Text = "数字小键盘状态：【未知】";
                _numLab.ForeColor = Color.FromArgb(140, 140, 140);
                _numOnBtn.Visible = false;
            }
            else
            {
                // 刚插上键盘时重新读一次 NumLock
                if (!_lastKeyboardConnected)
                {
                    // 触发一次状态读取（IsNumLockOn）
                }

                var num = KeyboardStatus.IsNumLockOn();
                SetChip(_numLab, num ? "数字小键盘状态：【已开启】" : "数字小键盘状态：【已关闭】", num);
                _numOnBtn.Visible = !num;
            }

            _lastKeyboardConnected = kb;
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
