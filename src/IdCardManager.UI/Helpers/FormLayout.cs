using System;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace IdCardManager.UI.Helpers
{
    internal static class FormLayout
    {
        public const int LabelColWidth = 140;
        public const int RowHeight = 64;
        public const int BoxHeight = 56;

        /// <summary>
        /// 充/扣页：banner + 单列表单，行高 AutoSize，避免 DPI 下裁切。
        /// </summary>
        public static TableLayoutPanel CreateMoneyHost(Control host, string mark, string title, Color accent)
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var banner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = accent,
                Margin = new Padding(0),
                Padding = new Padding(16, 0, 16, 0)
            };
            banner.Controls.Add(new Label
            {
                Text = mark + "  " + title,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });
            root.Controls.Add(banner, 0, 0);

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20),
                Margin = new Padding(0)
            };

            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            scroll.Controls.Add(form);
            root.Controls.Add(scroll, 0, 1);
            host.Controls.Add(root);
            return form;
        }

        public static TableLayoutPanel CreateFormHost(Control host)
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24),
                AutoScroll = true
            };

            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = Color.White
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            outer.Controls.Add(form);
            host.Controls.Add(outer);
            return form;
        }

        public static Panel AddPageBanner(Control host, string mark, string title, Color accent)
        {
            var banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = accent,
                Padding = new Padding(16, 0, 20, 0)
            };

            banner.Controls.Add(new Label
            {
                Text = mark + "  " + title,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });
            host.Controls.Add(banner);
            banner.BringToFront();
            return banner;
        }

        public static UITextBox AddAmountField(TableLayoutPanel form, string watermark, int boxWidth = 360)
        {
            var row = NextAutoRow(form);
            var line = MakeFieldLine();
            line.Controls.Add(MakeAutoLabel("金额"), 0, 0);

            var box = new UITextBox
            {
                Text = string.Empty,
                Font = new Font("Microsoft YaHei UI", 16f, FontStyle.Bold, GraphicsUnit.Point),
                Size = new Size(boxWidth, BoxHeight),
                MinimumSize = new Size(boxWidth, BoxHeight),
                Margin = new Padding(0, 8, 10, 8),
                Anchor = AnchorStyles.Left,
                FillColor = Color.White,
                ForeColor = Color.Black,
                RectColor = Color.FromArgb(80, 160, 255),
                RectSize = 2,
                Watermark = watermark ?? string.Empty
            };
            line.Controls.Add(box, 1, 0);
            line.Controls.Add(MakeAutoLabel("元"), 2, 0);

            form.Controls.Add(line, 0, row);
            return box;
        }

        public static UITextBox AddField(TableLayoutPanel form, string label, string value, int boxWidth = 360)
        {
            var row = NextAutoRow(form);
            var line = MakeFieldLine();
            line.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 100f);
            line.Controls.Add(MakeAutoLabel(label), 0, 0);

            var box = new UITextBox
            {
                Text = value ?? string.Empty,
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular, GraphicsUnit.Point),
                Size = new Size(boxWidth, BoxHeight),
                Margin = new Padding(0, 4, 0, 4),
                Anchor = AnchorStyles.Left,
                FillColor = Color.White,
                ForeColor = Color.Black,
                RectColor = Color.FromArgb(80, 160, 255),
                RectSize = 2
            };
            line.Controls.Add(box, 1, 0);
            form.Controls.Add(line, 0, row);
            return box;
        }

        public static Label AddBalanceRow(TableLayoutPanel form, string label, string value)
        {
            var row = NextAutoRow(form);
            var line = MakeFieldLine();
            line.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 100f);
            line.Controls.Add(MakeAutoLabel(label), 0, 0);

            var val = new Label
            {
                Text = value,
                Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(0, 100, 180),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            line.Controls.Add(val, 1, 0);
            form.Controls.Add(line, 0, row);
            return val;
        }

        public static Label AddHint(TableLayoutPanel form)
        {
            var row = NextAutoRow(form);
            var hint = new Label
            {
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(200, 80, 0),
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6),
                BackColor = Color.Transparent
            };
            form.Controls.Add(hint, 0, row);
            return hint;
        }

        public static FlowLayoutPanel AddQuickAmounts(TableLayoutPanel form, decimal[] amounts, Action<decimal> onPick)
        {
            var row = NextAutoRow(form);
            var wrap = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 6, 0, 8),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            foreach (var a in amounts)
            {
                var v = a;
                var text = v.ToString("0.##");
                var chip = new UIButton
                {
                    Text = text,
                    Size = new Size(Math.Max(96, 32 + text.Length * 16), 48),
                    Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold, GraphicsUnit.Point),
                    Margin = new Padding(0, 0, 10, 10),
                    FillColor = Color.FromArgb(245, 247, 250),
                    FillHoverColor = Color.FromArgb(220, 230, 245),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    RectColor = Color.FromArgb(180, 190, 200)
                };
                chip.Click += (_, __) => onPick(v);
                wrap.Controls.Add(chip);
            }

            form.Controls.Add(wrap, 0, row);
            return wrap;
        }

        public static FlowLayoutPanel AddButtonRow(TableLayoutPanel form)
        {
            var row = NextAutoRow(form);
            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0),
                BackColor = Color.Transparent
            };
            form.Controls.Add(buttons, 0, row);
            return buttons;
        }

        public static UIButton BigButton(string text, int width = 200)
        {
            return new UIButton
            {
                Text = text,
                Size = new Size(width, 56),
                MinimumSize = new Size(width, 56),
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(0, 0, 14, 0)
            };
        }

        public static UIButton BackButton()
        {
            return new UIButton
            {
                Text = "← 返回",
                Size = new Size(128, 48),
                MinimumSize = new Size(128, 48),
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(0, 0, 12, 0)
            };
        }

        public static Panel CreateScrollHost(Control host)
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24)
            };
            host.Controls.Add(scroll);
            return scroll;
        }

        private static int NextAutoRow(TableLayoutPanel form)
        {
            var row = form.RowCount;
            form.RowCount = row + 1;
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return row;
        }

        private static TableLayoutPanel MakeFieldLine()
        {
            var line = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 2, 0, 2),
                BackColor = Color.Transparent
            };
            // 标签列用 AutoSize，避免「当前余额」被 Absolute 宽度挤成换行
            line.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            line.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return line;
        }

        private static Label MakeAutoLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Regular, GraphicsUnit.Point),
                AutoSize = true,
                MaximumSize = new Size(0, 0),
                AutoEllipsis = false,
                Margin = new Padding(0, 12, 14, 12),
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
        }

        public const int LabelWidth = LabelColWidth;
    }
}
