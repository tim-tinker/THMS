namespace THMS.UI.WinForms.Controls
{
    partial class PlannedPaymentsControl
    {
        private System.ComponentModel.IContainer components = null;
        private Panel pnlRoot;
        private Label lblHeader;
        private DataGridView gridPayments;
        private Label lblStatus;
        private DateTimePicker dtUntil;
        private ThmsButton btnAdd;
        private ThmsButton btnDelete;
        private ThmsButton btnMinimums;
        private ThmsButton btnPromotions;
        private ThmsButton btnExtra;
        private ThmsButton btnPayAll;

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
            gridPayments = new DataGridView();
            lblStatus = new Label();
            dtUntil = new DateTimePicker();
            btnAdd = new ThmsButton();
            btnDelete = new ThmsButton();
            btnMinimums = new ThmsButton();
            btnPromotions = new ThmsButton();
            btnExtra = new ThmsButton();
            btnPayAll = new ThmsButton();
            ((System.ComponentModel.ISupportInitialize)gridPayments).BeginInit();
            pnlRoot.SuspendLayout();
            SuspendLayout();
            pnlRoot.Controls.Add(gridPayments);
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
            lblHeader.Text = "Planned Payments";
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.AutoSize = false;
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 28;
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "Ready.";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            gridPayments.AllowUserToAddRows = false;
            gridPayments.AllowUserToDeleteRows = false;
            gridPayments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridPayments.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridPayments.Dock = DockStyle.Fill;
            gridPayments.Name = "gridPayments";
            gridPayments.ReadOnly = true;
            gridPayments.RowHeadersVisible = false;
            gridPayments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(pnlRoot);
            Name = "PlannedPaymentsControl";
            Size = new Size(1000, 280);
            ((System.ComponentModel.ISupportInitialize)gridPayments).EndInit();
            pnlRoot.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Control CreateToolbar()
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 4),
                Name = "toolbar"
            };
            dtUntil.Format = DateTimePickerFormat.Short;
            dtUntil.Width = 120;
            dtUntil.Name = "dtUntil";
            dtUntil.Margin = new Padding(0, 8, 8, 4);
            btnAdd.Text = "Add Planned Payment";
            btnAdd.Name = "btnAdd";
            btnAdd.Margin = new Padding(0, 4, 8, 4);
            btnAdd.Click += OnAdd;
            btnDelete.Text = "Delete Planned Payment";
            btnDelete.Name = "btnDelete";
            btnDelete.Destructive = true;
            btnDelete.Margin = new Padding(0, 4, 8, 4);
            btnDelete.Click += OnDelete;
            btnMinimums.Text = "Generate Minimum Payments";
            btnMinimums.Name = "btnMinimums";
            btnMinimums.Margin = new Padding(0, 4, 8, 4);
            btnMinimums.Click += OnGenerateMinimums;
            btnPromotions.Text = "Generate Promotion Payments";
            btnPromotions.Name = "btnPromotions";
            btnPromotions.Margin = new Padding(0, 4, 8, 4);
            btnPromotions.Click += OnGeneratePromotions;
            btnExtra.Text = "Generate Extra Principal Payments";
            btnExtra.Name = "btnExtra";
            btnExtra.Margin = new Padding(0, 4, 8, 4);
            btnExtra.Click += OnGenerateExtra;
            btnPayAll.Text = "Generate Pay-All-Due";
            btnPayAll.Name = "btnPayAll";
            btnPayAll.Margin = new Padding(0, 4, 8, 4);
            btnPayAll.Click += OnGeneratePayAll;
            toolbar.Controls.Add(new Label { Text = "Plan until", AutoSize = true, Margin = new Padding(0, 12, 8, 4) });
            toolbar.Controls.Add(dtUntil);
            toolbar.Controls.Add(btnAdd);
            toolbar.Controls.Add(btnDelete);
            toolbar.Controls.Add(btnMinimums);
            toolbar.Controls.Add(btnPromotions);
            toolbar.Controls.Add(btnExtra);
            toolbar.Controls.Add(btnPayAll);
            return toolbar;
        }
    }
}
