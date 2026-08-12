namespace THMS.UI.WinForms
{
    partial class EnergyDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            _panelSummary = new Panel();
            _tableSummary = new TableLayoutPanel();
            _labelProduced = new Label();
            _labelConsumed = new Label();
            _labelImported = new Label();
            _labelExported = new Label();
            _labelNetGrid = new Label();
            _labelGridDependence = new Label();
            _labelNetGridDependence = new Label();
            _labelBatteryCharge = new Label();
            _labelBatteryDischarge = new Label();
            _labelEvCharge = new Label();
            _labelEvConsumption = new Label();
            _tabs = new TabControl();
            _tabDay = new TabPage();
            _tabWeek = new TabPage();
            _tabMonth = new TabPage();
            _tabYear = new TabPage();
            _tabCustom = new TabPage();
            _panelCustomBreakdown = new Panel();
            _panelCustomChart = new Panel();
            _panelCustomRange = new Panel();
            _panelPeriodChart = new Panel();
            _panelPeriodMetrics = new Panel();
            _panelSummary.SuspendLayout();
            _tableSummary.SuspendLayout();
            _tabs.SuspendLayout();
            _tabDay.SuspendLayout();
            _tabCustom.SuspendLayout();
            SuspendLayout();
            // 
            // _panelSummary
            // 
            _panelSummary.Controls.Add(_tableSummary);
            _panelSummary.Dock = DockStyle.Top;
            _panelSummary.Location = new Point(0, 0);
            _panelSummary.Name = "_panelSummary";
            _panelSummary.Padding = new Padding(10);
            _panelSummary.Size = new Size(1646, 88);
            _panelSummary.TabIndex = 1;
            // 
            // _tableSummary
            // 
            _tableSummary.ColumnCount = 11;
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09091F));
            _tableSummary.Controls.Add(_labelProduced, 0, 0);
            _tableSummary.Controls.Add(_labelConsumed, 1, 0);
            _tableSummary.Controls.Add(_labelImported, 2, 0);
            _tableSummary.Controls.Add(_labelExported, 3, 0);
            _tableSummary.Controls.Add(_labelNetGrid, 4, 0);
            _tableSummary.Controls.Add(_labelGridDependence, 5, 0);
            _tableSummary.Controls.Add(_labelNetGridDependence, 6, 0);
            _tableSummary.Controls.Add(_labelBatteryCharge, 7, 0);
            _tableSummary.Controls.Add(_labelBatteryDischarge, 8, 0);
            _tableSummary.Controls.Add(_labelEvCharge, 9, 0);
            _tableSummary.Controls.Add(_labelEvConsumption, 10, 0);
            _tableSummary.Dock = DockStyle.Fill;
            _tableSummary.Location = new Point(10, 10);
            _tableSummary.Name = "_tableSummary";
            _tableSummary.RowCount = 1;
            _tableSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _tableSummary.Size = new Size(1626, 68);
            _tableSummary.TabIndex = 0;
            // 
            // _labelProduced
            // 
            _labelProduced.Dock = DockStyle.Fill;
            _labelProduced.Location = new Point(3, 0);
            _labelProduced.Name = "_labelProduced";
            _labelProduced.Size = new Size(141, 68);
            _labelProduced.TabIndex = 0;
            _labelProduced.Text = "Produced";
            _labelProduced.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelConsumed
            // 
            _labelConsumed.Dock = DockStyle.Fill;
            _labelConsumed.Location = new Point(150, 0);
            _labelConsumed.Name = "_labelConsumed";
            _labelConsumed.Size = new Size(141, 68);
            _labelConsumed.TabIndex = 1;
            _labelConsumed.Text = "Consumed";
            _labelConsumed.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelImported
            // 
            _labelImported.Dock = DockStyle.Fill;
            _labelImported.Location = new Point(297, 0);
            _labelImported.Name = "_labelImported";
            _labelImported.Size = new Size(141, 68);
            _labelImported.TabIndex = 2;
            _labelImported.Text = "Imported";
            _labelImported.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelExported
            // 
            _labelExported.Dock = DockStyle.Fill;
            _labelExported.Location = new Point(444, 0);
            _labelExported.Name = "_labelExported";
            _labelExported.Size = new Size(141, 68);
            _labelExported.TabIndex = 3;
            _labelExported.Text = "Exported";
            _labelExported.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelNetGrid
            // 
            _labelNetGrid.Dock = DockStyle.Fill;
            _labelNetGrid.Location = new Point(591, 0);
            _labelNetGrid.Name = "_labelNetGrid";
            _labelNetGrid.Size = new Size(141, 68);
            _labelNetGrid.TabIndex = 5;
            _labelNetGrid.Text = "Net Grid";
            _labelNetGrid.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelGridDependence
            // 
            _labelGridDependence.Dock = DockStyle.Fill;
            _labelGridDependence.Location = new Point(738, 0);
            _labelGridDependence.Name = "_labelGridDependence";
            _labelGridDependence.Size = new Size(141, 68);
            _labelGridDependence.TabIndex = 3;
            _labelGridDependence.Text = "Grid Dependence";
            _labelGridDependence.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelNetGridDependence
            // 
            _labelNetGridDependence.Dock = DockStyle.Fill;
            _labelNetGridDependence.Location = new Point(885, 0);
            _labelNetGridDependence.Name = "_labelNetGridDependence";
            _labelNetGridDependence.Size = new Size(141, 68);
            _labelNetGridDependence.TabIndex = 5;
            _labelNetGridDependence.Text = "Net Grid Dependence";
            _labelNetGridDependence.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelBatteryCharge
            // 
            _labelBatteryCharge.Dock = DockStyle.Fill;
            _labelBatteryCharge.Location = new Point(1032, 0);
            _labelBatteryCharge.Name = "_labelBatteryCharge";
            _labelBatteryCharge.Size = new Size(141, 68);
            _labelBatteryCharge.TabIndex = 4;
            _labelBatteryCharge.Text = "Battery +";
            _labelBatteryCharge.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelBatteryDischarge
            // 
            _labelBatteryDischarge.Dock = DockStyle.Fill;
            _labelBatteryDischarge.Location = new Point(1179, 0);
            _labelBatteryDischarge.Name = "_labelBatteryDischarge";
            _labelBatteryDischarge.Size = new Size(141, 68);
            _labelBatteryDischarge.TabIndex = 4;
            _labelBatteryDischarge.Text = "Battery -";
            _labelBatteryDischarge.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelEvCharge
            // 
            _labelEvCharge.Dock = DockStyle.Fill;
            _labelEvCharge.Location = new Point(1326, 0);
            _labelEvCharge.Name = "_labelEvCharge";
            _labelEvCharge.Size = new Size(141, 68);
            _labelEvCharge.TabIndex = 6;
            _labelEvCharge.Text = "EV Charge";
            _labelEvCharge.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelEvConsumption
            // 
            _labelEvConsumption.Dock = DockStyle.Fill;
            _labelEvConsumption.Location = new Point(1473, 0);
            _labelEvConsumption.Name = "_labelEvConsumption";
            _labelEvConsumption.Size = new Size(150, 68);
            _labelEvConsumption.TabIndex = 6;
            _labelEvConsumption.Text = "EV Consumption";
            _labelEvConsumption.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _tabs
            // 
            _tabs.Controls.Add(_tabDay);
            _tabs.Controls.Add(_tabWeek);
            _tabs.Controls.Add(_tabMonth);
            _tabs.Controls.Add(_tabYear);
            _tabs.Controls.Add(_tabCustom);
            _tabs.Dock = DockStyle.Fill;
            _tabs.Location = new Point(0, 88);
            _tabs.Name = "_tabs";
            _tabs.SelectedIndex = 0;
            _tabs.Size = new Size(1646, 812);
            _tabs.TabIndex = 0;
            _tabs.SelectedIndexChanged += OnSelectedIndexChangedTabControl;
            // 
            // _tabDay
            // 
            _tabDay.Location = new Point(4, 39);
            _tabDay.Name = "_tabDay";
            _tabDay.Padding = new Padding(10);
            _tabDay.Size = new Size(1638, 769);
            _tabDay.TabIndex = 0;
            _tabDay.Text = "Day";
            // 
            // _tabWeek
            // 
            _tabWeek.Location = new Point(4, 39);
            _tabWeek.Name = "_tabWeek";
            _tabWeek.Size = new Size(1638, 769);
            _tabWeek.TabIndex = 1;
            _tabWeek.Text = "Week";
            // 
            // _tabMonth
            // 
            _tabMonth.Location = new Point(4, 39);
            _tabMonth.Name = "_tabMonth";
            _tabMonth.Size = new Size(1638, 769);
            _tabMonth.TabIndex = 2;
            _tabMonth.Text = "Month";
            // 
            // _tabYear
            // 
            _tabYear.Location = new Point(4, 39);
            _tabYear.Name = "_tabYear";
            _tabYear.Size = new Size(1638, 769);
            _tabYear.TabIndex = 3;
            _tabYear.Text = "Year";
            // 
            // _tabCustom
            // 
            _tabCustom.Controls.Add(_panelCustomBreakdown);
            _tabCustom.Controls.Add(_panelCustomChart);
            _tabCustom.Controls.Add(_panelCustomRange);
            _tabCustom.Location = new Point(4, 39);
            _tabCustom.Name = "_tabCustom";
            _tabCustom.Padding = new Padding(10);
            _tabCustom.Size = new Size(1638, 769);
            _tabCustom.TabIndex = 4;
            _tabCustom.Text = "Custom";
            // 
            // _panelCustomBreakdown
            // 
            _panelCustomBreakdown.BorderStyle = BorderStyle.FixedSingle;
            _panelCustomBreakdown.Dock = DockStyle.Fill;
            _panelCustomBreakdown.Location = new Point(10, 370);
            _panelCustomBreakdown.Name = "_panelCustomBreakdown";
            _panelCustomBreakdown.Size = new Size(1618, 389);
            _panelCustomBreakdown.TabIndex = 0;
            // 
            // _panelCustomChart
            // 
            _panelCustomChart.BorderStyle = BorderStyle.FixedSingle;
            _panelCustomChart.Dock = DockStyle.Top;
            _panelCustomChart.Location = new Point(10, 70);
            _panelCustomChart.Name = "_panelCustomChart";
            _panelCustomChart.Size = new Size(1618, 300);
            _panelCustomChart.TabIndex = 1;
            // 
            // _panelCustomRange
            // 
            _panelCustomRange.BorderStyle = BorderStyle.FixedSingle;
            _panelCustomRange.Dock = DockStyle.Top;
            _panelCustomRange.Location = new Point(10, 10);
            _panelCustomRange.Name = "_panelCustomRange";
            _panelCustomRange.Size = new Size(1618, 60);
            _panelCustomRange.TabIndex = 2;
            // 
            // _panelPeriodChart
            // 
            _panelPeriodChart.Location = new Point(0, 0);
            _panelPeriodChart.Name = "_panelPeriodChart";
            _panelPeriodChart.Size = new Size(200, 100);
            _panelPeriodChart.TabIndex = 0;
            // 
            // _panelPeriodMetrics
            // 
            _panelPeriodMetrics.Location = new Point(0, 0);
            _panelPeriodMetrics.Name = "_panelPeriodMetrics";
            _panelPeriodMetrics.Size = new Size(200, 100);
            _panelPeriodMetrics.TabIndex = 0;
            // 
            // EnergyDashboardForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1646, 900);
            Controls.Add(_tabs);
            Controls.Add(_panelSummary);
            Name = "EnergyDashboardForm";
            Text = "Energy Dashboard";
            Load += OnLoadForm;
            _panelSummary.ResumeLayout(false);
            _tableSummary.ResumeLayout(false);
            _tabs.ResumeLayout(false);
            _tabDay.ResumeLayout(false);
            _tabCustom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel _panelSummary;
        private TableLayoutPanel _tableSummary;
        private Label _labelProduced;
        private Label _labelConsumed;
        private Label _labelImported;
        private Label _labelExported;
        private Label _labelNetGrid;
        private Label _labelEvCharge;
        private Label _labelGridDependence;
        private Label _labelNetGridDependence;
        private Label _labelBatteryCharge;
        private Label _labelBatteryDischarge;
        private Label _labelEvConsumption;

        private TabControl _tabs;
        private TabPage _tabDay;
        private TabPage _tabWeek;
        private TabPage _tabMonth;
        private TabPage _tabYear;
        private TabPage _tabCustom;

        private Panel _panelPeriodChart;
        private Panel _panelPeriodMetrics;

        private Panel _panelCustomRange;
        private Panel _panelCustomChart;
        private Panel _panelCustomBreakdown;
    }
}
