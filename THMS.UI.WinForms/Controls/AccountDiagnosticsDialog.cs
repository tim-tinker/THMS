using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class AccountDiagnosticsDialog : Form
    {
        public AccountDiagnosticsDialog()
            : this(new AccountDiagnosticsOrchestrator())
        {
        }

        public AccountDiagnosticsDialog(AccountDiagnosticsOrchestrator orchestrator)
        {
            Text = "Account Diagnostics";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(720, 420);
            MinimumSize = new Size(480, 280);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = "Account Diagnostics",
                TextAlign = ContentAlignment.MiddleLeft
            };

            var findings = orchestrator.Run();
            var text = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = string.Join(Environment.NewLine, findings)
            };

            var btnClose = new ThmsButton { Text = "Close", DialogResult = DialogResult.OK };
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 12, 12, 16)
            };
            buttons.Controls.Add(btnClose);

            Controls.Add(text);
            Controls.Add(heading);
            Controls.Add(buttons);
            AcceptButton = btnClose;
            CancelButton = btnClose;
        }
    }
}
