namespace THMS.UI.WinForms.Controls
{
    partial class FinanceDiagnosticsControl
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Panel pnlAccounts;
        private Label lblAccounts;
        private Button btnRunAccounts;
        private TextBox txtAccounts;
        private Panel pnlTransactions;
        private Label lblTransactions;
        private Button btnRunTransactions;
        private TextBox txtTransactions;
        private Panel pnlSplits;
        private Label lblSplits;
        private Button btnRunSplits;
        private TextBox txtSplits;
        private Panel pnlLoans;
        private Label lblLoans;
        private Button btnRunLoans;
        private TextBox txtLoans;
        private Panel pnlForecast;
        private Label lblForecast;
        private Button btnRunForecast;
        private TextBox txtForecast;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            pnlAccounts = new Panel();
            lblAccounts = new Label();
            btnRunAccounts = new Button();
            txtAccounts = new TextBox();
            pnlTransactions = new Panel();
            lblTransactions = new Label();
            btnRunTransactions = new Button();
            txtTransactions = new TextBox();
            pnlSplits = new Panel();
            lblSplits = new Label();
            btnRunSplits = new Button();
            txtSplits = new TextBox();
            pnlLoans = new Panel();
            lblLoans = new Label();
            btnRunLoans = new Button();
            txtLoans = new TextBox();
            pnlForecast = new Panel();
            lblForecast = new Label();
            btnRunForecast = new Button();
            txtForecast = new TextBox();
            lblStatus = new Label();
            layout.SuspendLayout();
            pnlAccounts.SuspendLayout();
            pnlTransactions.SuspendLayout();
            pnlSplits.SuspendLayout();
            pnlLoans.SuspendLayout();
            pnlForecast.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(pnlAccounts, 0, 0);
            layout.Controls.Add(pnlTransactions, 0, 1);
            layout.Controls.Add(pnlSplits, 0, 2);
            layout.Controls.Add(pnlLoans, 0, 3);
            layout.Controls.Add(pnlForecast, 0, 4);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            // 
            // groups
            // 
            WireGroup(pnlAccounts, lblAccounts, btnRunAccounts, txtAccounts,
                "Accounts", "Account Diagnostics", "Run Account Diagnostics", OnRunAccountDiagnostics);
            WireGroup(pnlTransactions, lblTransactions, btnRunTransactions, txtTransactions,
                "Transactions", "Transaction Diagnostics", "Run Transaction Diagnostics", OnRunTransactionDiagnostics);
            WireGroup(pnlSplits, lblSplits, btnRunSplits, txtSplits,
                "Splits", "Split Diagnostics", "Run Split Diagnostics", OnRunSplitDiagnostics);
            WireGroup(pnlLoans, lblLoans, btnRunLoans, txtLoans,
                "Loans", "Loan Diagnostics", "Run Loan Diagnostics", OnRunLoanDiagnostics);
            WireGroup(pnlForecast, lblForecast, btnRunForecast, txtForecast,
                "Forecast", "Forecast Diagnostics", "Run Forecast Diagnostics", OnRunForecastDiagnostics);
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(8, 0, 8, 0);
            lblStatus.Text = "Ready.";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // FinanceDiagnosticsControl
            // 
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(layout);
            Controls.Add(lblStatus);
            Name = "FinanceDiagnosticsControl";
            Size = new Size(1000, 720);
            layout.ResumeLayout(false);
            pnlAccounts.ResumeLayout(false);
            pnlAccounts.PerformLayout();
            pnlTransactions.ResumeLayout(false);
            pnlTransactions.PerformLayout();
            pnlSplits.ResumeLayout(false);
            pnlSplits.PerformLayout();
            pnlLoans.ResumeLayout(false);
            pnlLoans.PerformLayout();
            pnlForecast.ResumeLayout(false);
            pnlForecast.PerformLayout();
            ResumeLayout(false);
        }

        private static void WireGroup(
            Panel panel,
            Label label,
            Button button,
            TextBox output,
            string key,
            string header,
            string buttonText,
            EventHandler onClick)
        {
            panel.Controls.Add(output);
            panel.Controls.Add(button);
            panel.Controls.Add(label);
            panel.Dock = DockStyle.Fill;
            panel.Name = "pnl" + key;
            panel.Padding = new Padding(8);

            label.AutoSize = false;
            label.Dock = DockStyle.Top;
            label.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label.Height = 36;
            label.Name = "lbl" + key;
            label.Text = header;
            label.TextAlign = ContentAlignment.MiddleLeft;

            button.Dock = DockStyle.Top;
            button.Height = 32;
            button.Name = "btnRun" + key;
            button.Text = buttonText;
            button.Click += onClick;

            output.Dock = DockStyle.Fill;
            output.Multiline = true;
            output.Name = "txt" + key;
            output.ReadOnly = true;
            output.ScrollBars = ScrollBars.Vertical;
        }
    }
}
