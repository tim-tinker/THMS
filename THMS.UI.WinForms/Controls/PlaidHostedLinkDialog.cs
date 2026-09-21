using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using THMS.External;
using THMS.External.Plaid;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidHostedLinkDialog : Form
    {
        private readonly PlaidAccountOrchestrator _orchestrator;
        private readonly string _hostedLinkUrl;
        private readonly string _linkToken;
        private readonly WebView2 _webView = new();
        private readonly Label _status = new();
        private readonly CancellationTokenSource _lifetime = new();
        private bool _userCancelled;
        private bool _completing;

        public string? PublicToken { get; private set; }

        public PlaidHostedLinkDialog(PlaidLinkTokenResult session, PlaidAccountOrchestrator orchestrator)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentException.ThrowIfNullOrWhiteSpace(session.HostedLinkUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(session.LinkToken);
            _hostedLinkUrl = session.HostedLinkUrl;
            _linkToken = session.LinkToken;
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            InitializeComponent();
            Load += async (_, _) => await InitializeWebViewAsync();
            FormClosing += OnFormClosing;
            FormClosed += (_, _) =>
            {
                _lifetime.Cancel();
                _lifetime.Dispose();
            };
        }

        private void InitializeComponent()
        {
            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = "Opening Plaid Link...";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            var btnCancel = new ThmsButton { Text = "Cancel" };
            btnCancel.Click += (_, _) =>
            {
                _userCancelled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            };

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 12, 12, 16),
                WrapContents = false
            };
            buttons.Controls.Add(btnCancel);

            _webView.Dock = DockStyle.Fill;
            _webView.TabIndex = 0;

            CancelButton = btnCancel;
            Controls.Add(_webView);
            Controls.Add(_status);
            Controls.Add(buttons);
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(920, 720);
            MinimumSize = new Size(640, 480);
            StartPosition = FormStartPosition.CenterParent;
            Text = "Connect with Plaid Link";
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                var userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "THMS",
                    "WebView2");
                Directory.CreateDirectory(userData);
                var environment = await CoreWebView2Environment.CreateAsync(null, userData);
                await _webView.EnsureCoreWebView2Async(environment);
                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                _webView.CoreWebView2.Navigate(_hostedLinkUrl);
                _status.Text = "Complete Plaid Link in the window below.";
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show(this,
                    "Microsoft Edge WebView2 Runtime is not installed. Install it, then try Connect with Plaid Link again.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AbortLink();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Plaid Link failed to open.\n{ex.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AbortLink();
            }
        }

        private void AbortLink()
        {
            _userCancelled = true;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)
                || !PlaidLinkManager.IsHostedLinkCompletion(uri))
            {
                return;
            }

            e.Cancel = true;
            if (_completing)
                return;

            _completing = true;
            _ = CompleteFromRedirectAsync();
        }

        private async Task CompleteFromRedirectAsync()
        {
            _status.Text = "Finishing Plaid Link...";
            try
            {
                PublicToken = await _orchestrator.WaitForPublicTokenAsync(
                    _linkToken,
                    cancellationToken: _lifetime.Token);
                DialogResult = string.IsNullOrWhiteSpace(PublicToken)
                    ? DialogResult.Cancel
                    : DialogResult.OK;
            }
            catch (OperationCanceledException)
            {
                DialogResult = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Plaid Link failed.\n{ex.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
            }
            finally
            {
                if (!IsDisposed)
                    Close();
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_userCancelled || PublicToken is not null || _completing)
                return;

            e.Cancel = true;
            _completing = true;
            _ = FinishFromCloseAsync();
        }

        private async Task FinishFromCloseAsync()
        {
            try
            {
                _status.Text = "Checking Plaid Link result...";
                PublicToken = await _orchestrator.GetPublicTokenFromLinkSessionAsync(_linkToken);
                DialogResult = string.IsNullOrWhiteSpace(PublicToken)
                    ? DialogResult.Cancel
                    : DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Plaid Link failed.\n{ex.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
            }
            finally
            {
                if (!IsDisposed)
                    Close();
            }
        }
    }
}
