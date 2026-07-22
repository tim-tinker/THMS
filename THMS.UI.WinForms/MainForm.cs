using THMS.Domain;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms;

public partial class MainForm : Form
{
    private TransportationDashboardForm _transportationDashboard;
    private EnergyDashboardForm _energyDashboard;
    private FinanceDashboardForm _financeDashboard;
    private HouseholdDashboardForm _householdDashboard;
    private SettingsDashboardForm _settingsDashboard;
    private TransportationDashboardViewModel _transportationVm;
    private EnergyDashboardViewModel _energyVm;
    private FinanceDashboardViewModel _financeVm;
    private HouseholdDashboardViewModel _householdVm;
    private SettingsDashboardViewModel _settingsVm;

    private readonly Dictionary<string, BaseDashboardForm> _modules = new Dictionary<string, BaseDashboardForm>();

    public MainForm()
    {
        InitializeComponent();
        InitializeModules();
    }

    private void InitializeModules()
    {
        _transportationVm = new TransportationDashboardViewModel();
        _energyVm = new EnergyDashboardViewModel();
        _financeVm = new FinanceDashboardViewModel();
        _householdVm = new HouseholdDashboardViewModel();
        _settingsVm = new SettingsDashboardViewModel();

        _transportationDashboard = new TransportationDashboardForm();
        _energyDashboard = new EnergyDashboardForm();
        _financeDashboard = new FinanceDashboardForm();
        _householdDashboard = new HouseholdDashboardForm();
        _settingsDashboard = new SettingsDashboardForm();

        _transportationDashboard.BindViewModel(_transportationVm);
        _energyDashboard.BindViewModel(_energyVm);
        _financeDashboard.BindViewModel(_financeVm);
        _householdDashboard.BindViewModel(_householdVm);
        _settingsDashboard.BindViewModel(_settingsVm);

        RegisterModule("Transportation", _transportationDashboard);
        RegisterModule("Energy", _energyDashboard);
        RegisterModule("Finance", _financeDashboard);
        RegisterModule("Household", _householdDashboard);
        RegisterModule("Settings", _settingsDashboard);
    }

    private void RegisterModule(string key, BaseDashboardForm form)
    {
        form.Visible = false;
        contentPanel.Controls.Add(form);
        _modules[key] = form;
    }

    private void btnTransportation_Click(object sender, EventArgs e)
    {
        ShowModule("Transportation");
    }

    private void btnEnergy_Click(object sender, EventArgs e)
    {
        ShowModule("Energy");
    }

    private void btnFinance_Click(object sender, EventArgs e)
    {
        ShowModule("Finance");
    }

    private void btnHousehold_Click(object sender, EventArgs e)
    {
        ShowModule("Household");
    }

    private void btnSettings_Click(object sender, EventArgs e)
    {
        ShowModule("Settings");
    }

    private void ShowModule(string key)
    {
        foreach (var module in _modules.Values)
            module.Visible = false;

        var selected = _modules[key];
        selected.Visible = true;
        selected.BringToFront();
        selected.OnActivated();
    }

}
