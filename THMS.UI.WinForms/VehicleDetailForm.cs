using System;
using System.Linq;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class VehicleDetailForm : Form
    {
        private readonly VehicleDetailViewModel _vm;

        public VehicleDetailForm(TransportationDataStore store, Guid vehicleId)
        {
            InitializeComponent();
            _vm = new VehicleDetailViewModel(store, vehicleId);
            LoadVehicle();
            LoadGrids();
        }

        private void LoadVehicle()
        {
            if (_vm.Vehicle == null)
            {
                MessageBox.Show("Vehicle not found.");
                Close();
                return;
            }

            lblNameValue.Text = _vm.Vehicle.Name;
            lblMakeValue.Text = _vm.Vehicle.Make;
            lblModelValue.Text = _vm.Vehicle.Model;
            lblYearValue.Text = _vm.Vehicle.Year.ToString();
        }

        private void LoadGrids()
        {
            mileageGrid.DataSource = _vm.Mileage
                .Select(m => new
                {
                    m.Date,
                    m.OdometerMiles
                })
                .ToList();

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
                    f.Gallons,
                    f.Cost
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
