using System;
using System.Drawing;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    /// <summary>
    /// 首页：3×2 百分比网格铺满；设备三齐才可点；未解锁时点击先弹密码。
    /// </summary>
    public sealed class HomePage : UIPanel
    {
        public enum HomeAction
        {
            Store,
            Withdraw,
            CardData,
            Trades,
            ResetPin
        }

        private readonly AppComposition _app;
        private readonly Label _sessionHint;
        private readonly UIButton _unlockBtn;
        private readonly FeatureCard[] _cards;
        private bool _deviceReady;

        public event Action<HomeAction> Navigate;

        public HomePage(AppComposition app)
        {
            _app = app;
            Text = string.Empty;
            FillColor = Color.FromArgb(240, 242, 245);
            RectColor = FillColor;
            Padding = new Padding(12);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var titles = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            titles.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            titles.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            titles.Controls.Add(new Label
            {
                Text = "请选择功能",
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold, GraphicsUnit.Point),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            }, 0, 0);

            _sessionHint = new Label
            {
                Text = "请连接读卡器并插入授权卡、用户卡",
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(140, 140, 140),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            titles.Controls.Add(_sessionHint, 0, 1);
            header.Controls.Add(titles, 0, 0);

            _unlockBtn = new UIButton
            {
                Text = "校验密码",
                Size = new Size(100, 32),
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(8, 4, 0, 4)
            };
            _unlockBtn.Click += async (_, __) =>
            {
                if (!_deviceReady || NavigationLock.IsLocked) return;
                if (await SessionGate.EnsureUnlockedAsync(FindForm(), _app))
                {
                    RefreshSessionUi();
                }
            };
            header.Controls.Add(_unlockBtn, 1, 0);

            // 正规 3 列 × 2 行；下排第 3 格留空
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            for (var i = 0; i < 3; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));
            }

            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _cards = new[]
            {
                Place(grid, 0, 0, HomeAction.Store, "充钱", "增加余额", Color.FromArgb(0, 150, 136), true),
                Place(grid, 1, 0, HomeAction.Withdraw, "扣钱", "减少余额", Color.FromArgb(230, 126, 34), true),
                Place(grid, 2, 0, HomeAction.CardData, "卡数据", "查看字段", Color.FromArgb(52, 120, 200), true),
                Place(grid, 0, 1, HomeAction.Trades, "卡流水", "本地记录", Color.FromArgb(120, 90, 180), true),
                Place(grid, 1, 1, HomeAction.ResetPin, "重置密码", "需授权卡", Color.FromArgb(90, 90, 96), false)
            };

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(grid, 0, 1);
            Controls.Add(root);

            _app.Session.Changed += () =>
            {
                if (IsDisposed) return;
                if (InvokeRequired) BeginInvoke(new Action(() => RefreshSessionUi()));
                else RefreshSessionUi();
            };

            RefreshSessionUi();
        }

        private FeatureCard Place(
            TableLayoutPanel grid,
            int col,
            int row,
            HomeAction action,
            string title,
            string subtitle,
            Color accent,
            bool requiresUnlock)
        {
            var card = new FeatureCard(title, subtitle, accent, requiresUnlock)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6)
            };
            card.Click += async (_, __) => await OnCardClickAsync(card, action, requiresUnlock);
            grid.Controls.Add(card, col, row);
            return card;
        }

        private async System.Threading.Tasks.Task OnCardClickAsync(FeatureCard card, HomeAction action, bool requiresUnlock)
        {
            // 设备不齐或业务进行中：禁止任何输入
            if (!_deviceReady || !card.IsInteractive || NavigationLock.IsLocked)
            {
                return;
            }

            if (action == HomeAction.ResetPin)
            {
                await ResetPinFlow.RunAsync(FindForm(), _app);
                return;
            }

            if (requiresUnlock && !_app.Session.IsUnlocked)
            {
                if (!await SessionGate.EnsureUnlockedAsync(FindForm(), _app))
                {
                    return;
                }

                RefreshSessionUi();
            }

            if (NavigationLock.IsLocked) return;
            Navigate?.Invoke(action);
        }

        public void ApplyDeviceState(CardSlotStatus status)
        {
            _deviceReady = DeviceGate.IsReady(status);
            RefreshSessionUi(status);
        }

        private void RefreshSessionUi(CardSlotStatus status = null)
        {
            var unlocked = _app.Session.IsUnlocked;
            var block = status != null
                ? DeviceGate.BlockMessage(status)
                : (_deviceReady ? null : "请连接读卡器并插入授权卡、用户卡");

            if (!_deviceReady)
            {
                _sessionHint.Text = string.IsNullOrEmpty(block)
                    ? "请连接读卡器并插入授权卡、用户卡"
                    : block;
                _sessionHint.ForeColor = Color.FromArgb(180, 90, 0);
            }
            else if (!unlocked)
            {
                _sessionHint.Text = "点击功能卡片将弹出密码校验";
                _sessionHint.ForeColor = Color.FromArgb(180, 90, 0);
            }
            else
            {
                _sessionHint.Text = string.IsNullOrEmpty(_app.Session.OilLimitText)
                    ? "已就绪"
                    : "已就绪 · " + _app.Session.OilLimitText;
                _sessionHint.ForeColor = Color.FromArgb(0, 128, 0);
            }

            _unlockBtn.Visible = !unlocked;
            _unlockBtn.Enabled = _deviceReady && !unlocked;

            foreach (var card in _cards)
            {
                // 设备齐就可点；未解锁时点卡片会弹密码（不灰掉）
                card.SetInteractive(_deviceReady, _deviceReady && !unlocked && card.RequiresUnlock);
            }
        }

        private sealed class FeatureCard : Panel
        {
            private readonly Label _title;
            private readonly Label _sub;
            private readonly Label _lockHint;
            private readonly Color _accent;

            public bool RequiresUnlock { get; }
            public bool IsInteractive { get; private set; }

            public FeatureCard(string title, string subtitle, Color accent, bool requiresUnlock)
            {
                RequiresUnlock = requiresUnlock;
                _accent = accent;
                DoubleBuffered = true;
                BackColor = accent;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(16, 14, 16, 12),
                    BackColor = Color.Transparent
                };
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                _title = new Label
                {
                    Text = title,
                    Font = new Font("Microsoft YaHei UI", 20f, FontStyle.Bold, GraphicsUnit.Point),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft,
                    BackColor = Color.Transparent
                };
                layout.Controls.Add(_title, 0, 0);

                _sub = new Label
                {
                    Text = subtitle ?? string.Empty,
                    Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Regular, GraphicsUnit.Point),
                    ForeColor = Color.FromArgb(235, 235, 235),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.TopLeft,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Margin = new Padding(0, 6, 0, 0)
                };
                layout.Controls.Add(_sub, 0, 1);

                _lockHint = new Label
                {
                    Text = "点击后校验密码",
                    Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
                    ForeColor = Color.FromArgb(255, 240, 200),
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Visible = false,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 4, 0, 0)
                };
                layout.Controls.Add(_lockHint, 0, 2);

                Controls.Add(layout);

                void Bubble(object s, EventArgs e) => OnClick(EventArgs.Empty);
                layout.Click += Bubble;
                foreach (Control c in layout.Controls)
                {
                    c.Click += Bubble;
                }

                SetInteractive(false, false);
            }

            public void SetInteractive(bool interactive, bool showNeedPin)
            {
                IsInteractive = interactive;
                BackColor = interactive ? _accent : Blend(_accent, Color.FromArgb(190, 190, 190), 0.55f);
                _lockHint.Visible = showNeedPin;
                Cursor = interactive ? Cursors.Hand : Cursors.No;
                foreach (Control c in Controls)
                {
                    c.Cursor = Cursor;
                    foreach (Control cc in c.Controls)
                    {
                        cc.Cursor = Cursor;
                    }
                }
            }

            private static Color Blend(Color a, Color b, float t)
            {
                return Color.FromArgb(
                    (int)(a.R + (b.R - a.R) * t),
                    (int)(a.G + (b.G - a.G) * t),
                    (int)(a.B + (b.B - a.B) * t));
            }
        }
    }
}
