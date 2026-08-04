using System;
using System.Windows.Forms;
using System.Xml.Linq;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace THMS.UI.WinForms
{
    public partial class AddVehicleForm : Form
    {
        private readonly VehicleListViewModel _vm;

        public AddVehicleForm(VehicleListViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vehicle name is required.");
                return;
            }

            var vehicle = _checkEv.Checked
                ? (VehicleBase)new VehicleEv
                {
                    Id = Guid.NewGuid(),
                    Name = txtName.Text.Trim(),
                    Make = txtMake.Text.Trim(),
                    Model = txtModel.Text.Trim(),
                    Year = (int)numYear.Value,
                    BatteryCapacityKwh = _numericFuelCapacity.Value
                }
                : new VehicleIce
                {
                    Id = Guid.NewGuid(),
                    Name = txtName.Text.Trim(),
                    Make = txtMake.Text.Trim(),
                    Model = txtModel.Text.Trim(),
                    Year = (int)numYear.Value,
                    FuelTankCapacityGallons = _numericFuelCapacity.Value
                };

            _vm.AddVehicle(vehicle);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnCheckedChangedEv(object sender, EventArgs e)
        {
            _labelFuelType.Text = _checkEv.Checked ? "kiloWatt-hours" : "gallons";
        }
    }
}
