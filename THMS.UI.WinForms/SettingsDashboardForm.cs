using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    // TODO: Not used. Other forms or updaters appear to have replaced this class. Remove later once that is confirmed.
    public partial class SettingsDashboardForm : BaseDashboardForm
    {
        private SettingsDashboardViewModel ViewModel { get; set; } = null!;

        public SettingsDashboardForm()
        {
            InitializeComponent();
        }

        public override void InitializeDashboard()
        {
            ViewModel = new SettingsDashboardViewModel();
            BindControlsToViewModel();
            ViewModel.Initialize();
            RefreshDashboard();
        }

        private void BindControlsToViewModel()
        {
            chkDarkMode.Checked = ViewModel.DarkModeEnabled;
            chkAutoSave.Checked = ViewModel.AutoSaveEnabled;
            chkShowTooltips.Checked = ViewModel.ShowTooltips;
        }

        private void OnDarkModeCheckChanged(object sender, EventArgs e)
        {
            ViewModel.DarkModeEnabled = chkDarkMode.Checked;
        }

        private void OnAutoSaveCheckChanged(object sender, EventArgs e)
        {
            ViewModel.AutoSaveEnabled = chkAutoSave.Checked;
        }

        private void OnShowTooltipsCheckChanged(object sender, EventArgs e)
        {
            ViewModel.ShowTooltips = chkShowTooltips.Checked;
        }

        public override void RefreshDashboard()
        {
            chkDarkMode.Checked = ViewModel.DarkModeEnabled;
            chkAutoSave.Checked = ViewModel.AutoSaveEnabled;
            chkShowTooltips.Checked = ViewModel.ShowTooltips;
        }
    }
}
