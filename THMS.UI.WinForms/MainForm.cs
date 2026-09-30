using THMS.Logic.Orchestrators.Finance;
using THMS.UI.WinForms;
using THMS.UI.WinForms.Controls;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private const int NavButtonHeight = 40;
        private static readonly Color NavSelectedBack = Color.FromArgb(0, 99, 177);
        private static readonly Color NavSelectedFore = Color.White;
        private static readonly Color NavIdleBack = Color.FromArgb(245, 245, 245);
        private static readonly Color NavIdleFore = Color.FromArgb(32, 32, 32);

        private readonly Dictionary<string, BaseDashboardForm> _dashboards = [];
        private readonly Dictionary<string, BaseEmbeddedForm> _embeddedForms = [];
        private readonly PlaidTransactionOrchestrator _plaidSync = new();
        private CancellationTokenSource? _plaidSyncCts;

        /// <summary>Designer only.</summary>
        public MainForm()
        {
            InitializeComponent();
            AppStatus.Bind(SetAppStatus);
        }

        public void LoadModules()
        {
            AddSectionLabel("Dashboards");
            AddDashboard("Finance", new FinanceDashboardForm());
            AddDashboard("Vehicles", new VehicleListDashboardForm());
            AddDashboard("Energy", new EnergyDashboardForm());

            AddSectionLabel("Data Management");
            AddEmbeddedForm("Register", new RegisterForm());
            AddEmbeddedForm("Data Manager", new DataManagerForm());
            AddEmbeddedForm("Energy", new DataCenterForm());
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ShowDashboard("Finance");
        }

        private async void OnShown(object? sender, EventArgs e)
        {
            Shown -= OnShown;
            await RunStartupPlaidSyncAsync();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _plaidSyncCts?.Cancel();
        }

        private async Task RunStartupPlaidSyncAsync()
        {
            if (!_plaidSync.HasLinkedItems())
            {
                SetAppStatus("Ready.");
                return;
            }

            if (!_plaidSync.HasItemsReadyForIncrementalSync())
            {
                SetAppStatus("Plaid is linked. Import history from Link Accounts before auto-sync can run.");
                return;
            }

            _plaidSyncCts = new CancellationTokenSource();
            SetAppStatus("Syncing Plaid...", busy: true);
            try
            {
                var progress = new Progress<PlaidSyncProgress>(AppStatus.Report);
                var result = await _plaidSync.SyncIncrementalAsync(_plaidSyncCts.Token, progress);
                SetAppStatus(result.Summary);
                RefreshAfterPlaidSync();
            }
            catch (OperationCanceledException)
            {
                SetAppStatus("Plaid sync cancelled.");
            }
            catch (Exception ex)
            {
                SetAppStatus("Plaid sync failed.");
                MessageBox.Show(this, $"Plaid sync failed.\n{ex.Message}", "Plaid",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshAfterPlaidSync()
        {
            if (_embeddedForms.TryGetValue("Register", out var register) && register is RegisterForm form)
                form.RefreshAfterExternalData();
            if (_dashboards.TryGetValue("Finance", out var dashboard))
                dashboard.RefreshDashboard();
        }

        private void SetAppStatus(string message, bool busy = false, int? completed = null, int? total = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => SetAppStatus(message, busy, completed, total));
                return;
            }

            lblPlaidStatus.Text = string.IsNullOrWhiteSpace(message) ? "Ready." : message;
            if (!busy)
            {
                // Clear the stored cursor before UseWaitCursor. Turning the flag off
                // broadcasts the current cursor, and a stored WaitCursor sticks on the
                // control under the mouse (the unreconciled grid after accept-as-new).
                Cursor = Cursors.Default;
                UseWaitCursor = false;
                ClearStuckWaitCursor(this);
                Cursor.Current = Cursors.Default;

                syncProgress.Visible = false;
                if (syncProgress.Style != ProgressBarStyle.Marquee)
                {
                    syncProgress.Value = 0;
                    syncProgress.Style = ProgressBarStyle.Marquee;
                }

                return;
            }

            if (total is int max && max > 0)
            {
                if (syncProgress.Style != ProgressBarStyle.Continuous)
                    syncProgress.Style = ProgressBarStyle.Continuous;
                syncProgress.Maximum = Math.Max(1, max);
                syncProgress.Value = Math.Clamp(completed ?? 0, 0, syncProgress.Maximum);
            }
            else if (syncProgress.Style != ProgressBarStyle.Marquee)
            {
                syncProgress.Value = 0;
                syncProgress.Style = ProgressBarStyle.Marquee;
            }

            UseWaitCursor = true;
            syncProgress.Visible = true;
            lblPlaidStatus.Update();
            syncProgress.Update();
            Application.DoEvents();
        }

        private static void ClearStuckWaitCursor(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child.UseWaitCursor)
                    child.UseWaitCursor = false;
                if (child.Cursor == Cursors.WaitCursor)
                    child.Cursor = Cursors.Default;
                ClearStuckWaitCursor(child);
            }
        }

        private void OnStatusStripPaint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(200, 200, 200));
            e.Graphics.DrawLine(pen, 0, 0, statusStrip.Width, 0);
        }

        private void AddSectionLabel(string text)
        {
            var isFirst = navigationPanel.Controls.Count == 0;
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, isFirst ? 0 : 16, 0, 8),
            };
            navigationPanel.Controls.Add(label);
        }

        private void AddDashboard(string label, BaseDashboardForm dashboardForm)
        {
            _dashboards[label] = dashboardForm;
            dashboardForm.ConfigureAsEmbeddedDashboard();
            dashboardForm.InitializeDashboard();

            var button = CreateNavButton(label);
            button.Click += OnClickNavigateToDashboard;
            ConfigureChildForm(dashboardForm);
        }

        private void AddEmbeddedForm(string label, BaseEmbeddedForm embeddedForm)
        {
            _embeddedForms[label] = embeddedForm;
            embeddedForm.ConfigureAsEmbeddedForm();

            var button = CreateNavButton(label);
            button.Click += OnClickNavigateToEmbedded;
            ConfigureChildForm(embeddedForm);
        }

        private ThmsButton CreateNavButton(string label)
        {
            var button = new ThmsButton
            {
                Text = label,
                AutoSize = false,
                Height = NavButtonHeight,
                Width = GetNavButtonWidth(),
                Margin = new Padding(0, 0, 0, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                BackColor = NavIdleBack,
                ForeColor = NavIdleFore
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            button.FlatAppearance.BorderSize = 1;
            navigationPanel.Controls.Add(button);
            return button;
        }

        private int GetNavButtonWidth()
        {
            return Math.Max(50, navigationPanel.ClientSize.Width - navigationPanel.Padding.Horizontal);
        }

        private void OnNavigationPanelResize(object? sender, EventArgs e)
        {
            var width = GetNavButtonWidth();
            foreach (Control control in navigationPanel.Controls)
            {
                if (control is Button)
                {
                    control.Width = width;
                }
            }
        }

        private void ConfigureChildForm(Form form)
        {
            form.Visible = false;
            dashboardHostPanel.Controls.Add(form);
        }

        private void OnClickNavigateToDashboard(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            ShowDashboard(button.Text, button);
        }

        private void OnClickNavigateToEmbedded(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            ShowFormInMainPanel(button.Text, button);
        }

        private void ShowDashboard(string moduleName, Button? navButton = null)
        {
            dashboardHostPanel.SuspendLayout();
            try
            {
                HideAllEmbeddedForms();

                var dashboard = _dashboards[moduleName];
                dashboard.Visible = true;
                dashboard.RefreshDashboard();
                HighlightNavButton(navButton ?? FindNavButton(moduleName));
            }
            finally
            {
                dashboardHostPanel.ResumeLayout(true);
            }
        }

        public void ShowRegisterAccount(Guid accountId)
        {
            ShowFormInMainPanel("Register");
            if (_embeddedForms.TryGetValue("Register", out var embedded) && embedded is RegisterForm register)
                register.ShowAccount(accountId);
        }

        private void ShowFormInMainPanel(string formName, Button? navButton = null)
        {
            dashboardHostPanel.SuspendLayout();
            try
            {
                HideAllEmbeddedForms();

                var dashboard = _embeddedForms[formName];
                dashboard.Visible = true;
                HighlightNavButton(navButton ?? FindNavButton(formName));
            }
            finally
            {
                dashboardHostPanel.ResumeLayout(true);
            }
        }

        private Button? FindNavButton(string label)
        {
            foreach (Control control in navigationPanel.Controls)
            {
                if (control is Button button && string.Equals(button.Text, label, StringComparison.Ordinal))
                    return button;
            }

            return null;
        }

        private void HighlightNavButton(Button? selected)
        {
            foreach (Control control in navigationPanel.Controls)
            {
                if (control is not Button button)
                    continue;

                var isSelected = selected is not null && ReferenceEquals(button, selected);
                button.BackColor = isSelected ? NavSelectedBack : NavIdleBack;
                button.ForeColor = isSelected ? NavSelectedFore : NavIdleFore;
                button.FlatAppearance.BorderColor = isSelected
                    ? NavSelectedBack
                    : Color.FromArgb(200, 200, 200);
            }
        }

        private void HideAllEmbeddedForms()
        {
            foreach (var form in _dashboards.Values)
            {
                form.Visible = false;
            }

            foreach (var form in _embeddedForms.Values)
            {
                form.Visible = false;
            }
        }
    }
}
