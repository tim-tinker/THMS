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
            _labelBattery = new Label();
            _labelNet = new Label();
            _labelEvCharging = new Label();
            _tabs = new TabControl();
            _tabDay = new TabPage();
            _panelDayBreakdown = new Panel();
            _panelDayBatterySoc = new Panel();
            _panelDayEnergyFlow = new Panel();
            _tabWeek = new TabPage();
            _tabMonth = new TabPage();
            _tabYear = new TabPage();
            _tabCustom = new TabPage();
            _panelCustomBreakdown = new Panel();
            _panelCustomChart = new Panel();
            _panelCustomRange = new Panel();
            _panelPeriodChart = new Panel();
            _panelPeriodBreakdown = new Panel();
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
            _tableSummary.ColumnCount = 8;
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.20339F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.7118645F));
            _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            _tableSummary.Controls.Add(_labelProduced, 0, 0);
            _tableSummary.Controls.Add(_labelConsumed, 1, 0);
            _tableSummary.Controls.Add(_labelImported, 2, 0);
            _tableSummary.Controls.Add(_labelExported, 3, 0);
            _tableSummary.Controls.Add(_labelBattery, 4, 0);
            _tableSummary.Controls.Add(_labelNet, 5, 0);
            _tableSummary.Controls.Add(_labelEvCharging, 6, 0);
            _tableSummary.Dock = DockStyle.Fill;
            _tableSummary.Location = new Point(10, 10);
            _tableSummary.Name = "_tableSummary";
            _tableSummary.RowCount = 1;
            _tableSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _tableSummary.Size = new Size(1626, 50);
            _tableSummary.TabIndex = 0;
            // 
            // _labelProduced
            // 
            _labelProduced.Dock = DockStyle.Fill;
            _labelProduced.Location = new Point(3, 0);
            _labelProduced.Name = "_labelProduced";
            _labelProduced.Size = new Size(197, 50);
            _labelProduced.TabIndex = 0;
            _labelProduced.Text = "Produced: 0 kWh";
            _labelProduced.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelConsumed
            // 
            _labelConsumed.Dock = DockStyle.Fill;
            _labelConsumed.Location = new Point(206, 0);
            _labelConsumed.Name = "_labelConsumed";
            _labelConsumed.Size = new Size(197, 50);
            _labelConsumed.TabIndex = 1;
            _labelConsumed.Text = "Consumed: 0 kWh";
            _labelConsumed.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelImported
            // 
            _labelImported.Dock = DockStyle.Fill;
            _labelImported.Location = new Point(409, 0);
            _labelImported.Name = "_labelImported";
            _labelImported.Size = new Size(197, 50);
            _labelImported.TabIndex = 2;
            _labelImported.Text = "Imported: 0 kWh";
            _labelImported.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelExported
            // 
            _labelExported.Dock = DockStyle.Fill;
            _labelExported.Location = new Point(612, 0);
            _labelExported.Name = "_labelExported";
            _labelExported.Size = new Size(197, 50);
            _labelExported.TabIndex = 3;
            _labelExported.Text = "Exported: 0 kWh";
            _labelExported.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelBattery
            // 
            _labelBattery.Dock = DockStyle.Fill;
            _labelBattery.Location = new Point(815, 0);
            _labelBattery.Name = "_labelBattery";
            _labelBattery.Size = new Size(197, 50);
            _labelBattery.TabIndex = 4;
            _labelBattery.Text = "Battery: 0 kWh";
            _labelBattery.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelNet
            // 
            _labelNet.Dock = DockStyle.Fill;
            _labelNet.Location = new Point(1018, 0);
            _labelNet.Name = "_labelNet";
            _labelNet.Size = new Size(192, 50);
            _labelNet.TabIndex = 5;
            _labelNet.Text = "Net: 0 kWh";
            _labelNet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _labelEvCharging
            // 
            _labelEvCharging.Dock = DockStyle.Fill;
            _labelEvCharging.Location = new Point(1216, 0);
            _labelEvCharging.Name = "_labelEvCharging";
            _labelEvCharging.Size = new Size(200, 50);
            _labelEvCharging.TabIndex = 6;
            _labelEvCharging.Text = "EV Charging: 0 kWh";
            _labelEvCharging.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _tabs
            // 
            _tabs.Controls.Add(_tabDay);
            _tabs.Controls.Add(_tabWeek);
            _tabs.Controls.Add(_tabMonth);
            _tabs.Controls.Add(_tabYear);
            _tabs.Controls.Add(_tabCustom);
            _tabs.Dock = DockStyle.Fill;
            _tabs.Location = new Point(0, 70);
            _tabs.Name = "_tabs";
            _tabs.SelectedIndex = 0;
            _tabs.Size = new Size(1646, 830);
            _tabs.TabIndex = 0;
            // 
            // _tabDay
            // 
            _tabDay.Controls.Add(_panelDayBreakdown);
            _tabDay.Controls.Add(_panelDayBatterySoc);
            _tabDay.Controls.Add(_panelDayEnergyFlow);
            _tabDay.Location = new Point(4, 39);
            _tabDay.Name = "_tabDay";
            _tabDay.Padding = new Padding(10);
            _tabDay.Size = new Size(1638, 787);
            _tabDay.TabIndex = 0;
            _tabDay.Text = "Day";
            // 
            // _panelDayBreakdown
            // 
            _panelDayBreakdown.BorderStyle = BorderStyle.FixedSingle;
            _panelDayBreakdown.Dock = DockStyle.Fill;
            _panelDayBreakdown.Location = new Point(10, 410);
            _panelDayBreakdown.Name = "_panelDayBreakdown";
            _panelDayBreakdown.Size = new Size(1618, 367);
            _panelDayBreakdown.TabIndex = 0;
            // 
            // _panelDayBatterySoc
            // 
            _panelDayBatterySoc.BorderStyle = BorderStyle.FixedSingle;
            _panelDayBatterySoc.Dock = DockStyle.Top;
            _panelDayBatterySoc.Location = new Point(10, 260);
            _panelDayBatterySoc.Name = "_panelDayBatterySoc";
            _panelDayBatterySoc.Size = new Size(1618, 150);
            _panelDayBatterySoc.TabIndex = 1;
            // 
            // _panelDayEnergyFlow
            // 
            _panelDayEnergyFlow.BorderStyle = BorderStyle.FixedSingle;
            _panelDayEnergyFlow.Dock = DockStyle.Top;
            _panelDayEnergyFlow.Location = new Point(10, 10);
            _panelDayEnergyFlow.Name = "_panelDayEnergyFlow";
            _panelDayEnergyFlow.Size = new Size(1618, 250);
            _panelDayEnergyFlow.TabIndex = 2;
            // 
            // _tabWeek
            // 
            _tabWeek.Location = new Point(4, 39);
            _tabWeek.Name = "_tabWeek";
            _tabWeek.Size = new Size(1192, 787);
            _tabWeek.TabIndex = 1;
            _tabWeek.Text = "Week";
            // 
            // _tabMonth
            // 
            _tabMonth.Location = new Point(4, 39);
            _tabMonth.Name = "_tabMonth";
            _tabMonth.Size = new Size(1192, 787);
            _tabMonth.TabIndex = 2;
            _tabMonth.Text = "Month";
            // 
            // _tabYear
            // 
            _tabYear.Location = new Point(4, 39);
            _tabYear.Name = "_tabYear";
            _tabYear.Size = new Size(1192, 787);
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
            _tabCustom.Size = new Size(1192, 787);
            _tabCustom.TabIndex = 4;
            _tabCustom.Text = "Custom";
            // 
            // _panelCustomBreakdown
            // 
            _panelCustomBreakdown.BorderStyle = BorderStyle.FixedSingle;
            _panelCustomBreakdown.Dock = DockStyle.Fill;
            _panelCustomBreakdown.Location = new Point(10, 370);
            _panelCustomBreakdown.Name = "_panelCustomBreakdown";
            _panelCustomBreakdown.Size = new Size(1172, 407);
            _panelCustomBreakdown.TabIndex = 0;
            // 
            // _panelCustomChart
            // 
            _panelCustomChart.BorderStyle = BorderStyle.FixedSingle;
            _panelCustomChart.Dock = DockStyle.Top;
            _panelCustomChart.Location = new Point(10, 70);
            _panelCustomChart.Name = "_panelCustomChart";
            _panelCustomChart.Size = new Size(1172, 300);
            _panelCustomChart.TabIndex = 1;
            // 
            // _panelCustomRange
            // 
            _panelCustomRange.BorderStyle = BorderStyle.FixedSingle;
            _panelCustomRange.Dock = DockStyle.Top;
            _panelCustomRange.Location = new Point(10, 10);
            _panelCustomRange.Name = "_panelCustomRange";
            _panelCustomRange.Size = new Size(1172, 60);
            _panelCustomRange.TabIndex = 2;
            // 
            // _panelPeriodChart
            // 
            _panelPeriodChart.Location = new Point(0, 0);
            _panelPeriodChart.Name = "_panelPeriodChart";
            _panelPeriodChart.Size = new Size(200, 100);
            _panelPeriodChart.TabIndex = 0;
            // 
            // _panelPeriodBreakdown
            // 
            _panelPeriodBreakdown.Location = new Point(0, 0);
            _panelPeriodBreakdown.Name = "_panelPeriodBreakdown";
            _panelPeriodBreakdown.Size = new Size(200, 100);
            _panelPeriodBreakdown.TabIndex = 0;
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
        private Label _labelBattery;
        private Label _labelNet;
        private Label _labelEvCharging;

        private TabControl _tabs;
        private TabPage _tabDay;
        private TabPage _tabWeek;
        private TabPage _tabMonth;
        private TabPage _tabYear;
        private TabPage _tabCustom;

        private Panel _panelDayEnergyFlow;
        private Panel _panelDayBatterySoc;
        private Panel _panelDayBreakdown;

        private Panel _panelPeriodChart;
        private Panel _panelPeriodBreakdown;

        private Panel _panelCustomRange;
        private Panel _panelCustomChart;
        private Panel _panelCustomBreakdown;
    }
}
