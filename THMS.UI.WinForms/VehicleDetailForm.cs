using System;
using System.Linq;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.Data.Stores.InMemory;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class VehicleDetailForm : Form
    {
        private readonly VehicleDetailViewModel _vm;
        public VehicleDetailForm()
        {
            InitializeComponent();
        }

        public VehicleDetailForm(IVehicleDataStore store, Guid vehicleId)
            : this()
        {
            _vm = new VehicleDetailViewModel(store, vehicleId) ?? throw new ArgumentOutOfRangeException(nameof(vehicleId));

        }

        private void OnLoad(object sender, EventArgs e)
        {
            LoadVehicle();
            LoadGrids();
        }

        private void LoadVehicle()
        {
            var vehicle = _vm.Vehicle;
            lblNameValue.Text = vehicle.Name;
            lblMakeValue.Text = vehicle.Make;
            lblModelValue.Text = vehicle.Model;
            lblYearValue.Text = vehicle.Year.ToString();

            if (vehicle is VehicleEv evVehicle)
            {
                _splitFuelCharge.Panel1Collapsed = true;
            }
            else
            {
                _splitFuelCharge.Panel2Collapsed = true;
            }
                //    lblBatteryCapacityValue.Text = evVehicle.BatteryCapacity.ToString();
                //    lblChargingTimeValue.Text = evVehicle.ChargingTime.ToString();

                //}
                //else
                //{
                //    lblBatteryCapacityValue.Text = "N/A";
                //    lblChargingTimeValue.Text = "N/A";
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
                    f.GallonsAdded,
                    f.FuelCost
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
            var form = new ChargingCostEntryForm(_vm.VehicleId, _vm);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _vm.Refresh();
                LoadGrids();
            }
        }
    }
}
