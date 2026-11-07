using System;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace IdCardManager.UI.Helpers
{
    /// <summary>
    /// 常规尺寸：不再做适老二次放大；缩放交给系统 DPI / PerMonitorV2。
    /// </summary>
    internal static class UiScale
    {
        public static float Factor => 1f;

        public static void Initialize()
        {
        }

        public static int S(int px) => Math.Max(1, px);

        public static Size Size(int w, int h) => new Size(w, h);

        public static Point Point(int x, int y) => new Point(x, y);

        public static Padding Pad(int all) => new Padding(all);

        public static Padding Pad(int l, int t, int r, int b) => new Padding(l, t, r, b);

        public static Font Font(float emSize, FontStyle style = FontStyle.Regular)
        {
            return new Font("Microsoft YaHei UI", emSize, style, GraphicsUnit.Point);
        }

        public static UILabel Title(string text)
        {
            return new UILabel
            {
                Text = text,
                Font = Font(16f, FontStyle.Bold),
                Location = Point(12, 12),
                AutoSize = true
            };
        }

        public static UILabel Label(string text, int x, int y)
        {
            return new UILabel
            {
                Text = text,
                Font = Font(13f),
                Location = Point(x, y),
                AutoSize = true
            };
        }

        public static UITextBox TextBox(int x, int y, int w)
        {
            return new UITextBox
            {
                Location = Point(x, y),
                Size = Size(w, 40),
                Font = Font(14f)
            };
        }

        public static UIButton PrimaryButton(string text, int x, int y, int w = 160)
        {
            return new UIButton
            {
                Text = text,
                Location = Point(x, y),
                Size = Size(w, 44),
                Font = Font(13f, FontStyle.Bold)
            };
        }
    }
}
