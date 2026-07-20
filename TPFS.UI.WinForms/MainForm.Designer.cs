#nullable enable

using System.Windows.Forms.DataVisualization.Charting;

namespace TPFS.UI.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private SplitContainer splitContainer = null!;
    private ListBox navList = null!;
    private Panel contentPanel = null!;
    private Label appTitleLabel = null!;
    private ListBox vehicleListBox = null!;
    private Label lblVehicleName = null!;
    private Label lblAnnualCost = null!;
    private Label lblEnergyHome = null!;
    private Label lblEnergyPublic = null!;
    private Label lblEnergyRegen = null!;
    private Chart costChart = null!;
    private System.Windows.Forms.Button btnTransportation;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        splitContainer = new SplitContainer();
        appTitleLabel = new Label();
        navList = new ListBox();
        contentPanel = new Panel();
        vehicleListBox = new ListBox();
        lblVehicleName = new Label();
        lblAnnualCost = new Label();
        lblEnergyHome = new Label();
        lblEnergyPublic = new Label();
        lblEnergyRegen = new Label();
        btnTransportation = new System.Windows.Forms.Button();
        costChart = new Chart();
        var chartArea = new ChartArea("MainArea");
        var series = new Series("MonthlyCost");
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        ((System.ComponentModel.ISupportInitialize)costChart).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        contentPanel.SuspendLayout();
        SuspendLayout();
        //
        // splitContainer
        //
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.FixedPanel = FixedPanel.Panel1;
        splitContainer.Location = new Point(0, 0);
        splitContainer.Name = "splitContainer";
        splitContainer.Panel1.BackColor = Color.FromArgb(243, 243, 243);
        splitContainer.Panel1.Controls.Add(navList);
        splitContainer.Panel1.Controls.Add(appTitleLabel);
        splitContainer.Panel1MinSize = 180;
        splitContainer.Panel2.Controls.Add(contentPanel);
        splitContainer.Size = new Size(1100, 700);
        splitContainer.SplitterDistance = 220;
        splitContainer.TabIndex = 0;
        //
        // appTitleLabel
        //
        appTitleLabel.Dock = DockStyle.Top;
        appTitleLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        appTitleLabel.Location = new Point(0, 0);
        appTitleLabel.Name = "appTitleLabel";
        appTitleLabel.Padding = new Padding(12, 16, 12, 8);
        appTitleLabel.Size = new Size(220, 52);
        appTitleLabel.TabIndex = 0;
        appTitleLabel.Text = "TPFS";
        //
        // navList
        //
        navList.BorderStyle = BorderStyle.None;
        navList.Dock = DockStyle.Fill;
        navList.Font = new Font("Segoe UI", 11F);
        navList.FormattingEnabled = true;
        navList.ItemHeight = 28;
        navList.Location = new Point(0, 52);
        navList.Name = "navList";
        navList.IntegralHeight = false;
        navList.Size = new Size(220, 648);
        navList.TabIndex = 1;
        //
        // contentPanel
        //
        contentPanel.BackColor = SystemColors.Window;
        contentPanel.Controls.Add(costChart);
        contentPanel.Controls.Add(lblEnergyRegen);
        contentPanel.Controls.Add(lblEnergyPublic);
        contentPanel.Controls.Add(lblEnergyHome);
        contentPanel.Controls.Add(lblAnnualCost);
        contentPanel.Controls.Add(lblVehicleName);
        contentPanel.Controls.Add(vehicleListBox);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(0, 0);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(876, 700);
        contentPanel.TabIndex = 0;
        //
        // vehicleListBox
        //
        vehicleListBox.Font = new Font("Segoe UI", 11F);
        vehicleListBox.FormattingEnabled = true;
        vehicleListBox.ItemHeight = 24;
        vehicleListBox.Location = new Point(24, 24);
        vehicleListBox.Name = "vehicleListBox";
        vehicleListBox.Size = new Size(240, 220);
        vehicleListBox.TabIndex = 0;
        //
        // lblVehicleName
        //
        lblVehicleName.AutoSize = true;
        lblVehicleName.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        lblVehicleName.Location = new Point(290, 24);
        lblVehicleName.Name = "lblVehicleName";
        lblVehicleName.Size = new Size(120, 25);
        lblVehicleName.TabIndex = 1;
        lblVehicleName.Text = "Vehicle";
        //
        // lblAnnualCost
        //
        lblAnnualCost.AutoSize = true;
        lblAnnualCost.Font = new Font("Segoe UI", 11F);
        lblAnnualCost.Location = new Point(290, 64);
        lblAnnualCost.Name = "lblAnnualCost";
        lblAnnualCost.Size = new Size(100, 20);
        lblAnnualCost.TabIndex = 2;
        lblAnnualCost.Text = "Annual Cost:";
        //
        // lblEnergyHome
        //
        lblEnergyHome.AutoSize = true;
        lblEnergyHome.Font = new Font("Segoe UI", 11F);
        lblEnergyHome.Location = new Point(290, 100);
        lblEnergyHome.Name = "lblEnergyHome";
        lblEnergyHome.Size = new Size(120, 20);
        lblEnergyHome.TabIndex = 3;
        lblEnergyHome.Text = "Home Charging:";
        //
        // lblEnergyPublic
        //
        lblEnergyPublic.AutoSize = true;
        lblEnergyPublic.Font = new Font("Segoe UI", 11F);
        lblEnergyPublic.Location = new Point(290, 132);
        lblEnergyPublic.Name = "lblEnergyPublic";
        lblEnergyPublic.Size = new Size(120, 20);
        lblEnergyPublic.TabIndex = 4;
        lblEnergyPublic.Text = "Public Charging:";
        //
        // lblEnergyRegen
        //
        lblEnergyRegen.AutoSize = true;
        lblEnergyRegen.Font = new Font("Segoe UI", 11F);
        lblEnergyRegen.Location = new Point(290, 164);
        lblEnergyRegen.Name = "lblEnergyRegen";
        lblEnergyRegen.Size = new Size(60, 20);
        lblEnergyRegen.TabIndex = 5;
        lblEnergyRegen.Text = "Regen:";
        //
        // costChart
        //
        costChart.ChartAreas.Add(chartArea);
        series.ChartType = SeriesChartType.Column;
        costChart.Series.Add(series);
        costChart.Location = new Point(24, 270);
        costChart.Name = "costChart";
        costChart.Size = new Size(820, 380);
        costChart.TabIndex = 6;
        costChart.Text = "costChart";

        // contentPanel
        this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.contentPanel.Location = new System.Drawing.Point(0, 40);
        this.contentPanel.Name = "contentPanel";
        this.contentPanel.Size = new System.Drawing.Size(1200, 760);

        // btnTransportation
        this.btnTransportation.Dock = System.Windows.Forms.DockStyle.Top;
        this.btnTransportation.Height = 40;
        this.btnTransportation.Text = "Transportation Dashboard";
        this.btnTransportation.Click += new System.EventHandler(this.btnTransportation_Click);        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        Controls.Add(splitContainer);
        MinimumSize = new Size(800, 500);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Personal Finance System";
        contentPanel.ResumeLayout(false);
        contentPanel.PerformLayout();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)costChart).EndInit();
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        ResumeLayout(false);
    }
}
