using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using IdCardManager.Domain;
using IdCardManager.Domain.Entities;
using IdCardManager.Domain.Enums;
using IdCardManager.UI.Composition;
using IdCardManager.UI.Controls;
using IdCardManager.UI.Helpers;
using IdCardManager.UI.Pages;
using Sunny.UI;

namespace IdCardManager.UI.Forms
{
    public sealed class MainForm : UIForm
    {
        private readonly AppComposition _app;
        private readonly StatusHeader _status;
        private readonly Panel _toolbar;
        private readonly UIButton _back;
        private readonly Label _pageTitle;
        private readonly Panel _host;
        private readonly HomePage _homePage;
        private readonly StoreValuePage _storePage;
        private readonly WithdrawValuePage _withdrawPage;
        private readonly CardDataPage _cardDataPage;
        private readonly TradeListPage _tradePage;
        private bool _closing;

        public MainForm(AppComposition app)
        {
            _app = app;
            Text = StationDefaults.AppTitle;
            TryApplyIcon();
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1024, 700);
            ShowDragStretch = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Font = UiScale.Font(12f);

            _status = new StatusHeader();
            _status.ReconnectRequested += async () => await ReconnectReaderAsync();
            _toolbar = BuildToolbar(out _back, out _pageTitle);
            _host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            _homePage = Prepare(new HomePage(app), keepFill: true);
            _storePage = Prepare(new StoreValuePage(app));
            _withdrawPage = Prepare(new WithdrawValuePage(app));
            _cardDataPage = Prepare(new CardDataPage(app));
            _tradePage = Prepare(new TradeListPage(app));
            // SunnyUI 会把 UIPanel.Text 画到内容区，统一清空避免飘字
            ClearPageCaption(_homePage, _storePage, _withdrawPage, _cardDataPage, _tradePage);

            _homePage.Navigate += OnHomeNavigate;
            _back.Click += (_, __) =>
            {
                if (NavigationLock.IsLocked) return;
                ShowHome();
            };
            NavigationLock.Changed += () =>
            {
                if (IsDisposed) return;
                void Apply() => ApplyInteractionLock();
                if (InvokeRequired) BeginInvoke(new Action(Apply));
                else Apply();
            };

            Controls.Add(_host);
            Controls.Add(_toolbar);
            Controls.Add(_status);

            ShowHome();

            _app.StatusHub.StatusChanged += OnStatus;
            Load += async (_, __) =>
            {
                WindowState = FormWindowState.Maximized;
                await UiBusy.RunAsync(this, _app.StatusHub, "正在连接读卡器", () =>
                {
                    _app.Card.Open();
                    return true;
                });
                _app.StatusHub.Start();
            };
            FormClosing += OnFormClosing;
        }

        /// <summary>
        /// 忙等窗是非模态的，必须主动禁用内容区，否则弹框期间仍能点首页卡片。
        /// </summary>
        private void ApplyInteractionLock()
        {
            var locked = NavigationLock.IsLocked;
            _host.Enabled = !locked;
            _toolbar.Enabled = !locked;
            _back.Enabled = !locked;
            _status.Enabled = !locked;
            UseWaitCursor = locked;
        }

        private async System.Threading.Tasks.Task ReconnectReaderAsync()
        {
            if (NavigationLock.IsLocked) return;

            var ok = await UiBusy.RunAsync(this, _app.StatusHub, "正在重新连接读卡器", () =>
                _app.StatusHub.ReconnectReader());

            if (!ok)
            {
                MessageBox.Show(
                    this,
                    "读卡器仍未连上。请确认读卡器已插好电源/USB，再点一次「重新连接读卡器」。",
                    "连接失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void TryApplyIcon()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "app.ico");
                if (File.Exists(path))
                {
                    Icon = new Icon(path);
                }
            }
            catch
            {
                // ignore
            }
        }

        private Panel BuildToolbar(out UIButton back, out Label title)
        {
            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(12, 6, 12, 6),
                ColumnCount = 2,
                RowCount = 1
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            back = FormLayout.BackButton();
            back.Anchor = AnchorStyles.Left;
            back.Visible = false;
            bar.Controls.Add(back, 0, 0);

            title = new Label
            {
                Text = string.Empty,
                Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold, GraphicsUnit.Point),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 0, 0, 0)
            };
            bar.Controls.Add(title, 1, 0);
            return bar;
        }

        private static T Prepare<T>(T page, bool keepFill = false) where T : UIPanel
        {
            page.Dock = DockStyle.Fill;
            page.Text = string.Empty;
            if (!keepFill)
            {
                page.FillColor = Color.White;
                page.RectColor = Color.White;
            }

            page.Radius = 0;
            return page;
        }

        private static void ClearPageCaption(params UIPanel[] pages)
        {
            foreach (var p in pages)
            {
                if (p != null) p.Text = string.Empty;
            }
        }

        private void OnHomeNavigate(HomePage.HomeAction action)
        {
            switch (action)
            {
                case HomePage.HomeAction.Store:
                    ShowPage(_storePage, "充钱");
                    break;
                case HomePage.HomeAction.Withdraw:
                    ShowPage(_withdrawPage, "扣钱");
                    break;
                case HomePage.HomeAction.CardData:
                    ShowPage(_cardDataPage, "卡数据");
                    break;
                case HomePage.HomeAction.Trades:
                    _tradePage.Reload();
                    ShowPage(_tradePage, "卡流水");
                    break;
            }
        }

        private void ShowHome()
        {
            ShowPage(_homePage, "首页");
        }

        private void ShowPage(Control page, string title)
        {
            var isHome = ReferenceEquals(page, _homePage);
            _host.SuspendLayout();
            _host.Controls.Clear();
            page.Dock = DockStyle.Fill;
            _host.Controls.Add(page);
            _host.ResumeLayout();
            _pageTitle.Text = isHome ? string.Empty : title;
            _toolbar.Visible = !isHome;
            _toolbar.Height = isHome ? 0 : 52;
            _back.Visible = !isHome;
            // 任务栏/标题栏区分当前业务，避免充钱/扣钱切错窗
            Text = isHome
                ? StationDefaults.AppTitle
                : StationDefaults.AppTitle + " · " + title;
        }

        private void OnStatus(CardSlotStatus status)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnStatus(status)));
                return;
            }

            _app.Session.SyncDevice(status);
            _status.Apply(status);

            var connected = status.ReaderState == ReaderConnectionState.Connected;
            _homePage.ApplyDeviceState(status);
            _storePage.ApplyDeviceState(connected, status);
            _withdrawPage.ApplyDeviceState(connected, status);
            _cardDataPage.ApplyDeviceState(connected, status);
        }

        private async void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_closing)
            {
                return;
            }

            // 业务进行中禁止关窗，避免拔卡/中断读写
            if (NavigationLock.IsLocked)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true;
            _closing = true;
            try
            {
                await UiBusy.RunAsync(this, _app.StatusHub, "正在关闭读卡器", () =>
                {
                    _app.StatusHub.Stop();
                    (_app.Card as IDisposable)?.Dispose();
                    return true;
                });
            }
            catch
            {
                try { _app.StatusHub.Stop(); } catch { /* ignore */ }
                try { (_app.Card as IDisposable)?.Dispose(); } catch { /* ignore */ }
            }

            FormClosing -= OnFormClosing;
            Close();
        }
    }
}
