using System;
using System.Windows.Forms;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class ChargingCostEntryForm : Form
    {
        private readonly ChargingCostEntryViewModel _vm;
        private readonly VehicleDetailViewModel _parentVm;

        public ChargingCostEntryForm(Guid vehicleId, VehicleDetailViewModel parentVm)
        {
            InitializeComponent();
            _parentVm = parentVm;
            _vm = new ChargingCostEntryViewModel(parentVm.Store, vehicleId);

            dtpTimestamp.Value = DateTime.Now;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (numCost.Value <= 0)
            {
                MessageBox.Show("Cost must be greater than zero.");
                return;
            }

            _vm.Timestamp = dtpTimestamp.Value;
            _vm.Cost = numCost.Value;

            _vm.Save();
            _parentVm.Refresh();

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
