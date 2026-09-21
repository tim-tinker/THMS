using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidLinkDialog : Form
    {
        private readonly PlaidAccountOrchestrator _orchestrator;
        private RadioButton? _radSandbox;
        private ThmsButton _btnContinue = null!;

        public string? PublicToken { get; private set; }

        public PlaidLinkDialog()
            : this(new PlaidAccountOrchestrator())
        {
        }

        public PlaidLinkDialog(PlaidAccountOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (var i = 0; i < 5; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var help = _orchestrator.IsSandbox
                ? "Sandbox can mint a test institution without opening Plaid Link. "
                    + "Connect with Plaid Link opens Plaid's bank-login page in this app."
                : "Connect with Plaid Link opens Plaid's bank-login page in this app. "
                    + "When you finish, THMS exchanges the result for an access token.";

            var lblHelp = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Text = help
            };

            var row = 1;
            if (_orchestrator.IsSandbox)
            {
                _radSandbox = new RadioButton
                {
                    AutoSize = true,
                    Checked = true,
                    Margin = new Padding(0, 12, 0, 4),
                    Text = "Use sandbox test bank"
                };
                var radConnect = new RadioButton
                {
                    AutoSize = true,
                    Margin = new Padding(0, 4, 0, 8),
                    Text = "Connect with Plaid Link"
                };
                layout.Controls.Add(_radSandbox, 0, row++);
                layout.Controls.Add(radConnect, 0, row++);
            }

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 8, 0, 0),
                Padding = new Padding(0, 8, 0, 16),
                WrapContents = false
            };
            var btnCancel = new ThmsButton
            {
                DialogResult = DialogResult.Cancel,
                Text = "Cancel"
            };
            _btnContinue = new ThmsButton { Text = "Continue" };
            _btnContinue.Click += OnContinue;
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(_btnContinue);

            layout.Controls.Add(lblHelp, 0, 0);
            layout.Controls.Add(buttons, 0, row);

            AcceptButton = _btnContinue;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(480, 220);
            Name = "PlaidLinkDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Link Institution (Plaid Link)";
        }

        private bool UseSandboxShortcut =>
            _orchestrator.IsSandbox && (_radSandbox?.Checked ?? false);

        private async void OnContinue(object? sender, EventArgs e)
        {
            if (UseSandboxShortcut)
            {
                PublicToken = null;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _btnContinue.Enabled = false;
            try
            {
                var session = await _orchestrator.CreateHostedLinkSessionAsync();
                using var hosted = new PlaidHostedLinkDialog(session, _orchestrator);
                if (hosted.ShowDialog(this) != DialogResult.OK
                    || string.IsNullOrWhiteSpace(hosted.PublicToken))
                {
                    _btnContinue.Enabled = true;
                    return;
                }

                PublicToken = hosted.PublicToken;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Plaid Link failed.\n{ex.Message}", "Plaid Link",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _btnContinue.Enabled = true;
            }
        }
    }
}
