namespace THMS.UI.WinForms
{
    partial class VehicleDetailForm
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
            this.lblName = new System.Windows.Forms.Label();
            this.lblMake = new System.Windows.Forms.Label();
            this.lblModel = new System.Windows.Forms.Label();
            this.lblYear = new System.Windows.Forms.Label();

            this.lblNameValue = new System.Windows.Forms.Label();
            this.lblMakeValue = new System.Windows.Forms.Label();
            this.lblModelValue = new System.Windows.Forms.Label();
            this.lblYearValue = new System.Windows.Forms.Label();

            this.mileageGrid = new System.Windows.Forms.DataGridView();
            this.chargingGrid = new System.Windows.Forms.DataGridView();
            this.fuelGrid = new System.Windows.Forms.DataGridView();
            this.maintenanceGrid = new System.Windows.Forms.DataGridView();

            this.btnAddMileage = new System.Windows.Forms.Button();
            this.btnAddCharging = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.mileageGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chargingGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fuelGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maintenanceGrid)).BeginInit();

            this.SuspendLayout();

            // Labels
            this.lblName.Text = "Name:";
            this.lblName.Location = new System.Drawing.Point(12, 15);

            this.lblMake.Text = "Make:";
            this.lblMake.Location = new System.Drawing.Point(12, 45);

            this.lblModel.Text = "Model:";
            this.lblModel.Location = new System.Drawing.Point(12, 75);

            this.lblYear.Text = "Year:";
            this.lblYear.Location = new System.Drawing.Point(12, 105);

            this.lblNameValue.Location = new System.Drawing.Point(140, 15);
            this.lblMakeValue.Location = new System.Drawing.Point(140, 45);
            this.lblModelValue.Location = new System.Drawing.Point(140, 75);
            this.lblYearValue.Location = new System.Drawing.Point(140, 105);

            // Mileage Grid
            this.mileageGrid.Location = new System.Drawing.Point(12, 150);
            this.mileageGrid.Size = new System.Drawing.Size(350, 150);
            this.mileageGrid.ReadOnly = true;
            this.mileageGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // Charging Grid
            this.chargingGrid.Location = new System.Drawing.Point(380, 150);
            this.chargingGrid.Size = new System.Drawing.Size(350, 150);
            this.chargingGrid.ReadOnly = true;
            this.chargingGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // Fuel Grid
            this.fuelGrid.Location = new System.Drawing.Point(12, 320);
            this.fuelGrid.Size = new System.Drawing.Size(350, 150);
            this.fuelGrid.ReadOnly = true;
            this.fuelGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // Maintenance Grid
            this.maintenanceGrid.Location = new System.Drawing.Point(380, 320);
            this.maintenanceGrid.Size = new System.Drawing.Size(350, 150);
            this.maintenanceGrid.ReadOnly = true;
            this.maintenanceGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // Buttons
            this.btnAddMileage.Text = "Add Mileage";
            this.btnAddMileage.Location = new System.Drawing.Point(12, 490);
            this.btnAddMileage.Size = new System.Drawing.Size(150, 35);
            this.btnAddMileage.Click += new System.EventHandler(this.btnAddMileage_Click);

            this.btnAddCharging.Text = "Add Charging Cost";
            this.btnAddCharging.Location = new System.Drawing.Point(380, 490);
            this.btnAddCharging.Size = new System.Drawing.Size(150, 35);
            this.btnAddCharging.Click += new System.EventHandler(this.btnAddCharging_Click);

            // Form
            this.ClientSize = new System.Drawing.Size(750, 550);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblMake);
            this.Controls.Add(this.lblModel);
            this.Controls.Add(this.lblYear);

            this.Controls.Add(this.lblNameValue);
            this.Controls.Add(this.lblMakeValue);
            this.Controls.Add(this.lblModelValue);
            this.Controls.Add(this.lblYearValue);

            this.Controls.Add(this.mileageGrid);
            this.Controls.Add(this.chargingGrid);
            this.Controls.Add(this.fuelGrid);
            this.Controls.Add(this.maintenanceGrid);

            this.Controls.Add(this.btnAddMileage);
            this.Controls.Add(this.btnAddCharging);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Vehicle Details";

            ((System.ComponentModel.ISupportInitialize)(this.mileageGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chargingGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fuelGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maintenanceGrid)).EndInit();

            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblMake;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.Label lblYear;

        private System.Windows.Forms.Label lblNameValue;
        private System.Windows.Forms.Label lblMakeValue;
        private System.Windows.Forms.Label lblModelValue;
        private System.Windows.Forms.Label lblYearValue;

        private System.Windows.Forms.DataGridView mileageGrid;
        private System.Windows.Forms.DataGridView chargingGrid;
        private System.Windows.Forms.DataGridView fuelGrid;
        private System.Windows.Forms.DataGridView maintenanceGrid;

        private System.Windows.Forms.Button btnAddMileage;
        private System.Windows.Forms.Button btnAddCharging;
    }
}
