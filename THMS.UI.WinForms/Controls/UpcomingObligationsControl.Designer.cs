namespace THMS.UI.WinForms.Controls
{
    partial class UpcomingObligationsControl
    {
        private System.ComponentModel.IContainer components = null;
        private Panel pnlRoot;
        private Label lblHeader;
        private Button btnRefresh;
        private Button btnAddStatement;
        private Button btnEditStatement;
        private Button btnDeleteStatement;
        private DataGridView gridObligations;
        private Label lblStatus;

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
            btnRefresh = new Button();
            btnAddStatement = new Button();
            btnEditStatement = new Button();
            btnDeleteStatement = new Button();
            gridObligations = new DataGridView();
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)gridObligations).BeginInit();
            pnlRoot.SuspendLayout();
            SuspendLayout();
            pnlRoot.Controls.Add(gridObligations);
            pnlRoot.Controls.Add(CreateToolbar());
            pnlRoot.Controls.Add(lblHeader);
            pnlRoot.Controls.Add(lblStatus);
            pnlRoot.Dock = DockStyle.Fill;
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Padding = new Padding(8);
            lblHeader.AutoSize = false;
            lblHeader.Dock = DockStyle.Top;
            lblHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblHeader.Height = 36;
            lblHeader.Name = "lblHeader";
            lblHeader.Text = "Upcoming Obligations";
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.AutoSize = false;
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 28;
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "Ready.";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            gridObligations.AllowUserToAddRows = false;
            gridObligations.AllowUserToDeleteRows = false;
            gridObligations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridObligations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridObligations.Dock = DockStyle.Fill;
            gridObligations.Name = "gridObligations";
            gridObligations.ReadOnly = true;
            gridObligations.RowHeadersVisible = false;
            gridObligations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(pnlRoot);
            Name = "UpcomingObligationsControl";
            Size = new Size(1000, 240);
            ((System.ComponentModel.ISupportInitialize)gridObligations).EndInit();
            pnlRoot.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Control CreateToolbar()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Name = "toolbar",
                WrapContents = false,
                AutoScroll = true
            };
            btnRefresh.Text = "Refresh Obligations";
            btnRefresh.AutoSize = true;
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Click += OnRefresh;
            btnAddStatement.Text = "Add Statement";
            btnAddStatement.AutoSize = true;
            btnAddStatement.Name = "btnAddStatement";
            btnAddStatement.Click += OnAddStatement;
            btnEditStatement.Text = "Edit Statement";
            btnEditStatement.AutoSize = true;
            btnEditStatement.Name = "btnEditStatement";
            btnEditStatement.Click += OnEditStatement;
            btnDeleteStatement.Text = "Delete Statement";
            btnDeleteStatement.AutoSize = true;
            btnDeleteStatement.Name = "btnDeleteStatement";
            btnDeleteStatement.Click += OnDeleteStatement;
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnAddStatement);
            toolbar.Controls.Add(btnEditStatement);
            toolbar.Controls.Add(btnDeleteStatement);
            return toolbar;
        }
    }
}
