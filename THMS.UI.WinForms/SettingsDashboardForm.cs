using System;
using System.Windows.Forms;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class SettingsDashboardForm : BaseDashboardForm
    {
        private SettingsDashboardViewModel _vm;

        public SettingsDashboardForm()
        {
            InitializeComponent();
        }

        protected override void OnBindViewModel(BaseDashboardViewModel viewModel)
        {
            _vm = viewModel as SettingsDashboardViewModel
                ?? throw new ArgumentException("Invalid ViewModel type");

            chkDarkMode.Checked = _vm.DarkModeEnabled;
            chkAutoSave.Checked = _vm.AutoSaveEnabled;
            chkShowTooltips.Checked = _vm.ShowTooltips;

        }

        private void OnDarkModeCheckChanged(object sender, EventArgs e)
        {
            _vm.DarkModeEnabled = chkDarkMode.Checked;
        }

        private void OnAutoSaveCheckChanged(object sender, EventArgs e)
        {
            _vm.AutoSaveEnabled = chkAutoSave.Checked;
        }

        private void OnShowTooltipsCheckChanged(object sender, EventArgs e)
        {
            _vm.ShowTooltips = chkShowTooltips.Checked;
        }

        public override void OnActivated()
        {
            // Refresh UI if settings changed elsewhere
            chkDarkMode.Checked = _vm.DarkModeEnabled;
            chkAutoSave.Checked = _vm.AutoSaveEnabled;
            chkShowTooltips.Checked = _vm.ShowTooltips;
        }
    }
}
