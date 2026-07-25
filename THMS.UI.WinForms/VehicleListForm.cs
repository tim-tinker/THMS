using System;
using System.Linq;
using System.Windows.Forms;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class VehicleListForm : Form
    {
        private readonly VehicleListViewModel _vm;

        public VehicleListForm(TransportationDataStore store)
        {
            InitializeComponent();
            _vm = new VehicleListViewModel(store);
            LoadVehicles();
        }

        private void LoadVehicles()
        {
            vehicleGrid.DataSource = _vm.Vehicles
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Make,
                    v.Model,
                    v.Year
                })
                .ToList();
        }

        private void vehicleGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (vehicleGrid.SelectedRows.Count == 0)
            {
                _vm.SelectedVehicle = null;
                return;
            }

            var id = (Guid)vehicleGrid.SelectedRows[0].Cells["Id"].Value;
            _vm.SelectedVehicle = _vm.Vehicles.First(v => v.Id == id);
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            var form = new AddVehicleForm(_vm);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadVehicles();
            }
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
            if (_vm.SelectedVehicle == null)
            {
                MessageBox.Show("Please select a vehicle.");
                return;
            }

            var form = new VehicleDetailForm(_vm.Store, _vm.SelectedVehicle.Id);
            form.ShowDialog(this);
        }
    }
}
