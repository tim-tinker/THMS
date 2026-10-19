namespace THMS.UI.WinForms.Controls
{
    partial class BudgetRuleEditor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblName = new Label();
            txtName = new TextBox();
            lblAmount = new Label();
            numAmount = new NumericUpDown();
            lblFrequency = new Label();
            cmbFrequency = new ComboBox();
            lblPeriodStart = new Label();
            dtPeriodStart = new DateTimePicker();
            chkActive = new CheckBox();
            lblCategories = new Label();
            treeCategories = new TreeView();
            pnlButtons = new Panel();
            flowActions = new FlowLayoutPanel();
            btnAdd = new ThmsButton();
            btnSave = new ThmsButton();
            btnDelete = new ThmsButton();
            ((System.ComponentModel.ISupportInitialize)numAmount).BeginInit();
            pnlButtons.SuspendLayout();
            flowActions.SuspendLayout();
            SuspendLayout();
            lblName.AutoSize = true;
            lblName.Location = new Point(20, 20);
            lblName.Name = "lblName";
            lblName.Text = "Budget Name:";
            txtName.Location = new Point(150, 16);
            txtName.Name = "txtName";
            txtName.Size = new Size(240, 23);
            lblAmount.AutoSize = true;
            lblAmount.Location = new Point(20, 56);
            lblAmount.Name = "lblAmount";
            lblAmount.Text = "Default Amount:";
            numAmount.DecimalPlaces = 2;
            numAmount.Location = new Point(150, 52);
            numAmount.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numAmount.Minimum = new decimal(new int[] { 100000000, 0, 0, -2147483648 });
            numAmount.Name = "numAmount";
            numAmount.Size = new Size(240, 23);
            lblFrequency.AutoSize = true;
            lblFrequency.Location = new Point(20, 92);
            lblFrequency.Name = "lblFrequency";
            lblFrequency.Text = "Frequency:";
            cmbFrequency.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFrequency.Location = new Point(150, 88);
            cmbFrequency.Name = "cmbFrequency";
            cmbFrequency.Size = new Size(240, 23);
            lblPeriodStart.AutoSize = true;
            lblPeriodStart.Location = new Point(20, 128);
            lblPeriodStart.Name = "lblPeriodStart";
            lblPeriodStart.Text = "Period Start:";
            dtPeriodStart.Format = DateTimePickerFormat.Short;
            dtPeriodStart.Location = new Point(150, 124);
            dtPeriodStart.Name = "dtPeriodStart";
            dtPeriodStart.Size = new Size(240, 23);
            chkActive.AutoSize = true;
            chkActive.Checked = true;
            chkActive.CheckState = CheckState.Checked;
            chkActive.Location = new Point(150, 160);
            chkActive.Name = "chkActive";
            chkActive.Text = "Active";
            lblCategories.AutoSize = true;
            lblCategories.Location = new Point(20, 196);
            lblCategories.Name = "lblCategories";
            lblCategories.Text = "Categories:";
            treeCategories.CheckBoxes = true;
            treeCategories.Location = new Point(150, 196);
            treeCategories.Name = "treeCategories";
            treeCategories.Size = new Size(240, 96);
            flowActions.Controls.Add(btnAdd);
            flowActions.Controls.Add(btnSave);
            flowActions.Controls.Add(btnDelete);
            flowActions.Dock = DockStyle.Fill;
            flowActions.FlowDirection = FlowDirection.LeftToRight;
            flowActions.WrapContents = false;
            flowActions.Margin = new Padding(0);
            flowActions.Name = "flowActions";
            flowActions.Padding = new Padding(12, 12, 12, 12);
            pnlButtons.Controls.Add(flowActions);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 80;
            pnlButtons.Name = "pnlButtons";
            btnAdd.Margin = new Padding(0, 0, 8, 0);
            btnAdd.Name = "btnAdd";
            btnAdd.Text = "Add";
            btnAdd.Click += OnAdd;
            btnSave.Margin = new Padding(0, 0, 8, 0);
            btnSave.Name = "btnSave";
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnDelete.Margin = new Padding(0);
            btnDelete.Name = "btnDelete";
            btnDelete.Destructive = true;
            btnDelete.Text = "Delete";
            btnDelete.Click += OnDelete;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 388);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblAmount);
            Controls.Add(numAmount);
            Controls.Add(lblFrequency);
            Controls.Add(cmbFrequency);
            Controls.Add(lblPeriodStart);
            Controls.Add(dtPeriodStart);
            Controls.Add(chkActive);
            Controls.Add(lblCategories);
            Controls.Add(treeCategories);
            Controls.Add(pnlButtons);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BudgetRuleEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Budget Rule";
            ((System.ComponentModel.ISupportInitialize)numAmount).EndInit();
            flowActions.ResumeLayout(false);
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblName;
        private TextBox txtName;
        private Label lblAmount;
        private NumericUpDown numAmount;
        private Label lblFrequency;
        private ComboBox cmbFrequency;
        private Label lblPeriodStart;
        private DateTimePicker dtPeriodStart;
        private CheckBox chkActive;
        private Label lblCategories;
        private TreeView treeCategories;
        private Panel pnlButtons;
        private FlowLayoutPanel flowActions;
        private ThmsButton btnAdd;
        private ThmsButton btnSave;
        private ThmsButton btnDelete;
    }
}
