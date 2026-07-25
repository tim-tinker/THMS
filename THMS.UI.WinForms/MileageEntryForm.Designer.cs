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
            this.lblDate = new System.Windows.Forms.Label();
            this.lblOdometer = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.numOdometer = new System.Windows.Forms.NumericUpDown();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.numOdometer)).BeginInit();
            this.SuspendLayout();

            // lblDate
            this.lblDate.Location = new System.Drawing.Point(12, 15);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(120, 25);
            this.lblDate.Text = "Date:";

            // dtpDate
            this.dtpDate.Location = new System.Drawing.Point(140, 12);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(220, 27);

            // lblOdometer
            this.lblOdometer.Location = new System.Drawing.Point(12, 55);
            this.lblOdometer.Name = "lblOdometer";
            this.lblOdometer.Size = new System.Drawing.Size(120, 25);
            this.lblOdometer.Text = "Odometer (mi):";

            // numOdometer
            this.numOdometer.Location = new System.Drawing.Point(140, 52);
            this.numOdometer.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numOdometer.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numOdometer.Name = "numOdometer";
            this.numOdometer.Size = new System.Drawing.Size(120, 27);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(140, 100);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 35);
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(260, 100);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // MileageEntryForm
            this.ClientSize = new System.Drawing.Size(380, 155);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.numOdometer);
            this.Controls.Add(this.lblOdometer);
            this.Controls.Add(this.dtpDate);
            this.Controls.Add(this.lblDate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "MileageEntryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Mileage";

            ((System.ComponentModel.ISupportInitialize)(this.numOdometer)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Label lblOdometer;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.NumericUpDown numOdometer;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
