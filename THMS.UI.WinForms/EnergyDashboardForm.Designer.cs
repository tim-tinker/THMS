namespace THMS.UI.WinForms
{
    partial class EnergyDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.ListBox energySourceListBox;
        private System.Windows.Forms.GroupBox energyDetailsGroup;
        private System.Windows.Forms.Label lblSourceName;
        private System.Windows.Forms.Label lblMonthlyKwh;
        private System.Windows.Forms.Label lblCostPerKwh;
        private System.Windows.Forms.Label lblMonthlyCost;
        private System.Windows.Forms.DataVisualization.Charting.Chart energyChart;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.energySourceListBox = new System.Windows.Forms.ListBox();
            this.energyDetailsGroup = new System.Windows.Forms.GroupBox();
            this.lblSourceName = new System.Windows.Forms.Label();
            this.lblMonthlyKwh = new System.Windows.Forms.Label();
            this.lblCostPerKwh = new System.Windows.Forms.Label();
            this.lblMonthlyCost = new System.Windows.Forms.Label();
            this.energyChart = new System.Windows.Forms.DataVisualization.Charting.Chart();

            ((System.ComponentModel.ISupportInitialize)(this.energyChart)).BeginInit();
            this.mainLayout.SuspendLayout();
            this.energyDetailsGroup.SuspendLayout();
            this.SuspendLayout();

            // mainLayout
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Controls.Add(this.energySourceListBox, 0, 0);
            this.mainLayout.SetRowSpan(this.energySourceListBox, 2);
            this.mainLayout.Controls.Add(this.energyDetailsGroup, 1, 0);
            this.mainLayout.Controls.Add(this.energyChart, 1, 1);

            // energySourceListBox
            this.energySourceListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.energySourceListBox.SelectedIndexChanged += EnergySourceListBox_SelectedIndexChanged;

            // energyDetailsGroup
            this.energyDetailsGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.energyDetailsGroup.Text = "Energy Details";
            this.energyDetailsGroup.Controls.Add(this.lblSourceName);
            this.energyDetailsGroup.Controls.Add(this.lblMonthlyKwh);
            this.energyDetailsGroup.Controls.Add(this.lblCostPerKwh);
            this.energyDetailsGroup.Controls.Add(this.lblMonthlyCost);

            // lblSourceName
            this.lblSourceName.AutoSize = true;
            this.lblSourceName.Location = new System.Drawing.Point(16, 28);
            this.lblSourceName.Text = "Source:";

            // lblMonthlyKwh
            this.lblMonthlyKwh.AutoSize = true;
            this.lblMonthlyKwh.Location = new System.Drawing.Point(16, 52);
            this.lblMonthlyKwh.Text = "Monthly kWh:";

            // lblCostPerKwh
            this.lblCostPerKwh.AutoSize = true;
            this.lblCostPerKwh.Location = new System.Drawing.Point(16, 80);
            this.lblCostPerKwh.Text = "Cost per kWh:";

            // lblMonthlyCost
            this.lblMonthlyCost.AutoSize = true;
            this.lblMonthlyCost.Location = new System.Drawing.Point(16, 104);
            this.lblMonthlyCost.Text = "Monthly Cost:";

            // energyChart
            this.energyChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.energyChart.ChartAreas.Add(new System.Windows.Forms.DataVisualization.Charting.ChartArea("Default"));
            var series = new System.Windows.Forms.DataVisualization.Charting.Series("MonthlyEnergyCost");
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            series.ChartArea = "Default";
            this.energyChart.Series.Add(series);

            // EnergyDashboardForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mainLayout);
            this.Name = "EnergyDashboardForm";
            this.Text = "Energy Dashboard";

            this.mainLayout.ResumeLayout(false);
            this.energyDetailsGroup.ResumeLayout(false);
            this.energyDetailsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.energyChart)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
