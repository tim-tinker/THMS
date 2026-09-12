namespace THMS.UI.WinForms.Controls
{
    public class ExtraPrincipalDialog : Form
    {
        private readonly NumericUpDown _amount = new();

        public decimal Amount { get; private set; }

        public ExtraPrincipalDialog()
        {
            Text = "Extra Principal";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(320, 120);
            ShowInTaskbar = false;

            _amount.DecimalPlaces = 2;
            _amount.Maximum = 1_000_000;
            _amount.Minimum = 0.01m;
            _amount.Value = 100;
            _amount.Dock = DockStyle.Fill;

            var ok = new Button { Text = "Generate", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            ok.Click += (_, _) => Amount = _amount.Value;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.Controls.Add(new Label { Text = "Amount", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            body.Controls.Add(_amount, 1, 0);
            Controls.Add(body);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
