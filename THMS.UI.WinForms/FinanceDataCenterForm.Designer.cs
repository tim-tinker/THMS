namespace THMS.UI.WinForms
{
    partial class FinanceDataCenterForm
    {
        private System.ComponentModel.IContainer components = null;
        private TabControl tabs;
        private TabPage tabAccounts;
        private TabPage tabTransactions;
        private TabPage tabDiagnostics;
        private Controls.AccountsIngestionControl accountsIngestion;
        private Controls.TransactionIngestionControl transactionIngestion;
        private Controls.FinanceDiagnosticsControl financeDiagnostics;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tabs = new TabControl();
            tabAccounts = new TabPage();
            tabTransactions = new TabPage();
            tabDiagnostics = new TabPage();
            accountsIngestion = new Controls.AccountsIngestionControl();
            transactionIngestion = new Controls.TransactionIngestionControl();
            financeDiagnostics = new Controls.FinanceDiagnosticsControl();
            tabs.SuspendLayout();
            tabAccounts.SuspendLayout();
            tabTransactions.SuspendLayout();
            tabDiagnostics.SuspendLayout();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(tabAccounts);
            tabs.Controls.Add(tabTransactions);
            tabs.Controls.Add(tabDiagnostics);
            tabs.Dock = DockStyle.Fill;
            tabs.Name = "tabs";
            // 
            // tabAccounts
            // 
            tabAccounts.Controls.Add(accountsIngestion);
            tabAccounts.Name = "tabAccounts";
            tabAccounts.Padding = new Padding(4);
            tabAccounts.Text = "Accounts";
            // 
            // tabTransactions
            // 
            tabTransactions.Controls.Add(transactionIngestion);
            tabTransactions.Name = "tabTransactions";
            tabTransactions.Padding = new Padding(4);
            tabTransactions.Text = "Transactions";
            // 
            // tabDiagnostics
            // 
            tabDiagnostics.Controls.Add(financeDiagnostics);
            tabDiagnostics.Name = "tabDiagnostics";
            tabDiagnostics.Padding = new Padding(4);
            tabDiagnostics.Text = "Diagnostics";
            // 
            // hosted controls
            // 
            accountsIngestion.Dock = DockStyle.Fill;
            transactionIngestion.Dock = DockStyle.Fill;
            financeDiagnostics.Dock = DockStyle.Fill;
            // 
            // FinanceDataCenterForm
            // 
            ClientSize = new Size(1100, 760);
            Controls.Add(tabs);
            Name = "FinanceDataCenterForm";
            Text = "Finance Data Center";
            tabs.ResumeLayout(false);
            tabAccounts.ResumeLayout(false);
            tabTransactions.ResumeLayout(false);
            tabDiagnostics.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
