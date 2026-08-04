using System;
using System.Linq;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class VehicleDetailForm : Form
    {
        private readonly VehicleDetailViewModel _vm;
        private bool _syncingDates;

        public VehicleDetailForm()
        {
            InitializeComponent();

            _splitFuelMaintenance.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        public VehicleDetailForm(IVehicleDataStore store, Guid vehicleId)
            : this()
        {
            _vm = new VehicleDetailViewModel(store, vehicleId) ?? throw new ArgumentOutOfRangeException(nameof(vehicleId));
        }

        private void OnLoad(object sender, EventArgs e)
        {
            var start = ClampToPicker(_vm.StartTime);
            var end = ClampToPicker(_vm.EndTime);

            _syncingDates = true;
            try
            {
                _dateStart.Value = start;
                _dateEnd.Value = end;
            }
            finally
            {
                _syncingDates = false;
            }

            // Align the VM with picker-safe values so the query matches what the
            // user sees (DateTimePicker cannot represent MinValue/MaxValue).
            _vm.StartTime = start.Date;
            _vm.EndTime = EndOfDay(end);

            LoadVehicle();
            LoadGrids();
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

        private void LoadGrids()
        {
            chargingGrid.DataSource = _vm.ChargingCosts
                .Select(c => new
                {
                    c.Timestamp,
                    c.Cost
                })
                .ToList();

            fuelGrid.DataSource = _vm.FuelReceipts
                .Select(f => new
                {
                    f.Date,
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

        private void btnAddMileage_Click(object sender, EventArgs e)
        {
            var form = new MileageEntryForm(_vm.VehicleId, _vm);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _vm.Refresh();
                LoadGrids();
            }
        }

        private void btnAddCharging_Click(object sender, EventArgs e)
        {
            var form = new EvChargeSessionForm(_vm.VehicleId, _vm);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _vm.Refresh();
                LoadGrids();
            }
        }

        private void OnValueChangedStart(object sender, EventArgs e)
        {
            if (_syncingDates)
                return;

            _vm.StartTime = _dateStart.Value.Date;
            LoadGrids();
        }

        private void OnValueChangedEnd(object sender, EventArgs e)
        {
            if (_syncingDates)
                return;

            _vm.EndTime = EndOfDay(_dateEnd.Value);
            LoadGrids();
        }

        private static DateTime EndOfDay(DateTime value) =>
            value.Date.AddDays(1).AddTicks(-1);

        private static DateTime ClampToPicker(DateTime value)
        {
            if (value < DateTimePicker.MinimumDateTime)
                return DateTimePicker.MinimumDateTime;

            if (value > DateTimePicker.MaximumDateTime || value.Date > DateTime.Today)
                return DateTime.Today;

            return value.Date;
        }
    }
}
