using THMS.UI.WinForms.Controls;
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
            splitVehicles = new SplitContainer();
            vehicleGrid = new DataGridView();
            splitMonthly = new SplitContainer();
            monthlyGrid = new DataGridView();
            contributionGrid = new DataGridView();
            btnAddVehicle = new ThmsButton();
            btnDetails = new ThmsButton();
            ((System.ComponentModel.ISupportInitialize)splitVehicles).BeginInit();
            splitVehicles.Panel1.SuspendLayout();
            splitVehicles.Panel2.SuspendLayout();
            splitVehicles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)vehicleGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitMonthly).BeginInit();
            splitMonthly.Panel1.SuspendLayout();
            splitMonthly.Panel2.SuspendLayout();
            splitMonthly.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)monthlyGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)contributionGrid).BeginInit();
            SuspendLayout();
            // 
            // splitVehicles
            // 
            splitVehicles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitVehicles.Location = new Point(18, 12);
            splitVehicles.Margin = new Padding(0);
            splitVehicles.Name = "splitVehicles";
            splitVehicles.Orientation = Orientation.Horizontal;
            splitVehicles.Panel1.Controls.Add(vehicleGrid);
            splitVehicles.Panel2.Controls.Add(splitMonthly);
            splitVehicles.Size = new Size(944, 554);
            splitVehicles.TabIndex = 0;
            // 
            // vehicleGrid
            // 
            vehicleGrid.AllowUserToAddRows = false;
            vehicleGrid.AllowUserToDeleteRows = false;
            vehicleGrid.AllowUserToResizeRows = false;
            vehicleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            vehicleGrid.Dock = DockStyle.Fill;
            vehicleGrid.Location = new Point(0, 0);
            vehicleGrid.Margin = new Padding(4);
            vehicleGrid.MultiSelect = false;
            vehicleGrid.Name = "vehicleGrid";
            vehicleGrid.ReadOnly = true;
            vehicleGrid.RowHeadersVisible = false;
            vehicleGrid.RowHeadersWidth = 72;
            vehicleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            vehicleGrid.Size = new Size(944, 160);
            vehicleGrid.TabIndex = 0;
            // 
            // splitMonthly
            // 
            splitMonthly.Dock = DockStyle.Fill;
            splitMonthly.Location = new Point(0, 0);
            splitMonthly.Margin = new Padding(0);
            splitMonthly.Name = "splitMonthly";
            splitMonthly.Orientation = Orientation.Horizontal;
            splitMonthly.Panel1.Controls.Add(monthlyGrid);
            splitMonthly.Panel2.Controls.Add(contributionGrid);
            splitMonthly.Size = new Size(944, 380);
            splitMonthly.TabIndex = 0;
            // 
            // monthlyGrid
            // 
            monthlyGrid.AllowUserToAddRows = false;
            monthlyGrid.AllowUserToDeleteRows = false;
            monthlyGrid.AllowUserToResizeRows = false;
            monthlyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            monthlyGrid.Dock = DockStyle.Fill;
            monthlyGrid.Location = new Point(0, 0);
            monthlyGrid.Margin = new Padding(4);
            monthlyGrid.Name = "monthlyGrid";
            monthlyGrid.ReadOnly = true;
            monthlyGrid.RowHeadersVisible = false;
            monthlyGrid.RowHeadersWidth = 72;
            monthlyGrid.Size = new Size(944, 180);
            monthlyGrid.TabIndex = 0;
            // 
            // contributionGrid
            // 
            contributionGrid.AllowUserToAddRows = false;
            contributionGrid.AllowUserToDeleteRows = false;
            contributionGrid.AllowUserToResizeRows = false;
            contributionGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            contributionGrid.Dock = DockStyle.Fill;
            contributionGrid.Location = new Point(0, 0);
            contributionGrid.Margin = new Padding(4);
            contributionGrid.Name = "contributionGrid";
            contributionGrid.ReadOnly = true;
            contributionGrid.RowHeadersVisible = false;
            contributionGrid.RowHeadersWidth = 72;
            contributionGrid.Size = new Size(944, 180);
            contributionGrid.TabIndex = 0;
            // 
            // btnAddVehicle
            // 
            btnAddVehicle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAddVehicle.Location = new Point(18, 582);
            btnAddVehicle.Margin = new Padding(4);
            btnAddVehicle.Name = "btnAddVehicle";
            btnAddVehicle.Size = new Size(180, 52);
            btnAddVehicle.TabIndex = 1;
            btnAddVehicle.Text = "Add Vehicle";
            btnAddVehicle.UseVisualStyleBackColor = true;
            btnAddVehicle.Click += btnAddVehicle_Click;
            // 
            // btnDetails
            // 
            btnDetails.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDetails.Location = new Point(782, 582);
            btnDetails.Margin = new Padding(4);
            btnDetails.Name = "btnDetails";
            btnDetails.Size = new Size(180, 52);
            btnDetails.TabIndex = 2;
            btnDetails.Text = "Details";
            btnDetails.UseVisualStyleBackColor = true;
            btnDetails.Click += btnDetails_Click;
            // 
            // VehicleListDashboardForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 668);
            Controls.Add(btnDetails);
            Controls.Add(btnAddVehicle);
            Controls.Add(splitVehicles);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            Name = "VehicleListDashboardForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Vehicles";
            ((System.ComponentModel.ISupportInitialize)vehicleGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)monthlyGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)contributionGrid).EndInit();
            splitMonthly.Panel1.ResumeLayout(false);
            splitMonthly.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMonthly).EndInit();
            splitMonthly.ResumeLayout(false);
            splitVehicles.Panel1.ResumeLayout(false);
            splitVehicles.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitVehicles).EndInit();
            splitVehicles.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitVehicles;
        private SplitContainer splitMonthly;
        private System.Windows.Forms.DataGridView vehicleGrid;
        private System.Windows.Forms.DataGridView monthlyGrid;
        private System.Windows.Forms.DataGridView contributionGrid;
        private ThmsButton btnAddVehicle;
        private ThmsButton btnDetails;
    }
}
