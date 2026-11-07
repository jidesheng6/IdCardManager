using System;
using System.Drawing;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Helpers;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    /// <summary>
    /// 首页：3×2 网格；第 6 格为余额/油品信息卡（只读）。
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
        private readonly InfoCard _infoCard;
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

            _infoCard = new InfoCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6)
            };
            grid.Controls.Add(_infoCard, 2, 1);

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
                _sessionHint.Text = "已就绪";
                _sessionHint.ForeColor = Color.FromArgb(0, 128, 0);
            }

            _unlockBtn.Visible = !unlocked;
            _unlockBtn.Enabled = _deviceReady && !unlocked;

            foreach (var card in _cards)
            {
                card.SetInteractive(_deviceReady, _deviceReady && !unlocked && card.RequiresUnlock);
            }

            // 信息卡：优先会话余额，其次状态推送
            uint? bal = _app.Session.BalanceFen;
            var oil = _app.Session.OilLimitText;
            if (status != null)
            {
                if (status.BalanceFen.HasValue) bal = status.BalanceFen;
                if (!string.IsNullOrWhiteSpace(status.OilLimitText)) oil = status.OilLimitText;
                if (!status.UserCardPresent)
                {
                    bal = null;
                    oil = null;
                }
            }

            _infoCard.UpdateInfo(bal, oil);
        }

        private sealed class InfoCard : Panel
        {
            private readonly Label _bal;
            private readonly Label _oil;

            public InfoCard()
            {
                DoubleBuffered = true;
                BackColor = Color.FromArgb(20, 55, 95);
                Padding = new Padding(18);

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.Transparent,
                    Padding = new Padding(8)
                };
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

                _bal = new Label
                {
                    Text = "余额：-",
                    Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Bold, GraphicsUnit.Point),
                    ForeColor = Color.FromArgb(255, 220, 80),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    AutoEllipsis = true
                };
                _oil = new Label
                {
                    Text = "限加油品：-",
                    Font = new Font("Microsoft YaHei UI", 16f, FontStyle.Bold, GraphicsUnit.Point),
                    ForeColor = Color.White,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    AutoEllipsis = true
                };
                layout.Controls.Add(_bal, 0, 0);
                layout.Controls.Add(_oil, 0, 1);
                Controls.Add(layout);
                Cursor = Cursors.Default;
            }

            public void UpdateInfo(uint? balanceFen, string oilLimitText)
            {
                _bal.Text = !balanceFen.HasValue
                    ? "余额：-"
                    : "余额：" + AmountHelper.FenToYuan(balanceFen.Value).ToString("0.00") + "元";

                if (string.IsNullOrWhiteSpace(oilLimitText))
                {
                    _oil.Text = "限加油品：-";
                }
                else
                {
                    _oil.Text = "限加油品：" + oilLimitText.Replace("#", "号");
                }
            }
        }

        /// <summary>
        /// 自绘卡片：整卡可点，文字居中且不被 TableLayout 裁切。
        /// </summary>
        private sealed class FeatureCard : Panel
        {
            private readonly string _titleText;
            private readonly string _subText;
            private readonly Color _accent;
            private bool _showNeedPin;
            private Font _titleFont;
            private Font _subFont;
            private Font _hintFont;

            public bool RequiresUnlock { get; }
            public bool IsInteractive { get; private set; }

            public FeatureCard(string title, string subtitle, Color accent, bool requiresUnlock)
            {
                RequiresUnlock = requiresUnlock;
                _titleText = title ?? string.Empty;
                _subText = subtitle ?? string.Empty;
                _accent = accent;
                _titleFont = new Font("Microsoft YaHei UI", 26f, FontStyle.Bold, GraphicsUnit.Point);
                _subFont = new Font("Microsoft YaHei UI", 13f, FontStyle.Regular, GraphicsUnit.Point);
                _hintFont = new Font("Microsoft YaHei UI", 11f, FontStyle.Regular, GraphicsUnit.Point);

                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
                UpdateStyles();
                BackColor = accent;
                Margin = new Padding(6);
                SetInteractive(false, false);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                FitTitleFont();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(BackColor);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var pad = 16;
                var area = Rectangle.Inflate(ClientRectangle, -pad, -pad);
                var titleSize = TextRenderer.MeasureText(g, _titleText, _titleFont, area.Size, TextFormatFlags.NoPadding);
                var subSize = TextRenderer.MeasureText(g, _subText, _subFont, area.Size, TextFormatFlags.NoPadding);
                var hintSize = _showNeedPin
                    ? TextRenderer.MeasureText(g, "点击后校验密码", _hintFont, area.Size, TextFormatFlags.NoPadding)
                    : Size.Empty;

                var gap = 8;
                var blockH = titleSize.Height + gap + subSize.Height
                             + (_showNeedPin ? gap + hintSize.Height : 0);
                var top = area.Top + Math.Max(0, (area.Height - blockH) / 2);

                var titleRect = new Rectangle(area.Left, top, area.Width, titleSize.Height);
                TextRenderer.DrawText(g, _titleText, _titleFont, titleRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                var subRect = new Rectangle(area.Left, titleRect.Bottom + gap, area.Width, subSize.Height);
                TextRenderer.DrawText(g, _subText, _subFont, subRect, Color.FromArgb(235, 235, 235),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                if (_showNeedPin)
                {
                    var hintRect = new Rectangle(area.Left, subRect.Bottom + gap, area.Width, hintSize.Height);
                    TextRenderer.DrawText(g, "点击后校验密码", _hintFont, hintRect, Color.FromArgb(255, 240, 200),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button == MouseButtons.Left)
                {
                    OnClick(EventArgs.Empty);
                }
            }

            private void FitTitleFont()
            {
                var maxW = Math.Max(40, ClientSize.Width - 32);
                var maxH = Math.Max(28, ClientSize.Height / 3);
                for (var size = 26f; size >= 16f; size -= 1f)
                {
                    using (var probe = new Font("Microsoft YaHei UI", size, FontStyle.Bold, GraphicsUnit.Point))
                    {
                        var sz = TextRenderer.MeasureText(_titleText, probe);
                        if (sz.Width <= maxW && sz.Height <= maxH)
                        {
                            if (Math.Abs(_titleFont.Size - size) > 0.1f)
                            {
                                _titleFont.Dispose();
                                _titleFont = new Font("Microsoft YaHei UI", size, FontStyle.Bold, GraphicsUnit.Point);
                            }

                            return;
                        }
                    }
                }
            }

            public void SetInteractive(bool interactive, bool showNeedPin)
            {
                IsInteractive = interactive;
                _showNeedPin = showNeedPin;
                BackColor = interactive ? _accent : Blend(_accent, Color.FromArgb(190, 190, 190), 0.55f);
                Cursor = interactive ? Cursors.Hand : Cursors.No;
                Invalidate();
            }

            private static Color Blend(Color a, Color b, float t)
            {
                return Color.FromArgb(
                    (int)(a.R + (b.R - a.R) * t),
                    (int)(a.G + (b.G - a.G) * t),
                    (int)(a.B + (b.B - a.B) * t));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _titleFont?.Dispose();
                    _subFont?.Dispose();
                    _hintFont?.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
