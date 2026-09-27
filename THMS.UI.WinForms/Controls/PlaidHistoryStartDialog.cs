namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidHistoryStartDialog : Form
    {
        private readonly DateTimePicker _date = new();

        public DateTime HistoryStart { get; private set; }

        public PlaidHistoryStartDialog(string? institutionName = null)
        {
            Text = "Import Plaid history";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 168);

            var label = new Label
            {
                AutoSize = false,
                Left = 16,
                Top = 16,
                Width = 428,
                Height = 48,
                Text = string.IsNullOrWhiteSpace(institutionName)
                    ? "Import posted Plaid activity on or after:"
                    : $"Import posted activity for {institutionName} on or after:"
            };
            _date.Format = DateTimePickerFormat.Short;
            _date.Left = 16;
            _date.Top = 72;
            _date.Width = 200;
            _date.Value = DateTime.Today.AddMonths(-3);

            var ok = new ThmsButton { Text = "Import", Left = 240, Top = 116 };
            ok.Click += (_, _) =>
            {
                HistoryStart = _date.Value.Date;
                DialogResult = DialogResult.OK;
                Close();
            };
            var cancel = new ThmsButton { Text = "Later", Left = 348, Top = 116, DialogResult = DialogResult.Cancel };

            Controls.Add(label);
            Controls.Add(_date);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
