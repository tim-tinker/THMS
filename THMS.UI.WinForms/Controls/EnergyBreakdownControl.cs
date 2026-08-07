using System.Drawing;
using System.Windows.Forms;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public class EnergyBreakdownControl : UserControl
    {
        private const int MetricCount = 7;

        private TableLayoutPanel _table = null!;

        private Label _lblSolar = null!;
        private Label _lblBatteryCharge = null!;
        private Label _lblBatteryDischarge = null!;
        private Label _lblGridImport = null!;
        private Label _lblGridExport = null!;
        private Label _lblHome = null!;
        private Label _lblEv = null!;

        public EnergyBreakdownControl(EnergyDayViewModel vm)
        {
            LayoutTable();
            LoadFromDay(vm);
        }

        public EnergyBreakdownControl(EnergyPeriodViewModel vm)
        {
            LayoutTable();
            LoadFromPeriod(vm);
        }

        private void LayoutTable()
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(8);

            _table = CreateTable();
            Controls.Add(_table);

            _lblSolar = AddRow("Solar");
            _lblBatteryCharge = AddRow("Battery Charge");
            _lblBatteryDischarge = AddRow("Battery Discharge");
            _lblGridImport = AddRow("Grid Import");
            _lblGridExport = AddRow("Grid Export");
            _lblHome = AddRow("Home Consumption");
            _lblEv = AddRow("EV Charging");
        }

        private void LoadFromDay(EnergyDayViewModel vm)
        {
            _lblSolar.Text = $"{vm.SolarKwh:N1} kWh";
            _lblBatteryCharge.Text = $"{vm.BatteryChargeKwh:N1} kWh";
            _lblBatteryDischarge.Text = $"{vm.BatteryDischargeKwh:N1} kWh";
            _lblGridImport.Text = $"{vm.GridImportKwh:N1} kWh";
            _lblGridExport.Text = $"{vm.GridExportKwh:N1} kWh";
            _lblHome.Text = $"{vm.HomeConsumptionKwh:N1} kWh";
            _lblEv.Text = $"{vm.EvChargingKwh:N1} kWh";
        }

        private void LoadFromPeriod(EnergyPeriodViewModel vm)
        {
            _lblSolar.Text = $"{vm.SolarKwh:N1} kWh";
            _lblBatteryCharge.Text = $"{vm.BatteryChargeKwh:N1} kWh";
            _lblBatteryDischarge.Text = $"{vm.BatteryDischargeKwh:N1} kWh";
            _lblGridImport.Text = $"{vm.GridImportKwh:N1} kWh";
            _lblGridExport.Text = $"{vm.GridExportKwh:N1} kWh";
            _lblHome.Text = $"{vm.HomeConsumptionKwh:N1} kWh";
            _lblEv.Text = $"{vm.EvChargingKwh:N1} kWh";
        }

        private static TableLayoutPanel CreateTable()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 0,
                AutoSize = false,
                BackColor = Color.White,
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            return table;
        }

        private Label AddRow(string labelText)
        {
            int rowIndex = _table.RowCount;
            _table.RowCount++;
            _table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / MetricCount));

            var lblName = new Label
            {
                Text = labelText,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 2, 4, 2),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
            };

            var lblValue = new Label
            {
                Text = "0.0 kWh",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 2, 4, 2),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
            };

            _table.Controls.Add(lblName, 0, rowIndex);
            _table.Controls.Add(lblValue, 1, rowIndex);
            return lblValue;
        }
    }
}
