namespace TPFS.UI.WinForms
{
    partial class TransportationDashboardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.GroupBox grpHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ComboBox cboHousehold;
        private System.Windows.Forms.Label lblHousehold;
        private System.Windows.Forms.ComboBox cboVehicle;
        private System.Windows.Forms.Label lblVehicle;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.Button btnRefresh;

        private System.Windows.Forms.GroupBox grpSummary;
        private System.Windows.Forms.Label lblMilesDriven;
        private System.Windows.Forms.Label lblMilesDrivenValue;
        private System.Windows.Forms.Label lblFuelCost;
        private System.Windows.Forms.Label lblFuelCostValue;
        private System.Windows.Forms.Label lblMaintenanceCost;
        private System.Windows.Forms.Label lblMaintenanceCostValue;
        private System.Windows.Forms.Label lblTotalCostPerMile;
        private System.Windows.Forms.Label lblTotalCostPerMileValue;

        private System.Windows.Forms.GroupBox grpTrips;
        private System.Windows.Forms.DataGridView dgvTrips;

        private System.Windows.Forms.GroupBox grpCharts;
        private System.Windows.Forms.TabControl tabCharts;
        private System.Windows.Forms.TabPage tabCostByMonth;
        private System.Windows.Forms.TabPage tabMilesByMonth;
        private System.Windows.Forms.Panel pnlCostByMonthChart;
        private System.Windows.Forms.Panel pnlMilesByMonthChart;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// true if managed resources should be disposed; otherwise, false.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.grpHeader = new System.Windows.Forms.GroupBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblHousehold = new System.Windows.Forms.Label();
            this.cboHousehold = new System.Windows.Forms.ComboBox();
            this.lblVehicle = new System.Windows.Forms.Label();
            this.cboVehicle = new System.Windows.Forms.ComboBox();
            this.lblFrom = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.btnRefresh = new System.Windows.Forms.Button();

            this.grpSummary = new System.Windows.Forms.GroupBox();
            this.lblMilesDriven = new System.Windows.Forms.Label();
            this.lblMilesDrivenValue = new System.Windows.Forms.Label();
            this.lblFuelCost = new System.Windows.Forms.Label();
            this.lblFuelCostValue = new System.Windows.Forms.Label();
            this.lblMaintenanceCost = new System.Windows.Forms.Label();
            this.lblMaintenanceCostValue = new System.Windows.Forms.Label();
            this.lblTotalCostPerMile = new System.Windows.Forms.Label();
            this.lblTotalCostPerMileValue = new System.Windows.Forms.Label();

            this.grpTrips = new System.Windows.Forms.GroupBox();
            this.dgvTrips = new System.Windows.Forms.DataGridView();

            this.grpCharts = new System.Windows.Forms.GroupBox();
            this.tabCharts = new System.Windows.Forms.TabControl();
            this.tabCostByMonth = new System.Windows.Forms.TabPage();
            this.pnlCostByMonthChart = new System.Windows.Forms.Panel();
            this.tabMilesByMonth = new System.Windows.Forms.TabPage();
            this.pnlMilesByMonthChart = new System.Windows.Forms.Panel();

            this.layoutRoot.SuspendLayout();
            this.grpHeader.SuspendLayout();
            this.grpSummary.SuspendLayout();
            this.grpTrips.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTrips)).BeginInit();
            this.grpCharts.SuspendLayout();
            this.tabCharts.SuspendLayout();
            this.tabCostByMonth.SuspendLayout();
            this.tabMilesByMonth.SuspendLayout();
            this.SuspendLayout();

            // 
            // TransportationDashboardForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "TPFS – Household Transportation Dashboard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(1100, 700);

            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.RowCount = 3;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Controls.Add(this.grpHeader, 0, 0);
            this.layoutRoot.Controls.Add(this.grpSummary, 0, 1);
            this.layoutRoot.Controls.Add(this.grpTrips, 0, 2);
            this.Controls.Add(this.layoutRoot);

            // 
            // grpHeader
            // 
            this.grpHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHeader.Text = "Filters";
            this.grpHeader.Padding = new System.Windows.Forms.Padding(10);

            // header layout (manual positioning)
            this.lblTitle.AutoSize = true;
            this.lblTitle.Text = "Household Transportation Dashboard";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(15, 20);

            this.lblHousehold.AutoSize = true;
            this.lblHousehold.Text = "Household:";
            this.lblHousehold.Location = new System.Drawing.Point(15, 55);

            this.cboHousehold.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHousehold.Location = new System.Drawing.Point(90, 52);
            this.cboHousehold.Width = 200;

            this.lblVehicle.AutoSize = true;
            this.lblVehicle.Text = "Vehicle:";
            this.lblVehicle.Location = new System.Drawing.Point(310, 55);

            this.cboVehicle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVehicle.Location = new System.Drawing.Point(365, 52);
            this.cboVehicle.Width = 200;

            this.lblFrom.AutoSize = true;
            this.lblFrom.Text = "From:";
            this.lblFrom.Location = new System.Drawing.Point(590, 55);

            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Location = new System.Drawing.Point(635, 52);
            this.dtpFrom.Width = 110;

            this.lblTo.AutoSize = true;
            this.lblTo.Text = "To:";
            this.lblTo.Location = new System.Drawing.Point(760, 55);

            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Location = new System.Drawing.Point(795, 52);
            this.dtpTo.Width = 110;

            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Location = new System.Drawing.Point(930, 50);
            this.btnRefresh.Width = 100;

            this.grpHeader.Controls.Add(this.lblTitle);
            this.grpHeader.Controls.Add(this.lblHousehold);
            this.grpHeader.Controls.Add(this.cboHousehold);
            this.grpHeader.Controls.Add(this.lblVehicle);
            this.grpHeader.Controls.Add(this.cboVehicle);
            this.grpHeader.Controls.Add(this.lblFrom);
            this.grpHeader.Controls.Add(this.dtpFrom);
            this.grpHeader.Controls.Add(this.lblTo);
            this.grpHeader.Controls.Add(this.dtpTo);
            this.grpHeader.Controls.Add(this.btnRefresh);

            // 
            // grpSummary
            // 
            this.grpSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSummary.Text = "Summary (selected vehicle & period)";
            this.grpSummary.Padding = new System.Windows.Forms.Padding(10);

            // summary labels
            int baseX = 15;
            int baseY = 30;
            int valueX = 220;
            int rowHeight = 25;

            this.lblMilesDriven.AutoSize = true;
            this.lblMilesDriven.Text = "Miles driven:";
            this.lblMilesDriven.Location = new System.Drawing.Point(baseX, baseY);

            this.lblMilesDrivenValue.AutoSize = true;
            this.lblMilesDrivenValue.Text = "0";
            this.lblMilesDrivenValue.Location = new System.Drawing.Point(valueX, baseY);

            this.lblFuelCost.AutoSize = true;
            this.lblFuelCost.Text = "Fuel cost:";
            this.lblFuelCost.Location = new System.Drawing.Point(baseX, baseY + rowHeight);

            this.lblFuelCostValue.AutoSize = true;
            this.lblFuelCostValue.Text = "$0.00";
            this.lblFuelCostValue.Location = new System.Drawing.Point(valueX, baseY + rowHeight);

            this.lblMaintenanceCost.AutoSize = true;
            this.lblMaintenanceCost.Text = "Maintenance cost:";
            this.lblMaintenanceCost.Location = new System.Drawing.Point(baseX, baseY + 2 * rowHeight);

            this.lblMaintenanceCostValue.AutoSize = true;
            this.lblMaintenanceCostValue.Text = "$0.00";
            this.lblMaintenanceCostValue.Location = new System.Drawing.Point(valueX, baseY + 2 * rowHeight);

            this.lblTotalCostPerMile.AutoSize = true;
            this.lblTotalCostPerMile.Text = "Total cost per mile:";
            this.lblTotalCostPerMile.Location = new System.Drawing.Point(baseX, baseY + 3 * rowHeight);

            this.lblTotalCostPerMileValue.AutoSize = true;
            this.lblTotalCostPerMileValue.Text = "$0.00";
            this.lblTotalCostPerMileValue.Location = new System.Drawing.Point(valueX, baseY + 3 * rowHeight);

            this.grpSummary.Controls.Add(this.lblMilesDriven);
            this.grpSummary.Controls.Add(this.lblMilesDrivenValue);
            this.grpSummary.Controls.Add(this.lblFuelCost);
            this.grpSummary.Controls.Add(this.lblFuelCostValue);
            this.grpSummary.Controls.Add(this.lblMaintenanceCost);
            this.grpSummary.Controls.Add(this.lblMaintenanceCostValue);
            this.grpSummary.Controls.Add(this.lblTotalCostPerMile);
            this.grpSummary.Controls.Add(this.lblTotalCostPerMileValue);

            // 
            // grpTrips
            // 
            this.grpTrips.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTrips.Text = "Trips and charts";
            this.grpTrips.Padding = new System.Windows.Forms.Padding(10);

            // split horizontally: left grid, right charts
            var tripsLayout = new System.Windows.Forms.TableLayoutPanel();
            tripsLayout.ColumnCount = 2;
            tripsLayout.RowCount = 1;
            tripsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            tripsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            tripsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));

            // 
            // dgvTrips
            // 
            this.dgvTrips.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTrips.AllowUserToAddRows = false;
            this.dgvTrips.AllowUserToDeleteRows = false;
            this.dgvTrips.ReadOnly = true;
            this.dgvTrips.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTrips.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;

            // basic columns (you’ll bind via ViewModel later)
            this.dgvTrips.Columns.Add("Date", "Date");
            this.dgvTrips.Columns.Add("Purpose", "Purpose");
            this.dgvTrips.Columns.Add("Miles", "Miles");
            this.dgvTrips.Columns.Add("FuelCost", "Fuel cost");
            this.dgvTrips.Columns.Add("MaintenanceCost", "Maintenance cost");
            this.dgvTrips.Columns.Add("TotalCost", "Total cost");

            tripsLayout.Controls.Add(this.dgvTrips, 0, 0);

            // 
            // grpCharts
            // 
            this.grpCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCharts.Text = "Charts";

            // 
            // tabCharts
            // 
            this.tabCharts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabCharts.Controls.Add(this.tabCostByMonth);
            this.tabCharts.Controls.Add(this.tabMilesByMonth);

            // 
            // tabCostByMonth
            // 
            this.tabCostByMonth.Text = "Cost by month";
            this.tabCostByMonth.Padding = new System.Windows.Forms.Padding(5);
            this.tabCostByMonth.Controls.Add(this.pnlCostByMonthChart);

            // 
            // pnlCostByMonthChart
            // 
            this.pnlCostByMonthChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCostByMonthChart.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlCostByMonthChart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // 
            // tabMilesByMonth
            // 
            this.tabMilesByMonth.Text = "Miles by month";
            this.tabMilesByMonth.Padding = new System.Windows.Forms.Padding(5);
            this.tabMilesByMonth.Controls.Add(this.pnlMilesByMonthChart);

            // 
            // pnlMilesByMonthChart
            // 
            this.pnlMilesByMonthChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMilesByMonthChart.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlMilesByMonthChart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            this.grpCharts.Controls.Add(this.tabCharts);
            tripsLayout.Controls.Add(this.grpCharts, 1, 0);

            this.grpTrips.Controls.Add(tripsLayout);

            // 
            // finalize
            // 
            this.ResumeLayout(false);
        }

        #endregion
    }
}
