using System.Drawing;
using System.Windows.Forms;
using IdCardManager.Domain.Enums;
using IdCardManager.Domain.Helpers;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    public sealed class TradeListPage : UIPanel
    {
        private readonly AppComposition _app;
        private readonly UIDataGridView _grid;
        private readonly UIButton _refresh;
        private readonly Label _empty;

        public TradeListPage(AppComposition app)
        {
            _app = app;
            Text = "流水";
            FillColor = Color.White;
            RectColor = Color.White;

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(16, 10, 16, 8)
            };

            _refresh = FormLayout.BigButton("刷新", 120);
            _refresh.Location = new Point(16, 8);
            _refresh.Click += (_, __) => Reload();
            top.Controls.Add(_refresh);

            _empty = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(140, 140, 140),
                Text = "暂无流水",
                Visible = false,
                BackColor = Color.White
            };

            _grid = new UIDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular, GraphicsUnit.Point),
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = 36 }
            };
            _grid.Columns.Add("Time", "时间");
            _grid.Columns.Add("Type", "类型");
            _grid.Columns.Add("Card", "卡号");
            _grid.Columns.Add("Amount", "金额");
            _grid.Columns.Add("Balance", "余额");
            _grid.Columns.Add("Ok", "结果");
            _grid.Columns.Add("Err", "说明");

            Controls.Add(_grid);
            Controls.Add(_empty);
            Controls.Add(top);
        }

        public void Reload()
        {
            _grid.Rows.Clear();
            var list = _app.Trades.Query();
            foreach (var t in list)
            {
                _grid.Rows.Add(
                    t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    t.TradeType == TradeType.Store ? "充钱" : "扣钱",
                    t.CardSerialNo,
                    AmountHelper.FenToYuan(t.AmountFen).ToString("0.00"),
                    t.BalanceAfterFen.HasValue ? AmountHelper.FenToYuan(t.BalanceAfterFen.Value).ToString("0.00") : "-",
                    t.Success ? "成功" : "失败",
                    t.ErrorMessage ?? string.Empty);
            }

            var empty = _grid.Rows.Count == 0;
            _empty.Visible = empty;
            _grid.Visible = !empty;
        }
    }
}
