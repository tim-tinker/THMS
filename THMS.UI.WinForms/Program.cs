using Microsoft.Extensions.DependencyInjection;
using THMS.Data.Stores;
using THMS.Logic.Energy;
using THMS.UI.WinForms.Navigation;
using THMS.UI.WinForms.Updates;

namespace THMS.UI.WinForms;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Services = BuildServices();
        Application.Run(Services.GetRequiredService<MainForm>());
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // Shared data stores — one instance for the whole app
        services.AddSingleton<IVehicleDataStore, InMemoryVehicleDataStore>();
        services.AddSingleton<IFinanceDataStore, InMemoryFinanceDataStore>();
        services.AddSingleton<IEnergyDataStore, InMemoryEnergyDataStore>();

        // Dashboard forms — created once, hosted inside MainForm
        services.AddSingleton<TransportationDashboardForm>();
        services.AddSingleton<EnergyDashboardForm>();
        services.AddSingleton<FinanceDashboardForm>();
        services.AddSingleton<VehicleListDashboardForm>();
        services.AddSingleton<SettingsDashboardForm>();
        services.AddSingleton<DataCenterForm>();
        services.AddSingleton<DataManagerForm>();

        services.AddSingleton<EnergyAggregationService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<MainForm>();

        // Data source update services
        services.AddSingleton<IDataSourceUpdater, SolarDataUpdater>();
        services.AddSingleton<IDataSourceUpdater, HomeCircuitUpdater>();
        services.AddSingleton<IDataSourceUpdater, HomeCircuitAttributionUpdater>();
        services.AddSingleton<IDataSourceUpdater, EvChargeSessionUpdater>();
        services.AddSingleton<IDataSourceUpdater, ElectricContractUpdater>();

        // Later, switch implementations without touching forms:
        // services.AddSingleton<IVehicleDataStore, SQLiteVehicleDataStore>();

        return services.BuildServiceProvider();
    }
}
