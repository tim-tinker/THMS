namespace THMS.UI.WinForms.Controls
{
    partial class TransactionIngestionControl
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Panel pnlFileImport;
        private Label lblFileImport;
        private TextBox txtFilePaths;
        private Button btnBrowseFiles;
        private Button btnLoadFiles;
        private Button btnImportFiles;
        private DataGridView gridFilePreview;
        private Label lblFileStatus;
        private Panel pnlPlaidImport;
        private Label lblPlaidImport;
        private DateTimePicker dtStart;
        private DateTimePicker dtEnd;
        private Button btnDownloadPlaid;
        private Button btnImportPlaid;
        private DataGridView gridPlaidPreview;
        private Label lblPlaidStatus;
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
            btnBrowseFiles = new Button();
            btnLoadFiles = new Button();
            btnImportFiles = new Button();
            gridFilePreview = new DataGridView();
            lblFileStatus = new Label();
            pnlPlaidImport = new Panel();
            lblPlaidImport = new Label();
            dtStart = new DateTimePicker();
            dtEnd = new DateTimePicker();
            btnDownloadPlaid = new Button();
            btnImportPlaid = new Button();
            gridPlaidPreview = new DataGridView();
            lblPlaidStatus = new Label();
            pnlLedger = new Panel();
            lblLedger = new Label();
            transactionUpdater = new TransactionUpdaterControl();
            ((System.ComponentModel.ISupportInitialize)gridFilePreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridPlaidPreview).BeginInit();
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
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            // 
            // pnlFileImport
            // 
            pnlFileImport.Controls.Add(gridFilePreview);
            pnlFileImport.Controls.Add(CreateFileToolbar());
            pnlFileImport.Controls.Add(lblFileImport);
            pnlFileImport.Controls.Add(lblFileStatus);
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
            // lblFileStatus
            // 
            lblFileStatus.Dock = DockStyle.Bottom;
            lblFileStatus.Height = 24;
            lblFileStatus.Name = "lblFileStatus";
            lblFileStatus.Text = "Ready.";
            lblFileStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // gridFilePreview
            // 
            gridFilePreview.AllowUserToAddRows = false;
            gridFilePreview.AllowUserToDeleteRows = false;
            gridFilePreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridFilePreview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridFilePreview.Dock = DockStyle.Fill;
            gridFilePreview.Name = "gridFilePreview";
            gridFilePreview.ReadOnly = true;
            gridFilePreview.RowHeadersVisible = false;
            gridFilePreview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // 
            // pnlPlaidImport
            // 
            pnlPlaidImport.Controls.Add(gridPlaidPreview);
            pnlPlaidImport.Controls.Add(CreatePlaidToolbar());
            pnlPlaidImport.Controls.Add(lblPlaidImport);
            pnlPlaidImport.Controls.Add(lblPlaidStatus);
            pnlPlaidImport.Dock = DockStyle.Fill;
            pnlPlaidImport.Name = "pnlPlaidImport";
            pnlPlaidImport.Padding = new Padding(8);
            // 
            // lblPlaidImport
            // 
            lblPlaidImport.AutoSize = false;
            lblPlaidImport.Dock = DockStyle.Top;
            lblPlaidImport.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPlaidImport.Height = 36;
            lblPlaidImport.Name = "lblPlaidImport";
            lblPlaidImport.Text = "Plaid Transaction Import";
            lblPlaidImport.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblPlaidStatus
            // 
            lblPlaidStatus.Dock = DockStyle.Bottom;
            lblPlaidStatus.Height = 24;
            lblPlaidStatus.Name = "lblPlaidStatus";
            lblPlaidStatus.Text = "Ready.";
            lblPlaidStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // gridPlaidPreview
            // 
            gridPlaidPreview.AllowUserToAddRows = false;
            gridPlaidPreview.AllowUserToDeleteRows = false;
            gridPlaidPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridPlaidPreview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridPlaidPreview.Dock = DockStyle.Fill;
            gridPlaidPreview.Name = "gridPlaidPreview";
            gridPlaidPreview.ReadOnly = true;
            gridPlaidPreview.RowHeadersVisible = false;
            gridPlaidPreview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
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
            ((System.ComponentModel.ISupportInitialize)gridFilePreview).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridPlaidPreview).EndInit();
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
                ColumnCount = 4,
                Name = "fileToolbar"
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            txtFilePaths.Dock = DockStyle.Fill;
            txtFilePaths.Name = "txtFilePaths";
            btnBrowseFiles.Text = "Browse…";
            btnBrowseFiles.AutoSize = true;
            btnBrowseFiles.Name = "btnBrowseFiles";
            btnBrowseFiles.Click += OnBrowseFiles;
            btnLoadFiles.Text = "Load Files";
            btnLoadFiles.AutoSize = true;
            btnLoadFiles.Name = "btnLoadFiles";
            btnLoadFiles.Click += OnLoadFiles;
            btnImportFiles.Text = "Import Transactions";
            btnImportFiles.AutoSize = true;
            btnImportFiles.Name = "btnImportFiles";
            btnImportFiles.Click += OnImportFiles;
            toolbar.Controls.Add(txtFilePaths, 0, 0);
            toolbar.Controls.Add(btnBrowseFiles, 1, 0);
            toolbar.Controls.Add(btnLoadFiles, 2, 0);
            toolbar.Controls.Add(btnImportFiles, 3, 0);
            return toolbar;
        }

        private Control CreatePlaidToolbar()
        {
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                ColumnCount = 6,
                Name = "plaidToolbar"
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var lblStart = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Text = "Start",
                TextAlign = ContentAlignment.MiddleLeft
            };
            dtStart.Format = DateTimePickerFormat.Short;
            dtStart.Name = "dtStart";
            dtStart.Width = 110;
            var lblEnd = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Text = "End",
                TextAlign = ContentAlignment.MiddleLeft
            };
            dtEnd.Format = DateTimePickerFormat.Short;
            dtEnd.Name = "dtEnd";
            dtEnd.Width = 110;
            btnDownloadPlaid.Text = "Download Plaid Transactions";
            btnDownloadPlaid.AutoSize = true;
            btnDownloadPlaid.Name = "btnDownloadPlaid";
            btnDownloadPlaid.Click += OnDownloadPlaid;
            btnImportPlaid.Text = "Import Plaid Transactions";
            btnImportPlaid.AutoSize = true;
            btnImportPlaid.Name = "btnImportPlaid";
            btnImportPlaid.Click += OnImportPlaid;
            toolbar.Controls.Add(lblStart, 0, 0);
            toolbar.Controls.Add(dtStart, 1, 0);
            toolbar.Controls.Add(lblEnd, 2, 0);
            toolbar.Controls.Add(dtEnd, 3, 0);
            toolbar.Controls.Add(btnDownloadPlaid, 4, 0);
            toolbar.Controls.Add(btnImportPlaid, 5, 0);
            return toolbar;
        }
    }
}
