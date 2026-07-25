using System;
using System.Windows.Forms;
using THMS.Logic.ViewModels.Transportation;
using THMS.UI.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class MileageEntryForm : Form
    {
        private readonly MileageEntryViewModel _vm;
        private readonly VehicleDetailViewModel _parentVm;

        public MileageEntryForm(Guid vehicleId, VehicleDetailViewModel parentVm)
        {
            InitializeComponent();
            _parentVm = parentVm;
            _vm = new MileageEntryViewModel(parentVm.Store, vehicleId);

            dtpDate.Value = DateTime.Now;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (numOdometer.Value <= 0)
            {
                MessageBox.Show("Odometer value must be greater than zero.");
                return;
            }

            _vm.Date = dtpDate.Value;
            _vm.OdometerMiles = numOdometer.Value;

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
