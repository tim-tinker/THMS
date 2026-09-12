using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public class ManualReconcileDialog : Form
    {
        private readonly DateTimePicker _date = new();

        public DateTime PostedDate { get; private set; }

        public ManualReconcileDialog(PlannedPaymentView payment)
        {
            Text = "Manual Reconcile Payment";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(420, 160);
            ShowInTaskbar = false;

            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today;
            _date.Dock = DockStyle.Fill;

            var ok = new Button { Text = "Reconcile", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            ok.Click += (_, _) => PostedDate = _date.Value.Date;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(12) };
            body.Controls.Add(new Label
            {
                Text = $"{payment.AccountName}: {payment.PlannedAmount:c2} planned for {payment.PlannedDate:d}",
                Dock = DockStyle.Fill,
                Height = 40
            });
            body.Controls.Add(new Label { Text = "Posted date", AutoSize = true });
            body.Controls.Add(_date);
            Controls.Add(body);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
