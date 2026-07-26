using System;
using System.Windows.Forms;
using System.Xml.Linq;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;
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

            var vehicle = new VehicleBase
            {
                Id = Guid.NewGuid(),
                Name = txtName.Text.Trim(),
                Make = txtMake.Text.Trim(),
                Model = txtModel.Text.Trim(),
                Year = (int)numYear.Value
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
    }
}
