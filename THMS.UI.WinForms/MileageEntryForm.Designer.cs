namespace THMS.UI.WinForms
{
    partial class MileageEntryForm
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
            lblDate = new Label();
            lblOdometer = new Label();
            dtpDate = new DateTimePicker();
            numOdometer = new NumericUpDown();
            btnSave = new Button();
            btnCancel = new Button();
            label1 = new Label();
            _numGallons = new NumericUpDown();
            _numCost = new NumericUpDown();
            label2 = new Label();
            _checkFull = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)numOdometer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numGallons).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numCost).BeginInit();
            SuspendLayout();
            // 
            // lblDate
            // 
            lblDate.Location = new Point(12, 15);
            lblDate.Name = "lblDate";
            lblDate.Size = new Size(120, 25);
            lblDate.TabIndex = 5;
            lblDate.Text = "Date:";
            // 
            // lblOdometer
            // 
            lblOdometer.Location = new Point(12, 55);
            lblOdometer.Name = "lblOdometer";
            lblOdometer.Size = new Size(120, 25);
            lblOdometer.TabIndex = 3;
            lblOdometer.Text = "Odometer (mi):";
            // 
            // dtpDate
            // 
            dtpDate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dtpDate.Format = DateTimePickerFormat.Short;
            dtpDate.Location = new Point(140, 12);
            dtpDate.Name = "dtpDate";
            dtpDate.Size = new Size(246, 35);
            dtpDate.TabIndex = 4;
            // 
            // numOdometer
            // 
            numOdometer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            numOdometer.Location = new Point(140, 52);
            numOdometer.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numOdometer.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numOdometer.Name = "numOdometer";
            numOdometer.Size = new Size(246, 35);
            numOdometer.TabIndex = 2;
            numOdometer.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(166, 233);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 35);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(286, 233);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 35);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 95);
            label1.Name = "label1";
            label1.Size = new Size(81, 30);
            label1.TabIndex = 6;
            label1.Text = "Gallons";
            // 
            // _numGallons
            // 
            _numGallons.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numGallons.DecimalPlaces = 3;
            _numGallons.Location = new Point(140, 93);
            _numGallons.Name = "_numGallons";
            _numGallons.Size = new Size(246, 35);
            _numGallons.TabIndex = 7;
            // 
            // _numCost
            // 
            _numCost.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numCost.DecimalPlaces = 2;
            _numCost.Location = new Point(140, 134);
            _numCost.Name = "_numCost";
            _numCost.Size = new Size(246, 35);
            _numCost.TabIndex = 8;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 136);
            label2.Name = "label2";
            label2.Size = new Size(54, 30);
            label2.TabIndex = 9;
            label2.Text = "Cost";
            // 
            // _checkFull
            // 
            _checkFull.AutoSize = true;
            _checkFull.Checked = true;
            _checkFull.CheckState = CheckState.Checked;
            _checkFull.Location = new Point(140, 175);
            _checkFull.Name = "_checkFull";
            _checkFull.Size = new Size(135, 34);
            _checkFull.TabIndex = 10;
            _checkFull.Text = "Filled Tank";
            _checkFull.UseVisualStyleBackColor = true;
            // 
            // MileageEntryForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(406, 288);
            Controls.Add(_checkFull);
            Controls.Add(label2);
            Controls.Add(_numCost);
            Controls.Add(_numGallons);
            Controls.Add(label1);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(numOdometer);
            Controls.Add(lblOdometer);
            Controls.Add(dtpDate);
            Controls.Add(lblDate);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "MileageEntryForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Mileage";
            ((System.ComponentModel.ISupportInitialize)numOdometer).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numGallons).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numCost).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Label lblOdometer;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.NumericUpDown numOdometer;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private Label label1;
        private NumericUpDown _numGallons;
        private NumericUpDown _numCost;
        private Label label2;
        private CheckBox _checkFull;
    }
}
