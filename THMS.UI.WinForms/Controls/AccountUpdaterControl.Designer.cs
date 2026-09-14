namespace THMS.UI.WinForms.Controls
{
    partial class AccountUpdaterControl
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView gridAccounts;
        private FlowLayoutPanel pnlButtons;
        private ThmsButton btnAdd;
        private ThmsButton btnEdit;
        private ThmsButton btnDelete;
        private ThmsButton btnImport;
        private ThmsButton btnConnectPlaid;
        private ThmsButton btnDiagnostics;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            gridAccounts = new DataGridView();
            pnlButtons = new FlowLayoutPanel();
            btnAdd = new ThmsButton();
            btnEdit = new ThmsButton();
            btnDelete = new ThmsButton();
            btnImport = new ThmsButton();
            btnConnectPlaid = new ThmsButton();
            btnDiagnostics = new ThmsButton();
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)gridAccounts).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            // 
            // gridAccounts
            // 
            gridAccounts.AllowUserToAddRows = false;
            gridAccounts.AllowUserToDeleteRows = false;
            gridAccounts.AutoGenerateColumns = false;
            gridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAccounts.ColumnHeadersHeight = 40;
            gridAccounts.Dock = DockStyle.Fill;
            gridAccounts.Location = new Point(0, 0);
            gridAccounts.Name = "gridAccounts";
            gridAccounts.ReadOnly = true;
            gridAccounts.RowHeadersWidth = 72;
            gridAccounts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAccounts.TabIndex = 0;
            gridAccounts.Columns.AddRange(
                TextColumn("Name", "Name"),
                TextColumn("Type", "Type"),
                TextColumn("Institution", "Institution"),
                TextColumn("AccountNumber", "Account Number"),
                TextColumn("BalanceAsOf", "As Of"));
            // 
            // pnlButtons
            // 
            pnlButtons.Controls.Add(btnAdd);
            pnlButtons.Controls.Add(btnEdit);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Controls.Add(btnImport);
            pnlButtons.Controls.Add(btnConnectPlaid);
            pnlButtons.Controls.Add(btnDiagnostics);
            pnlButtons.AutoSize = true;
            pnlButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.FlowDirection = FlowDirection.LeftToRight;
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Padding = new Padding(8, 8, 8, 8);
            pnlButtons.Size = new Size(800, 56);
            pnlButtons.TabIndex = 1;
            pnlButtons.WrapContents = true;
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(8, 0, 8, 0);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnAdd
            // 
            btnAdd.Name = "btnAdd";
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Add Account";
            btnAdd.Click += OnAddAccount;
            // 
            // btnEdit
            // 
            btnEdit.Name = "btnEdit";
            btnEdit.TabIndex = 1;
            btnEdit.Text = "Edit Account";
            btnEdit.Click += OnEditAccount;
            // 
            // btnDelete
            // 
            btnDelete.Destructive = true;
            btnDelete.Name = "btnDelete";
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Delete Account";
            btnDelete.Click += OnDeleteAccount;
            // 
            // btnImport
            // 
            btnImport.Name = "btnImport";
            btnImport.TabIndex = 3;
            btnImport.Text = "Import";
            btnImport.Click += OnImportAccounts;
            // 
            // btnConnectPlaid
            // 
            btnConnectPlaid.Name = "btnConnectPlaid";
            btnConnectPlaid.TabIndex = 4;
            btnConnectPlaid.Text = "Connect to Plaid";
            btnConnectPlaid.Click += OnConnectToPlaid;
            // 
            // btnDiagnostics
            // 
            btnDiagnostics.Name = "btnDiagnostics";
            btnDiagnostics.TabIndex = 5;
            btnDiagnostics.Text = "Diagnostics";
            btnDiagnostics.Click += OnRunDiagnostics;
            // 
            // AccountUpdaterControl
            // 
            Controls.Add(gridAccounts);
            Controls.Add(lblStatus);
            Controls.Add(pnlButtons);
            Name = "AccountUpdaterControl";
            Size = new Size(800, 600);
            ((System.ComponentModel.ISupportInitialize)gridAccounts).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true
            };
    }
}
