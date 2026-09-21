namespace THMS.UI.WinForms.Controls
{
    public sealed class AcceptBeforeDateDialog : Form
    {
        private readonly DateTimePicker _date = new();
        public DateTime BeforeDate => _date.Value.Date;

        public AcceptBeforeDateDialog(DateTime suggested)
        {
            Text = "Accept Unreconciled Before";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 16, 16, 0),
                RowCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12),
                MaximumSize = new Size(400, 0),
                Text = "Accept all unreconciled imports before this date as new:"
            };

            _date.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _date.Format = DateTimePickerFormat.Short;
            _date.Margin = new Padding(0, 0, 0, 8);
            _date.MinimumSize = new Size(160, 0);
            _date.Value = suggested == default ? DateTime.Today : suggested;

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0),
                Padding = new Padding(0, 12, 0, 16),
                WrapContents = false
            };
            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
            var ok = new ThmsButton { Text = "Accept", DialogResult = DialogResult.OK };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(_date, 0, 1);
            layout.Controls.Add(buttons, 0, 2);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
            MinimumSize = new Size(440, 0);
        }
    }
}
