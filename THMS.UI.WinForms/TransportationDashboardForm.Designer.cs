namespace THMS.UI.WinForms
{
    partial class TransportationDashboardForm
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
            this.vehicleListBox = new System.Windows.Forms.ListBox();
            this.lblVehicleName = new System.Windows.Forms.Label();
            this.lblLifetimeCostPerMile = new System.Windows.Forms.Label();
            this.lblMonthlyMiles = new System.Windows.Forms.Label();
            this.lblMonthlyCost = new System.Windows.Forms.Label();
            this.lblMonthlyCostPerMile = new System.Windows.Forms.Label();
            this.costChart = new System.Windows.Forms.DataVisualization.Charting.Chart();

            ((System.ComponentModel.ISupportInitialize)(this.costChart)).BeginInit();
            this.SuspendLayout();

            // vehicleListBox
            this.vehicleListBox.FormattingEnabled = true;
            this.vehicleListBox.ItemHeight = 20;
            this.vehicleListBox.Location = new System.Drawing.Point(12, 12);
            this.vehicleListBox.Name = "vehicleListBox";
            this.vehicleListBox.Size = new System.Drawing.Size(200, 424);
            this.vehicleListBox.TabIndex = 0;
            this.vehicleListBox.SelectedIndexChanged +=
                new System.EventHandler(this.VehicleListBox_SelectedIndexChanged);

            // lblVehicleName
            this.lblVehicleName.Location = new System.Drawing.Point(230, 20);
            this.lblVehicleName.Name = "lblVehicleName";
            this.lblVehicleName.Size = new System.Drawing.Size(300, 25);
            this.lblVehicleName.Text = "Vehicle:";

            // lblLifetimeCostPerMile
            this.lblLifetimeCostPerMile.Location = new System.Drawing.Point(230, 55);
            this.lblLifetimeCostPerMile.Name = "lblLifetimeCostPerMile";
            this.lblLifetimeCostPerMile.Size = new System.Drawing.Size(300, 25);
            this.lblLifetimeCostPerMile.Text = "Lifetime Cost/Mile:";

            // lblMonthlyMiles
            this.lblMonthlyMiles.Location = new System.Drawing.Point(230, 90);
            this.lblMonthlyMiles.Name = "lblMonthlyMiles";
            this.lblMonthlyMiles.Size = new System.Drawing.Size(300, 25);
            this.lblMonthlyMiles.Text = "Miles:";

            // lblMonthlyCost
            this.lblMonthlyCost.Location = new System.Drawing.Point(230, 125);
            this.lblMonthlyCost.Name = "lblMonthlyCost";
            this.lblMonthlyCost.Size = new System.Drawing.Size(300, 25);
            this.lblMonthlyCost.Text = "Cost:";

            // lblMonthlyCostPerMile
            this.lblMonthlyCostPerMile.Location = new System.Drawing.Point(230, 160);
            this.lblMonthlyCostPerMile.Name = "lblMonthlyCostPerMile";
            this.lblMonthlyCostPerMile.Size = new System.Drawing.Size(300, 25);
            this.lblMonthlyCostPerMile.Text = "Cost/Mile:";

            // costChart
            this.costChart.Location = new System.Drawing.Point(230, 200);
            this.costChart.Name = "costChart";
            this.costChart.Size = new System.Drawing.Size(540, 236);

            var chartArea = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            chartArea.Name = "ChartArea1";
            this.costChart.ChartAreas.Add(chartArea);

            var series = new System.Windows.Forms.DataVisualization.Charting.Series();
            series.Name = "MonthlyCost";
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            this.costChart.Series.Add(series);

            // TransportationDashboardForm
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.costChart);
            this.Controls.Add(this.lblMonthlyCostPerMile);
            this.Controls.Add(this.lblMonthlyCost);
            this.Controls.Add(this.lblMonthlyMiles);
            this.Controls.Add(this.lblLifetimeCostPerMile);
            this.Controls.Add(this.lblVehicleName);
            this.Controls.Add(this.vehicleListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "TransportationDashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Transportation Dashboard";

            ((System.ComponentModel.ISupportInitialize)(this.costChart)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ListBox vehicleListBox;
        private System.Windows.Forms.Label lblVehicleName;
        private System.Windows.Forms.Label lblLifetimeCostPerMile;
        private System.Windows.Forms.Label lblMonthlyMiles;
        private System.Windows.Forms.Label lblMonthlyCost;
        private System.Windows.Forms.Label lblMonthlyCostPerMile;
        private System.Windows.Forms.DataVisualization.Charting.Chart costChart;
    }
}
