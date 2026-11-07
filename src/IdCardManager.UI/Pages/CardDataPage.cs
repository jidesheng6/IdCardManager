using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Helpers;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    public sealed class CardDataPage : UIPanel
    {
        private readonly AppComposition _app;
        private readonly UIButton _read;
        private readonly Label _hint;
        private readonly UIDataGridView _grid;
        private bool _canRead;
        private bool _busy;

        public CardDataPage(AppComposition app)
        {
            _app = app;
            Text = "卡数据";
            FillColor = Color.White;
            RectColor = Color.White;

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Padding = new Padding(16, 12, 16, 8)
            };

            _read = FormLayout.BigButton("读取卡片", 160);
            _read.Location = new Point(16, 12);
            _read.Click += async (_, __) => await ReadAsync();
            top.Controls.Add(_read);

            _hint = new Label
            {
                Location = new Point(196, 22),
                Size = new Size(640, 32),
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(200, 80, 0),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            top.Controls.Add(_hint);

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
                RowTemplate = { Height = 34 }
            };
            _grid.Columns.Add("Group", "分组");
            _grid.Columns.Add("Name", "字段");
            _grid.Columns.Add("Value", "值");

            Controls.Add(_grid);
            Controls.Add(top);
        }

        public void ApplyDeviceState(bool connected, CardSlotStatus status)
        {
            if (_busy) return;

            if (!DeviceGate.IsReady(status))
            {
                _hint.Text = DeviceGate.BlockMessage(status) ?? "设备未就绪";
                _canRead = false;
                _read.Enabled = false;
                return;
            }

            if (!_app.Session.IsUnlocked)
            {
                _hint.Text = "请校验密码";
                _canRead = false;
                _read.Enabled = false;
                return;
            }

            _hint.Text = string.Empty;
            _canRead = true;
            _read.Enabled = true;
        }

        private async Task ReadAsync()
        {
            if (NavigationLock.IsLocked || _busy) return;

            if (!await SessionGate.EnsureUnlockedAsync(FindForm(), _app))
            {
                return;
            }

            var pin = _app.Session.Pin;
            _busy = true;
            _read.Enabled = false;
            _hint.ForeColor = Color.FromArgb(0, 100, 180);
            _hint.Text = "正在读卡";
            try
            {
                var snap = await UiBusy.RunAsync(
                    FindForm(),
                    _app.StatusHub,
                    "正在读取卡片",
                    () => _app.CardData.Read(pin));

                Bind(snap);
                if (snap.BalanceFen.HasValue)
                {
                    _app.Session.UpdateBalance(snap.BalanceFen);
                    _app.StatusHub.PublishSession(snap.BalanceFen, snap.OilTypeCode);
                }

                if (snap.ExtraFields.ContainsKey("错误"))
                {
                    _hint.ForeColor = Color.FromArgb(200, 80, 0);
                    _hint.Text = snap.ExtraFields["错误"];
                }
                else
                {
                    _hint.ForeColor = Color.FromArgb(0, 128, 0);
                    _hint.Text = "读取完成";
                }
            }
            finally
            {
                _busy = false;
                _read.Enabled = _canRead;
            }
        }

        private void Bind(CardSnapshot snap)
        {
            _grid.Rows.Clear();
            void Add(string group, string name, string value)
            {
                _grid.Rows.Add(group, name, value ?? string.Empty);
            }

            Add("概要", "卡类型", snap.DetectedCardType);
            Add("概要", "卡状态", snap.CardStep);
            Add("概要", "余额(元)", snap.BalanceFen.HasValue
                ? AmountHelper.FenToYuan((int)snap.BalanceFen.Value).ToString("0.00")
                : "-");
            Add("概要", "是否灰卡", snap.IsGreyCard);

            Add("公共文件", "发卡方标识", snap.IssuerId);
            Add("公共文件", "应用类型", snap.AppTypeId);
            Add("公共文件", "应用版本", snap.AppVer);
            Add("公共文件", "应用序列号", snap.AppSerialNo);
            Add("公共文件", "启用日期", snap.EnablementDate);
            Add("公共文件", "有效期", snap.ExpirationDate);
            Add("公共文件", "指令集版本", snap.InstructionSetVer);
            Add("公共文件", "FCI", snap.Fci);
            Add("公共文件", "原始HEX", snap.PubAppHex);

            Add("常用文件", "使用默认密码", snap.UseDefaultPassword);
            Add("常用文件", "员工号", snap.EmployeeNo);
            Add("常用文件", "员工密码HEX", snap.EmployeePasswordHex);
            Add("常用文件", "原始HEX", snap.CommonHex);

            Add("私有文件", "卡类型码", snap.CardTypeCode);
            Add("私有文件", "员工ID", snap.EmployeeId);
            Add("私有文件", "持卡人", snap.HolderName);
            Add("私有文件", "证件号", snap.CertificateNo);
            Add("私有文件", "证件类型", snap.CertificateType);
            Add("私有文件", "原始HEX", snap.PrivateHex);

            Add("敏感文件", "限加油品", snap.OilTypeCode);
            Add("敏感文件", "油品HEX", snap.OilTypeHex);
            Add("敏感文件", "区域模式", snap.AreaMode);
            Add("敏感文件", "区域限制", snap.AreaLimit);
            Add("敏感文件", "区域HEX", snap.AreaLimitHex);
            Add("敏感文件", "单次限额", snap.PerTimeAmount);
            Add("敏感文件", "单次限额HEX", snap.PerTimeAmountHex);
            Add("敏感文件", "日次数限制", snap.PerDayTimes);
            Add("敏感文件", "日金额限制", snap.PerDayAmount);
            Add("敏感文件", "日金额HEX", snap.PerDayAmountHex);
            Add("敏感文件", "车牌", snap.PlateNumber);
            Add("敏感文件", "价格索引", snap.PriceIndex);
            Add("敏感文件", "原始HEX", snap.SensitiveHex);

            foreach (var kv in snap.ExtraFields.OrderBy(x => x.Key))
            {
                Add("其它", kv.Key, kv.Value);
            }
        }
    }
}
