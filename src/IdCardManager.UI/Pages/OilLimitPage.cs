using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.Domain.Helpers;
using IdCardManager.Infrastructure.Card;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Helpers;
using Sunny.UI;

namespace IdCardManager.UI.Pages
{
    /// <summary>
    /// 限加油品：上半区修改当前卡可加油品，下半区查看本地卡流水。
    /// </summary>
    public sealed class OilLimitPage : UIPanel
    {
        private static readonly Color Accent = Color.FromArgb(52, 120, 200);
        private static readonly Color OptionBack = Color.FromArgb(242, 244, 247);
        private static readonly Color OptionBorder = Color.FromArgb(200, 205, 212);

        private readonly AppComposition _app;
        private readonly Dictionary<string, CheckBox> _boxes =
            new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase);

        private Label _current;
        private FlowLayoutPanel _options;
        private CheckBox _unlimited;
        private UIButton _readLimit;
        private UIButton _save;
        private Label _hint;
        private UIDataGridView _grid;
        private Label _empty;
        private UIButton _refreshTrades;

        private bool _busy;
        private bool _canEdit;
        private bool _suppress;

        public OilLimitPage(AppComposition app)
        {
            _app = app;
            Text = string.Empty;
            FillColor = Color.White;
            RectColor = Color.White;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildEditor(), 0, 0);
            root.Controls.Add(BuildTrades(), 0, 1);
            Controls.Add(root);

            LoadFromSession();
        }

        private Control BuildEditor()
        {
            var top = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 6),
                Margin = new Padding(0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            var header = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            header.Controls.Add(new Label
            {
                Text = "当前限加油品：",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(50, 50, 50),
                Margin = new Padding(0, 6, 4, 0),
                BackColor = Color.Transparent
            });
            _current = new Label
            {
                Text = "未读取",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Accent,
                Margin = new Padding(0, 6, 0, 0),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_current);
            layout.Controls.Add(header, 0, 0);

            _options = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 2)
            };

            _unlimited = CreateOptionBox("不限", null);
            _options.Controls.Add(_unlimited);

            // 仅保留 92 号、95 号两个可选油品，单选
            foreach (var code in new[] { "1020", "1040" })
            {
                var box = CreateOptionBox(OilTypeCatalog.GetName(code), code);
                _options.Controls.Add(box);
                _boxes[code] = box;
            }

            layout.Controls.Add(_options, 0, 1);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 0)
            };

            _readLimit = FormLayout.BigButton("读取当前限制", 180);
            _readLimit.Size = new Size(180, 52);
            _readLimit.MinimumSize = new Size(180, 52);
            _readLimit.Click += async (_, __) => await ReadCurrentAsync();
            buttons.Controls.Add(_readLimit);

            _save = FormLayout.BigButton("保存修改", 160);
            _save.Size = new Size(160, 52);
            _save.MinimumSize = new Size(160, 52);
            _save.FillColor = Accent;
            _save.FillHoverColor = Color.FromArgb(70, 140, 220);
            _save.Click += async (_, __) => await SaveAsync();
            buttons.Controls.Add(_save);

            layout.Controls.Add(buttons, 0, 2);

            _hint = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(200, 80, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            layout.Controls.Add(_hint, 0, 3);

            top.Controls.Add(layout);
            return top;
        }

        private Control BuildTrades()
        {
            var bottom = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(16, 6, 16, 4),
                Margin = new Padding(0)
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            bar.Controls.Add(new Label
            {
                Text = "本地流水",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            }, 0, 0);

            _refreshTrades = new UIButton
            {
                Text = "刷新流水",
                Size = new Size(120, 40),
                MinimumSize = new Size(120, 40),
                Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold, GraphicsUnit.Point)
            };
            _refreshTrades.Click += (_, __) => ReloadTrades();
            bar.Controls.Add(_refreshTrades, 1, 0);

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
                RowTemplate = { Height = 36 },
                BackgroundColor = Color.White
            };
            _grid.Columns.Add("Time", "时间");
            _grid.Columns.Add("Type", "类型");
            _grid.Columns.Add("Card", "卡号");
            _grid.Columns.Add("Amount", "金额");
            _grid.Columns.Add("Balance", "余额");
            _grid.Columns.Add("Ok", "结果");
            _grid.Columns.Add("Err", "说明");

            bottom.Controls.Add(_grid);
            bottom.Controls.Add(_empty);
            bottom.Controls.Add(bar);
            return bottom;
        }

        private CheckBox CreateOptionBox(string text, string code)
        {
            var box = new CheckBox
            {
                Text = text,
                Tag = code,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Size = new Size(code == null ? 120 : 168, 52),
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(0, 0, 10, 10),
                UseVisualStyleBackColor = false,
                BackColor = OptionBack,
                ForeColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand
            };
            box.FlatAppearance.BorderSize = 1;
            box.FlatAppearance.BorderColor = OptionBorder;
            box.CheckedChanged += (_, __) => OnOptionChanged(box);
            return box;
        }

        private void OnOptionChanged(CheckBox box)
        {
            if (_suppress) return;

            _suppress = true;
            try
            {
                // 单选：点亮当前，其余全部熄灭
                if (ReferenceEquals(box, _unlimited))
                {
                    if (_unlimited.Checked)
                    {
                        foreach (var b in _boxes.Values) b.Checked = false;
                    }
                }
                else if (box.Checked)
                {
                    _unlimited.Checked = false;
                    foreach (var b in _boxes.Values)
                    {
                        if (!ReferenceEquals(b, box)) b.Checked = false;
                    }
                }
            }
            finally
            {
                _suppress = false;
            }

            UpdateOptionStyles();
        }

        private void UpdateOptionStyles()
        {
            var unlimited = _unlimited.Checked;
            var anySpecific = _boxes.Values.Any(b => b.Checked);

            foreach (var box in _boxes.Values)
            {
                // 选中“不限”后，其余油品不可点
                box.Enabled = _canEdit && !unlimited;
                ApplyOptionStyle(box);
            }

            // 选中任意具体油品后，“不限”不可点
            _unlimited.Enabled = _canEdit && !anySpecific;
            ApplyOptionStyle(_unlimited);
        }

        private static void ApplyOptionStyle(CheckBox box)
        {
            box.BackColor = box.Checked ? Accent : OptionBack;
            box.ForeColor = box.Checked ? Color.White : Color.FromArgb(60, 60, 60);
            box.FlatAppearance.BorderColor = box.Checked ? Accent : OptionBorder;
        }

        private CheckBox GetOrCreateOption(string code)
        {
            if (_boxes.TryGetValue(code, out var box))
            {
                return box;
            }

            box = CreateOptionBox(OilTypeCatalog.GetName(code), code);
            _options.Controls.Add(box);
            _boxes[code] = box;
            return box;
        }

        private void LoadFromSession()
        {
            ApplyCodes(SplitCodes(_app.Session.OilLimitCodes), _app.Session.OilLimitText);
        }

        private void ApplyCodes(IEnumerable<string> codes, string text)
        {
            var list = codes == null
                ? new List<string>()
                : codes.Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // 单选：优先“不限”，否则取第一个具体油品码
            var unlimited = !string.IsNullOrWhiteSpace(text) &&
                            (list.Count == 0 || string.Equals(text, "不限", StringComparison.Ordinal));
            var specificCode = unlimited ? null : (list.Count > 0 ? list[0] : null);

            _suppress = true;
            try
            {
                _unlimited.Checked = unlimited;
                foreach (var box in _boxes.Values) box.Checked = false;
                if (specificCode != null)
                {
                    GetOrCreateOption(specificCode).Checked = true;
                }
            }
            finally
            {
                _suppress = false;
            }

            _current.Text = string.IsNullOrWhiteSpace(text) ? "未读取" : text.Replace("#", "号");
            UpdateOptionStyles();
        }

        private static List<string> SplitCodes(string codes)
        {
            if (string.IsNullOrWhiteSpace(codes)) return new List<string>();
            return codes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();
        }

        public void Reload()
        {
            LoadFromSession();
            ReloadTrades();
            ApplyDeviceState(true, _app.StatusHub.Current);
        }

        public void ApplyDeviceState(bool connected, CardSlotStatus status)
        {
            if (_busy) return;

            var wasAble = _canEdit;
            var canEdit = DeviceGate.IsReady(status) && _app.Session.IsUnlocked;
            _canEdit = canEdit;

            if (!canEdit)
            {
                var block = DeviceGate.BlockMessage(status);
                if (string.IsNullOrEmpty(block))
                {
                    block = _app.Session.IsUnlocked ? "设备未就绪" : "请校验密码";
                }

                SetHint(block, false);
            }
            else if (!wasAble)
            {
                SetHint(string.Empty, true);
            }

            SetEditingEnabled(canEdit);
            UpdateOptionStyles();
        }

        private void SetEditingEnabled(bool enabled)
        {
            _options.Enabled = enabled;
            _readLimit.Enabled = enabled;
            _save.Enabled = enabled;
        }

        private async Task ReadCurrentAsync()
        {
            if (NavigationLock.IsLocked || _busy) return;

            if (!await SessionGate.EnsureUnlockedAsync(FindForm(), _app))
            {
                return;
            }

            _busy = true;
            SetEditingEnabled(false);
            try
            {
                await SessionGate.RefreshBalanceOilAsync(FindForm(), _app);
                if (_app.Session.IsUnlocked)
                {
                    LoadFromSession();
                    SetHint("已读取当前限制", true);
                }
            }
            finally
            {
                _busy = false;
                SetEditingEnabled(_canEdit);
                UpdateOptionStyles();
            }
        }

        private async Task SaveAsync()
        {
            if (NavigationLock.IsLocked || _busy) return;

            var selected = _unlimited.Checked
                ? new List<string>()
                : _boxes.Values
                    .Where(b => b.Checked)
                    .Select(b => b.Tag as string)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (!_unlimited.Checked && selected.Count == 0)
            {
                SetHint("请选择油品，或勾选「不限」", false);
                return;
            }

            if (!await SessionGate.EnsureUnlockedAsync(FindForm(), _app))
            {
                return;
            }

            var names = selected.Count == 0 ? "不限" : string.Join(" / ", selected.Select(OilTypeCatalog.GetName));

            using (NavigationLock.Scope())
            {
                if (MessageBox.Show(
                        FindForm(),
                        "确认把该卡限加油品改为：" + names + "？",
                        "确认修改",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                _busy = true;
                SetEditingEnabled(false);
                try
                {
                    var pin = _app.Session.Pin;
                    var result = await UiBusy.RunAsync(
                        FindForm(),
                        _app.StatusHub,
                        "正在修改限加油品，请勿拔卡",
                        () => _app.OilLimit.Update(selected, pin));

                    if (result.Success)
                    {
                        _app.Session.UpdateOilLimit(result.OilLimitText, result.OilLimitCodes);
                        _app.StatusHub.PublishSession(_app.Session.BalanceFen, result.OilLimitText);
                        ApplyCodes(SplitCodes(result.OilLimitCodes), result.OilLimitText);
                        SetHint("修改成功", true);
                    }
                    else
                    {
                        SetHint(string.IsNullOrEmpty(result.ErrorMessage) ? "修改失败" : result.ErrorMessage, false);
                    }
                }
                finally
                {
                    _busy = false;
                    SetEditingEnabled(_canEdit);
                    UpdateOptionStyles();
                }
            }
        }

        public void ReloadTrades()
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

        private void SetHint(string text, bool ok)
        {
            _hint.ForeColor = ok ? Color.FromArgb(0, 128, 0) : Color.FromArgb(200, 80, 0);
            _hint.Text = text ?? string.Empty;
        }
    }
}
