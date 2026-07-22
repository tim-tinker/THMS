namespace THMS.UI.WinForms
{
    partial class TransportationDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.ListBox vehicleListBox;
        private System.Windows.Forms.GroupBox vehicleDetailsGroup;
        private System.Windows.Forms.Label lblVehicleName;
        private System.Windows.Forms.Label lblAnnualCost;
        private System.Windows.Forms.Label lblEnergyHome;
        private System.Windows.Forms.Label lblEnergyPublic;
        private System.Windows.Forms.Label lblEnergyRegen;
        private System.Windows.Forms.DataVisualization.Charting.Chart costChart;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            mainLayout = new TableLayoutPanel();
            vehicleListBox = new ListBox();
            vehicleDetailsGroup = new GroupBox();
            lblVehicleName = new Label();
            lblAnnualCost = new Label();
            lblEnergyHome = new Label();
            lblEnergyPublic = new Label();
            lblEnergyRegen = new Label();
            costChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            mainLayout.SuspendLayout();
            vehicleDetailsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)costChart).BeginInit();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 377F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(vehicleListBox, 0, 0);
            mainLayout.Controls.Add(vehicleDetailsGroup, 1, 0);
            mainLayout.Controls.Add(costChart, 1, 1);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Margin = new Padding(5, 6, 5, 6);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 2;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 320F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(1661, 983);
            mainLayout.TabIndex = 0;
            // 
            // vehicleListBox
            // 
            vehicleListBox.Dock = DockStyle.Fill;
            vehicleListBox.Location = new Point(5, 6);
            vehicleListBox.Margin = new Padding(5, 6, 5, 6);
            vehicleListBox.Name = "vehicleListBox";
            mainLayout.SetRowSpan(vehicleListBox, 2);
            vehicleListBox.Size = new Size(367, 971);
            vehicleListBox.TabIndex = 0;
            vehicleListBox.SelectedIndexChanged += VehicleListBox_SelectedIndexChanged;
            // 
            // vehicleDetailsGroup
            // 
            vehicleDetailsGroup.Controls.Add(lblVehicleName);
            vehicleDetailsGroup.Controls.Add(lblAnnualCost);
            vehicleDetailsGroup.Controls.Add(lblEnergyHome);
            vehicleDetailsGroup.Controls.Add(lblEnergyPublic);
            vehicleDetailsGroup.Controls.Add(lblEnergyRegen);
            vehicleDetailsGroup.Dock = DockStyle.Fill;
            vehicleDetailsGroup.Location = new Point(382, 6);
            vehicleDetailsGroup.Margin = new Padding(5, 6, 5, 6);
            vehicleDetailsGroup.Name = "vehicleDetailsGroup";
            vehicleDetailsGroup.Padding = new Padding(5, 6, 5, 6);
            vehicleDetailsGroup.Size = new Size(1274, 308);
            vehicleDetailsGroup.TabIndex = 1;
            vehicleDetailsGroup.TabStop = false;
            vehicleDetailsGroup.Text = "Vehicle Details";
            // 
            // lblVehicleName
            // 
            lblVehicleName.AutoSize = true;
            lblVehicleName.Location = new Point(27, 56);
            lblVehicleName.Margin = new Padding(5, 0, 5, 0);
            lblVehicleName.Name = "lblVehicleName";
            lblVehicleName.Size = new Size(84, 30);
            lblVehicleName.TabIndex = 0;
            lblVehicleName.Text = "Vehicle:";
            // 
            // lblAnnualCost
            // 
            lblAnnualCost.AutoSize = true;
            lblAnnualCost.Location = new Point(27, 104);
            lblAnnualCost.Margin = new Padding(5, 0, 5, 0);
            lblAnnualCost.Name = "lblAnnualCost";
            lblAnnualCost.Size = new Size(131, 30);
            lblAnnualCost.TabIndex = 1;
            lblAnnualCost.Text = "Annual Cost:";
            // 
            // lblEnergyHome
            // 
            lblEnergyHome.AutoSize = true;
            lblEnergyHome.Location = new Point(27, 160);
            lblEnergyHome.Margin = new Padding(5, 0, 5, 0);
            lblEnergyHome.Name = "lblEnergyHome";
            lblEnergyHome.Size = new Size(164, 30);
            lblEnergyHome.TabIndex = 2;
            lblEnergyHome.Text = "Home Charging:";
            // 
            // lblEnergyPublic
            // 
            lblEnergyPublic.AutoSize = true;
            lblEnergyPublic.Location = new Point(27, 208);
            lblEnergyPublic.Margin = new Padding(5, 0, 5, 0);
            lblEnergyPublic.Name = "lblEnergyPublic";
            lblEnergyPublic.Size = new Size(164, 30);
            lblEnergyPublic.TabIndex = 3;
            lblEnergyPublic.Text = "Public Charging:";
            // 
            // lblEnergyRegen
            // 
            lblEnergyRegen.AutoSize = true;
            lblEnergyRegen.Location = new Point(27, 256);
            lblEnergyRegen.Margin = new Padding(5, 0, 5, 0);
            lblEnergyRegen.Name = "lblEnergyRegen";
            lblEnergyRegen.Size = new Size(76, 30);
            lblEnergyRegen.TabIndex = 4;
            lblEnergyRegen.Text = "Regen:";
            // 
            // costChart
            // 
            chartArea1.Name = "Default";
            costChart.ChartAreas.Add(chartArea1);
            costChart.Dock = DockStyle.Fill;
            costChart.Location = new Point(382, 326);
            costChart.Margin = new Padding(5, 6, 5, 6);
            costChart.Name = "costChart";
            series1.ChartArea = "Default";
            series1.Name = "MonthlyCost";
            costChart.Series.Add(series1);
            costChart.Size = new Size(1274, 651);
            costChart.TabIndex = 2;
            // 
            // TransportationDashboardForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1661, 983);
            Controls.Add(mainLayout);
            Margin = new Padding(5, 6, 5, 6);
            Name = "TransportationDashboardForm";
            Text = "Transportation Dashboard";
            mainLayout.ResumeLayout(false);
            vehicleDetailsGroup.ResumeLayout(false);
            vehicleDetailsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)costChart).EndInit();
            ResumeLayout(false);
        }
    }
}
