namespace THMS.UI.WinForms
{
    partial class AddVehicleForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblName = new Label();
            lblMake = new Label();
            lblModel = new Label();
            lblYear = new Label();
            txtName = new TextBox();
            txtMake = new TextBox();
            txtModel = new TextBox();
            numYear = new NumericUpDown();
            btnSave = new Button();
            btnCancel = new Button();
            _checkEv = new CheckBox();
            _numericFuelCapacity = new NumericUpDown();
            _labelCapacity = new Label();
            _labelFuelType = new Label();
            ((System.ComponentModel.ISupportInitialize)numYear).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numericFuelCapacity).BeginInit();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.Location = new Point(12, 15);
            lblName.Name = "lblName";
            lblName.Size = new Size(120, 25);
            lblName.TabIndex = 9;
            lblName.Text = "Name:";
            // 
            // lblMake
            // 
            lblMake.Location = new Point(12, 55);
            lblMake.Name = "lblMake";
            lblMake.Size = new Size(120, 25);
            lblMake.TabIndex = 7;
            lblMake.Text = "Make:";
            // 
            // lblModel
            // 
            lblModel.Location = new Point(12, 95);
            lblModel.Name = "lblModel";
            lblModel.Size = new Size(120, 25);
            lblModel.TabIndex = 5;
            lblModel.Text = "Model:";
            // 
            // lblYear
            // 
            lblYear.Location = new Point(12, 135);
            lblYear.Name = "lblYear";
            lblYear.Size = new Size(120, 25);
            lblYear.TabIndex = 3;
            lblYear.Text = "Year:";
            // 
            // txtName
            // 
            txtName.Location = new Point(154, 12);
            txtName.Name = "txtName";
            txtName.Size = new Size(220, 35);
            txtName.TabIndex = 0;
            // 
            // txtMake
            // 
            txtMake.Location = new Point(154, 52);
            txtMake.Name = "txtMake";
            txtMake.Size = new Size(220, 35);
            txtMake.TabIndex = 1;
            // 
            // txtModel
            // 
            txtModel.Location = new Point(154, 92);
            txtModel.Name = "txtModel";
            txtModel.Size = new Size(220, 35);
            txtModel.TabIndex = 2;
            // 
            // numYear
            // 
            numYear.Location = new Point(154, 132);
            numYear.Maximum = new decimal(new int[] { 2050, 0, 0, 0 });
            numYear.Minimum = new decimal(new int[] { 1950, 0, 0, 0 });
            numYear.Name = "numYear";
            numYear.Size = new Size(120, 35);
            numYear.TabIndex = 3;
            numYear.Value = new decimal(new int[] { 2024, 0, 0, 0 });
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(277, 269);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 35);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(397, 269);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 35);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;
            // 
            // _checkEv
            // 
            _checkEv.AutoSize = true;
            _checkEv.Location = new Point(154, 173);
            _checkEv.Name = "_checkEv";
            _checkEv.Size = new Size(219, 34);
            _checkEv.TabIndex = 4;
            _checkEv.Text = "Electric Vehicle (EV)";
            _checkEv.UseVisualStyleBackColor = true;
            _checkEv.CheckedChanged += OnCheckedChangedEv;
            // 
            // _numericFuelCapacity
            // 
            _numericFuelCapacity.Location = new Point(154, 213);
            _numericFuelCapacity.Name = "_numericFuelCapacity";
            _numericFuelCapacity.Size = new Size(120, 35);
            _numericFuelCapacity.TabIndex = 5;
            // 
            // _labelCapacity
            // 
            _labelCapacity.AutoSize = true;
            _labelCapacity.Location = new Point(12, 215);
            _labelCapacity.Name = "_labelCapacity";
            _labelCapacity.Size = new Size(136, 30);
            _labelCapacity.TabIndex = 15;
            _labelCapacity.Text = "Fuel Capacity";
            // 
            // _labelFuelType
            // 
            _labelFuelType.AutoSize = true;
            _labelFuelType.Location = new Point(280, 215);
            _labelFuelType.Name = "_labelFuelType";
            _labelFuelType.Size = new Size(79, 30);
            _labelFuelType.TabIndex = 16;
            _labelFuelType.Text = "gallons";
            // 
            // AddVehicleForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(517, 324);
            Controls.Add(_labelFuelType);
            Controls.Add(_labelCapacity);
            Controls.Add(_numericFuelCapacity);
            Controls.Add(_checkEv);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(numYear);
            Controls.Add(lblYear);
            Controls.Add(txtModel);
            Controls.Add(lblModel);
            Controls.Add(txtMake);
            Controls.Add(lblMake);
            Controls.Add(txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "AddVehicleForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Vehicle";
            ((System.ComponentModel.ISupportInitialize)numYear).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numericFuelCapacity).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblMake;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.Label lblYear;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtMake;
        private System.Windows.Forms.TextBox txtModel;
        private System.Windows.Forms.NumericUpDown numYear;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private CheckBox _checkEv;
        private NumericUpDown _numericFuelCapacity;
        private Label _labelCapacity;
        private Label _labelFuelType;
    }
}
