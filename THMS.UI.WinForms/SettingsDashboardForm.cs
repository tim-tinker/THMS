using System;
using System.Windows.Forms;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class SettingsDashboardForm : BaseDashboardForm<SettingsDashboardViewModel>
    {
        private SettingsDashboardViewModel _vm;

        public SettingsDashboardForm()
        {
            InitializeComponent();
        }

        protected override void BindControlsToViewModel()
        {
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

        public override void RefreshDashboard()
        {
            // Refresh UI if settings changed elsewhere
            chkDarkMode.Checked = _vm.DarkModeEnabled;
            chkAutoSave.Checked = _vm.AutoSaveEnabled;
            chkShowTooltips.Checked = _vm.ShowTooltips;
        }
    }
}
