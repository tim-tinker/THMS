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
            vehicleGrid = new DataGridView();
            btnAddVehicle = new Button();
            btnDetails = new Button();
            historyBar = new Controls.HistoryPeriodBar();
            ((System.ComponentModel.ISupportInitialize)vehicleGrid).BeginInit();
            SuspendLayout();
            // 
            // historyBar
            // 
            historyBar.Location = new Point(18, 8);
            historyBar.Name = "historyBar";
            historyBar.Padding = new Padding(0, 4, 0, 4);
            historyBar.TabIndex = 0;
            // 
            // vehicleGrid
            // 
            vehicleGrid.AllowUserToAddRows = false;
            vehicleGrid.AllowUserToDeleteRows = false;
            vehicleGrid.AllowUserToResizeRows = false;
            vehicleGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            vehicleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            vehicleGrid.Location = new Point(18, 56);
            vehicleGrid.Margin = new Padding(4, 4, 4, 4);
            vehicleGrid.MultiSelect = false;
            vehicleGrid.Name = "vehicleGrid";
            vehicleGrid.ReadOnly = true;
            vehicleGrid.RowHeadersVisible = false;
            vehicleGrid.RowHeadersWidth = 72;
            vehicleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            vehicleGrid.Size = new Size(944, 506);
            vehicleGrid.TabIndex = 1;
            // 
            // btnAddVehicle
            // 
            btnAddVehicle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAddVehicle.Location = new Point(18, 582);
            btnAddVehicle.Margin = new Padding(4, 4, 4, 4);
            btnAddVehicle.Name = "btnAddVehicle";
            btnAddVehicle.Size = new Size(180, 52);
            btnAddVehicle.TabIndex = 2;
            btnAddVehicle.Text = "Add Vehicle";
            btnAddVehicle.UseVisualStyleBackColor = true;
            btnAddVehicle.Click += btnAddVehicle_Click;
            // 
            // btnDetails
            // 
            btnDetails.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDetails.Location = new Point(782, 582);
            btnDetails.Margin = new Padding(4, 4, 4, 4);
            btnDetails.Name = "btnDetails";
            btnDetails.Size = new Size(180, 52);
            btnDetails.TabIndex = 3;
            btnDetails.Text = "Details";
            btnDetails.UseVisualStyleBackColor = true;
            btnDetails.Click += btnDetails_Click;
            // 
            // VehicleListDashboardForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 652);
            Controls.Add(btnDetails);
            Controls.Add(btnAddVehicle);
            Controls.Add(vehicleGrid);
            Controls.Add(historyBar);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 4, 4, 4);
            Name = "VehicleListDashboardForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Vehicles";
            ((System.ComponentModel.ISupportInitialize)vehicleGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView vehicleGrid;
        private System.Windows.Forms.Button btnAddVehicle;
        private System.Windows.Forms.Button btnDetails;
        private Controls.HistoryPeriodBar historyBar;
    }
}
