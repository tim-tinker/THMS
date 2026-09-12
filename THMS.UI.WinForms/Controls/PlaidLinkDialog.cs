namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidLinkDialog : Form
    {
        private RadioButton radSandbox = null!;
        private RadioButton radPublicToken = null!;
        private TextBox txtPublicToken = null!;

        public string? PublicToken { get; private set; }

        public PlaidLinkDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var lblHelp = new Label
            {
                AutoSize = false,
                Location = new Point(16, 16),
                Size = new Size(440, 64),
                Text = "Plaid Link issues a public token that THMS exchanges for an access token. "
                    + "In sandbox, THMS can create a test institution token. "
                    + "Otherwise paste a public token from Plaid Link."
            };
            radSandbox = new RadioButton
            {
                AutoSize = true,
                Checked = true,
                Location = new Point(16, 88),
                Text = "Use sandbox test bank"
            };
            radPublicToken = new RadioButton
            {
                AutoSize = true,
                Location = new Point(16, 116),
                Text = "Paste public token"
            };
            txtPublicToken = new TextBox
            {
                Location = new Point(16, 144),
                Size = new Size(440, 23)
            };
            var btnContinue = new Button
            {
                Location = new Point(276, 188),
                Size = new Size(90, 32),
                Text = "Continue"
            };
            var btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(376, 188),
                Size = new Size(90, 32),
                Text = "Cancel"
            };
            btnContinue.Click += OnContinue;
            radSandbox.CheckedChanged += (_, _) => txtPublicToken.Enabled = radPublicToken.Checked;
            radPublicToken.CheckedChanged += (_, _) => txtPublicToken.Enabled = radPublicToken.Checked;
            txtPublicToken.Enabled = false;

            AcceptButton = btnContinue;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(472, 236);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PlaidLinkDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Link Institution (Plaid Link)";
            Controls.Add(lblHelp);
            Controls.Add(radSandbox);
            Controls.Add(radPublicToken);
            Controls.Add(txtPublicToken);
            Controls.Add(btnContinue);
            Controls.Add(btnCancel);
        }

        private void OnContinue(object? sender, EventArgs e)
        {
            if (radPublicToken.Checked)
            {
                var token = txtPublicToken.Text.Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show(this, "Paste a Plaid public token.", "Plaid Link",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                PublicToken = token;
            }
            else
            {
                PublicToken = null;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
