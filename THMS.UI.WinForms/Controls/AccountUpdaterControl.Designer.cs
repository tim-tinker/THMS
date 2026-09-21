namespace THMS.UI.WinForms.Controls
{
    partial class AccountUpdaterControl
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView gridAccounts;
        private FlowLayoutPanel pnlButtons;
        private ThmsButton btnAdd;
        private ThmsButton btnDelete;
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
            btnDelete = new ThmsButton();
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
                NameColumn(),
                TextColumn("StatementDate", "Statement Date"),
                MoneyColumn("StatementBalance", "Balance"),
                MoneyColumn("InterestPaid", "Interest Paid"),
                MoneyColumn("AmountDue", "Amount Due"),
                TextColumn("Paid", "Paid"),
                TextColumn("DueDate", "Due"),
                TextColumn("Apr", "APR"),
                MoneyColumn("CreditLimit", "Credit Limit"));
            foreach (DataGridViewColumn column in gridAccounts.Columns)
                column.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // pnlButtons
            // 
            pnlButtons.Controls.Add(btnAdd);
            pnlButtons.Controls.Add(btnDiagnostics);
            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.AutoSize = true;
            pnlButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.FlowDirection = FlowDirection.LeftToRight;
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Padding = new Padding(12, 12, 12, 16);
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
            // btnDelete
            // 
            btnDelete.Destructive = true;
            btnDelete.Name = "btnDelete";
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Delete Account";
            btnDelete.Click += OnDeleteAccount;
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

        private static DataGridViewLinkColumn NameColumn() =>
            new()
            {
                DataPropertyName = "Name",
                HeaderText = "Account",
                Name = "Name",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                LinkBehavior = LinkBehavior.HoverUnderline,
                TrackVisitedState = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, bool fill = false) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                AutoSizeMode = fill
                    ? DataGridViewAutoSizeColumnMode.Fill
                    : DataGridViewAutoSizeColumnMode.AllCells
            };

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }
    }
}
