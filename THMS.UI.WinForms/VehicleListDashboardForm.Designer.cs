namespace THMS.UI.WinForms
{
    partial class VehicleListDashboardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            this.vehicleGrid = new System.Windows.Forms.DataGridView();
            this.btnAddVehicle = new System.Windows.Forms.Button();
            this.btnDetails = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.vehicleGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // vehicleGrid
            // 
            this.vehicleGrid.AllowUserToAddRows = false;
            this.vehicleGrid.AllowUserToDeleteRows = false;
            this.vehicleGrid.AllowUserToResizeRows = false;
            this.vehicleGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.vehicleGrid.Location = new System.Drawing.Point(12, 12);
            this.vehicleGrid.MultiSelect = false;
            this.vehicleGrid.Name = "vehicleGrid";
            this.vehicleGrid.ReadOnly = true;
            this.vehicleGrid.RowHeadersVisible = false;
            this.vehicleGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.vehicleGrid.Size = new System.Drawing.Size(560, 300);
            this.vehicleGrid.TabIndex = 0;
            this.vehicleGrid.SelectionChanged += new System.EventHandler(this.vehicleGrid_SelectionChanged);
            // 
            // btnAddVehicle
            // 
            this.btnAddVehicle.Location = new System.Drawing.Point(12, 325);
            this.btnAddVehicle.Name = "btnAddVehicle";
            this.btnAddVehicle.Size = new System.Drawing.Size(120, 35);
            this.btnAddVehicle.TabIndex = 1;
            this.btnAddVehicle.Text = "Add Vehicle";
            this.btnAddVehicle.UseVisualStyleBackColor = true;
            this.btnAddVehicle.Click += new System.EventHandler(this.btnAddVehicle_Click);
            // 
            // btnDetails
            // 
            this.btnDetails.Location = new System.Drawing.Point(452, 325);
            this.btnDetails.Name = "btnDetails";
            this.btnDetails.Size = new System.Drawing.Size(120, 35);
            this.btnDetails.TabIndex = 2;
            this.btnDetails.Text = "Details";
            this.btnDetails.UseVisualStyleBackColor = true;
            this.btnDetails.Click += new System.EventHandler(this.btnDetails_Click);
            // 
            // VehicleListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 372);
            this.Controls.Add(this.btnDetails);
            this.Controls.Add(this.btnAddVehicle);
            this.Controls.Add(this.vehicleGrid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "VehicleListForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Vehicles";
            ((System.ComponentModel.ISupportInitialize)(this.vehicleGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView vehicleGrid;
        private System.Windows.Forms.Button btnAddVehicle;
        private System.Windows.Forms.Button btnDetails;
    }
}
