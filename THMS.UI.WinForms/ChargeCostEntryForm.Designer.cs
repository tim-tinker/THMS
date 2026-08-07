namespace THMS.UI.WinForms
{
    partial class ChargeCostEntryForm
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
            this.lblTimestamp = new System.Windows.Forms.Label();
            this.lblCost = new System.Windows.Forms.Label();
            this.dtpTimestamp = new System.Windows.Forms.DateTimePicker();
            this.numCost = new System.Windows.Forms.NumericUpDown();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.numCost)).BeginInit();
            this.SuspendLayout();

            // lblTimestamp
            this.lblTimestamp.Location = new System.Drawing.Point(12, 15);
            this.lblTimestamp.Name = "lblTimestamp";
            this.lblTimestamp.Size = new System.Drawing.Size(120, 25);
            this.lblTimestamp.Text = "Timestamp:";

            // dtpTimestamp
            this.dtpTimestamp.Location = new System.Drawing.Point(140, 12);
            this.dtpTimestamp.Name = "dtpTimestamp";
            this.dtpTimestamp.Size = new System.Drawing.Size(220, 27);

            // lblCost
            this.lblCost.Location = new System.Drawing.Point(12, 55);
            this.lblCost.Name = "lblCost";
            this.lblCost.Size = new System.Drawing.Size(120, 25);
            this.lblCost.Text = "Cost ($):";

            // numCost
            this.numCost.Location = new System.Drawing.Point(140, 52);
            this.numCost.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numCost.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numCost.Name = "numCost";
            this.numCost.Size = new System.Drawing.Size(120, 27);

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

            // ChargeCostEntryForm
            this.ClientSize = new System.Drawing.Size(380, 155);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.numCost);
            this.Controls.Add(this.lblCost);
            this.Controls.Add(this.dtpTimestamp);
            this.Controls.Add(this.lblTimestamp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "ChargeCostEntryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Charge Cost";

            ((System.ComponentModel.ISupportInitialize)(this.numCost)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblTimestamp;
        private System.Windows.Forms.Label lblCost;
        private System.Windows.Forms.DateTimePicker dtpTimestamp;
        private System.Windows.Forms.NumericUpDown numCost;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
