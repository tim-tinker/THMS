using System;
using System.Collections.Generic;
using System.Windows.Forms;
using THMS.Domain.Transportation;

namespace THMS.UI.WinForms
{
    public partial class VehicleSelectionForm : Form
    {
        public VehicleBase? SelectedVehicle { get; private set; }

        public VehicleSelectionForm(IEnumerable<VehicleBase> vehicles)
        {
            InitializeComponent();

            gridVehicles.AutoGenerateColumns = false;
            gridVehicles.DataSource = vehicles;
        }

        private void OnClickOk(object sender, EventArgs e)
        {
            if (gridVehicles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a vehicle.");
                return;
            }

            SelectedVehicle = gridVehicles.SelectedRows[0].DataBoundItem as VehicleBase;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnClickCancel(object sender, EventArgs e)
        {
            SelectedVehicle = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
