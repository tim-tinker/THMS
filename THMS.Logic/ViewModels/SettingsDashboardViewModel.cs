namespace THMS.Logic.ViewModels
{
    public class SettingsDashboardViewModel : BaseDashboardViewModel
    {
        public bool DarkModeEnabled { get; set; }
        public bool AutoSaveEnabled { get; set; }
        public bool ShowTooltips { get; set; }

        public SettingsDashboardViewModel()
        {
            DarkModeEnabled = false;
            AutoSaveEnabled = true;
            ShowTooltips = true;
        }
    }
}
