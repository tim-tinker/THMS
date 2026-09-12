namespace THMS.UI.WinForms.Controls
{
    partial class CashFlowForecastControl
    {
        private System.ComponentModel.IContainer components = null;
        private Panel pnlRoot;
        private Label lblHeader;
        private Label lblCurrent;
        private TextBox txtCurrent;
        private Label lblNextPayday;
        private TextBox txtNextPayday;
        private Label lblTwoPaydays;
        private TextBox txtTwoPaydays;
        private Label lblAfterPlanned;
        private TextBox txtAfterPlanned;
        private Button btnRefresh;
        private Button btnCommit;
        private Button btnReconcile;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlRoot = new Panel();
            lblHeader = new Label();
            lblCurrent = new Label();
            txtCurrent = new TextBox();
            lblNextPayday = new Label();
            txtNextPayday = new TextBox();
            lblTwoPaydays = new Label();
            txtTwoPaydays = new TextBox();
            lblAfterPlanned = new Label();
            txtAfterPlanned = new TextBox();
            btnRefresh = new Button();
            btnCommit = new Button();
            btnReconcile = new Button();
            pnlRoot.SuspendLayout();
            SuspendLayout();
            pnlRoot.Controls.Add(CreateFields());
            pnlRoot.Controls.Add(CreateButtons());
            pnlRoot.Controls.Add(lblHeader);
            pnlRoot.Dock = DockStyle.Fill;
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Padding = new Padding(8);
            lblHeader.AutoSize = false;
            lblHeader.Dock = DockStyle.Top;
            lblHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblHeader.Height = 36;
            lblHeader.Name = "lblHeader";
            lblHeader.Text = "Cash Flow Forecast";
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(pnlRoot);
            Name = "CashFlowForecastControl";
            Size = new Size(1000, 200);
            pnlRoot.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Control CreateFields()
        {
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Name = "fields"
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (var i = 0; i < 4; i++)
                fields.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            WireField(lblCurrent, txtCurrent, "Current Balance");
            WireField(lblNextPayday, txtNextPayday, "Forecast Next Payday");
            WireField(lblTwoPaydays, txtTwoPaydays, "Forecast Two Paydays");
            WireField(lblAfterPlanned, txtAfterPlanned, "Forecast After Planned");
            fields.Controls.Add(lblCurrent, 0, 0);
            fields.Controls.Add(txtCurrent, 1, 0);
            fields.Controls.Add(lblNextPayday, 0, 1);
            fields.Controls.Add(txtNextPayday, 1, 1);
            fields.Controls.Add(lblTwoPaydays, 0, 2);
            fields.Controls.Add(txtTwoPaydays, 1, 2);
            fields.Controls.Add(lblAfterPlanned, 0, 3);
            fields.Controls.Add(txtAfterPlanned, 1, 3);
            return fields;
        }

        private Control CreateButtons()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Name = "buttons"
            };
            btnRefresh.Text = "Refresh Forecast";
            btnRefresh.AutoSize = true;
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Click += OnRefresh;
            btnCommit.Text = "Commit Planned Payments";
            btnCommit.AutoSize = true;
            btnCommit.Name = "btnCommit";
            btnCommit.Click += OnCommit;
            btnReconcile.Text = "Manual Reconcile Payment";
            btnReconcile.AutoSize = true;
            btnReconcile.Name = "btnReconcile";
            btnReconcile.Click += OnReconcile;
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnCommit);
            toolbar.Controls.Add(btnReconcile);
            return toolbar;
        }

        private static void WireField(Label label, TextBox textBox, string caption)
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.Text = caption;
            label.TextAlign = ContentAlignment.MiddleLeft;
            textBox.Dock = DockStyle.Fill;
            textBox.ReadOnly = true;
        }
    }
}
