using System.Linq;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class VehicleDetailForm : Form
    {
        private readonly VehicleDetailViewModel _vm = null!;
        private readonly string _initialPeriod = HistoryPeriodBar.Month;
        private bool _chargingGridBound;

        public VehicleDetailForm()
        {
            InitializeComponent();

            _splitFuelMaintenance.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        public VehicleDetailForm(Guid vehicleId, string? historyPeriod = null)
            : this()
        {
            _vm = new VehicleDetailViewModel(vehicleId);
            _initialPeriod = string.IsNullOrWhiteSpace(historyPeriod)
                ? HistoryPeriodBar.Month
                : historyPeriod;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            if (_vm is null)
                return;

            historyBar.SelectPeriod(_initialPeriod);
            ApplyHistoryPeriod();
            LoadVehicle();
            BindChargeGrid();
            LoadGrids();
            historyBar.SelectedPeriodChanged += (_, _) =>
            {
                ApplyHistoryPeriod();
                LoadGrids();
            };
        }

        private void ApplyHistoryPeriod()
        {
            _vm.HistoryPeriod = historyBar.SelectedPeriod;
        }

        private void LoadVehicle()
        {
            var vehicle = _vm.Vehicle;
            lblName.Text = $"{vehicle.Name}:";
            lblMakeModelYear.Text = $"{vehicle.Year} {vehicle.Make} {vehicle.Model}";

            if (vehicle is VehicleEv)
            {
                _splitFuelCharge.Panel1Collapsed = true;
            }
            else
            {
                _splitFuelCharge.Panel2Collapsed = true;
            }
        }

        private void BindChargeGrid()
        {
            if (_chargingGridBound)
                return;

            chargingGrid.AutoGenerateColumns = false;
            chargingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            chargingGrid.Columns.Clear();
            chargingGrid.Columns.Add(DateColumn(nameof(VehicleChargeCostRow.StartTime), "Date/Time", "g"));
            chargingGrid.Columns.Add(NumberColumn(nameof(VehicleChargeCostRow.MilesDriven), "Miles Driven", "N1"));
            chargingGrid.Columns.Add(NumberColumn(nameof(VehicleChargeCostRow.Cost), "Cost", "c2"));
            chargingGrid.Columns.Add(NumberColumn(nameof(VehicleChargeCostRow.CostPerMile), "Cost per Mile", "c2"));
            chargingGrid.DataSource = _vm.ChargeCostRows;
            _chargingGridBound = true;
        }

        private static DataGridViewTextBoxColumn DateColumn(string property, string header, string format) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                DefaultCellStyle = new DataGridViewCellStyle { Format = format }
            };

        private static DataGridViewTextBoxColumn NumberColumn(string property, string header, string format) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = format
                }
            };

        private void LoadGrids()
        {
            fuelGrid.DataSource = _vm.FuelReceipts
                .Select(f => new
                {
                    f.EndTime,
                    f.OdometerMiles,
                    f.GallonsAdded,
                    f.FuelCost,
                    f.IsFullFillUp,
                })
                .ToList();

            maintenanceGrid.DataSource = _vm.MaintenanceInvoices
                .Select(m => new
                {
                    m.Date,
                    m.Description,
                    m.Cost
                })
                .ToList();
        }
    }
}
