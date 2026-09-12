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
        private Button btnAdd;
        private Button btnDelete;
        private Button btnMinimums;
        private Button btnPromotions;
        private Button btnExtra;
        private Button btnPayAll;

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
            btnAdd = new Button();
            btnDelete = new Button();
            btnMinimums = new Button();
            btnPromotions = new Button();
            btnExtra = new Button();
            btnPayAll = new Button();
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
                Height = 40,
                Name = "toolbar",
                WrapContents = false,
                AutoScroll = true
            };
            dtUntil.Format = DateTimePickerFormat.Short;
            dtUntil.Width = 110;
            dtUntil.Name = "dtUntil";
            btnAdd.Text = "Add Planned Payment";
            btnAdd.AutoSize = true;
            btnAdd.Name = "btnAdd";
            btnAdd.Click += OnAdd;
            btnDelete.Text = "Delete Planned Payment";
            btnDelete.AutoSize = true;
            btnDelete.Name = "btnDelete";
            btnDelete.Click += OnDelete;
            btnMinimums.Text = "Generate Minimum Payments";
            btnMinimums.AutoSize = true;
            btnMinimums.Name = "btnMinimums";
            btnMinimums.Click += OnGenerateMinimums;
            btnPromotions.Text = "Generate Promotion Payments";
            btnPromotions.AutoSize = true;
            btnPromotions.Name = "btnPromotions";
            btnPromotions.Click += OnGeneratePromotions;
            btnExtra.Text = "Generate Extra Principal Payments";
            btnExtra.AutoSize = true;
            btnExtra.Name = "btnExtra";
            btnExtra.Click += OnGenerateExtra;
            btnPayAll.Text = "Generate Pay-All-Due";
            btnPayAll.AutoSize = true;
            btnPayAll.Name = "btnPayAll";
            btnPayAll.Click += OnGeneratePayAll;
            toolbar.Controls.Add(new Label { Text = "Plan until", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
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
