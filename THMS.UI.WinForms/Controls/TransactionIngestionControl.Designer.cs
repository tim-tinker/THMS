namespace THMS.UI.WinForms.Controls
{
    partial class TransactionIngestionControl
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Panel pnlFileImport;
        private Label lblFileImport;
        private TextBox txtFilePaths;
        private ThmsButton btnBrowseFiles;
        private ThmsButton btnImportFiles;
        private Label lblFileHelp;
        private Panel pnlPlaidImport;
        private Label lblPlaidImport;
        private Label lblPlaidHelp;
        private ThmsButton btnSyncPlaid;
        private Panel pnlLedger;
        private Label lblLedger;
        private TransactionUpdaterControl transactionUpdater;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            pnlFileImport = new Panel();
            lblFileImport = new Label();
            txtFilePaths = new TextBox();
            btnBrowseFiles = new ThmsButton();
            btnImportFiles = new ThmsButton();
            lblFileHelp = new Label();
            pnlPlaidImport = new Panel();
            lblPlaidImport = new Label();
            lblPlaidHelp = new Label();
            btnSyncPlaid = new ThmsButton();
            pnlLedger = new Panel();
            lblLedger = new Label();
            transactionUpdater = new TransactionUpdaterControl();
            layout.SuspendLayout();
            pnlFileImport.SuspendLayout();
            pnlPlaidImport.SuspendLayout();
            pnlLedger.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(pnlFileImport, 0, 0);
            layout.Controls.Add(pnlPlaidImport, 0, 1);
            layout.Controls.Add(pnlLedger, 0, 2);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // 
            // pnlFileImport
            // 
            pnlFileImport.Controls.Add(lblFileHelp);
            pnlFileImport.Controls.Add(CreateFileToolbar());
            pnlFileImport.Controls.Add(lblFileImport);
            pnlFileImport.Dock = DockStyle.Fill;
            pnlFileImport.Name = "pnlFileImport";
            pnlFileImport.Padding = new Padding(8);
            // 
            // lblFileImport
            // 
            lblFileImport.AutoSize = false;
            lblFileImport.Dock = DockStyle.Top;
            lblFileImport.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblFileImport.Height = 36;
            lblFileImport.Name = "lblFileImport";
            lblFileImport.Text = "Import Transaction Data";
            lblFileImport.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblFileHelp
            // 
            lblFileHelp.Dock = DockStyle.Fill;
            lblFileHelp.Name = "lblFileHelp";
            lblFileHelp.Padding = new Padding(0, 4, 0, 0);
            lblFileHelp.Text = "Spreadsheet transactions import into Unreconciled.";
            // 
            // pnlPlaidImport
            // 
            pnlPlaidImport.Controls.Add(lblPlaidHelp);
            pnlPlaidImport.Controls.Add(CreatePlaidToolbar());
            pnlPlaidImport.Controls.Add(lblPlaidImport);
            pnlPlaidImport.Dock = DockStyle.Fill;
            pnlPlaidImport.Name = "pnlPlaidImport";
            pnlPlaidImport.Padding = new Padding(8);
            // 
            // lblPlaidImport
            // 
            lblPlaidImport.AutoSize = false;
            lblPlaidImport.Dock = DockStyle.Top;
            lblPlaidImport.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPlaidImport.Height = 28;
            lblPlaidImport.Name = "lblPlaidImport";
            lblPlaidImport.Text = "Plaid";
            lblPlaidImport.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblPlaidHelp
            // 
            lblPlaidHelp.Dock = DockStyle.Fill;
            lblPlaidHelp.Name = "lblPlaidHelp";
            lblPlaidHelp.Padding = new Padding(0, 4, 0, 0);
            lblPlaidHelp.Text = "Posted activity imports into Unreconciled. First history is imported from Link Accounts.";
            // 
            // pnlLedger
            // 
            pnlLedger.Controls.Add(transactionUpdater);
            pnlLedger.Controls.Add(lblLedger);
            pnlLedger.Dock = DockStyle.Fill;
            pnlLedger.Name = "pnlLedger";
            pnlLedger.Padding = new Padding(8);
            // 
            // lblLedger
            // 
            lblLedger.AutoSize = false;
            lblLedger.Dock = DockStyle.Top;
            lblLedger.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblLedger.Height = 36;
            lblLedger.Name = "lblLedger";
            lblLedger.Text = "Ledger Update";
            lblLedger.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // transactionUpdater
            // 
            transactionUpdater.Dock = DockStyle.Fill;
            transactionUpdater.Name = "transactionUpdater";
            // 
            // TransactionIngestionControl
            // 
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(layout);
            Name = "TransactionIngestionControl";
            Size = new Size(1000, 720);
            layout.ResumeLayout(false);
            pnlFileImport.ResumeLayout(false);
            pnlPlaidImport.ResumeLayout(false);
            pnlLedger.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Control CreateFileToolbar()
        {
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                ColumnCount = 3,
                Name = "fileToolbar"
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            txtFilePaths.Dock = DockStyle.Fill;
            txtFilePaths.Name = "txtFilePaths";
            btnBrowseFiles.Text = "Browse…";
            btnBrowseFiles.AutoSize = true;
            btnBrowseFiles.Name = "btnBrowseFiles";
            btnBrowseFiles.Click += OnBrowseFiles;
            btnImportFiles.Text = "Import Transactions";
            btnImportFiles.AutoSize = true;
            btnImportFiles.Name = "btnImportFiles";
            btnImportFiles.Click += OnImportFiles;
            toolbar.Controls.Add(txtFilePaths, 0, 0);
            toolbar.Controls.Add(btnBrowseFiles, 1, 0);
            toolbar.Controls.Add(btnImportFiles, 2, 0);
            return toolbar;
        }

        private Control CreatePlaidToolbar()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Name = "plaidToolbar",
                WrapContents = false
            };
            btnSyncPlaid.Text = "Sync Data";
            btnSyncPlaid.AutoSize = true;
            btnSyncPlaid.Name = "btnSyncPlaid";
            btnSyncPlaid.Click += OnSyncPlaid;
            toolbar.Controls.Add(btnSyncPlaid);
            return toolbar;
        }
    }
}
