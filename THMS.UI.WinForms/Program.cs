using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using THMS.Data.Stores;
using THMS.External.Plaid;
using THMS.Logic.Energy;
using THMS.Logic.Orchestrators;
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

        // Orchestrators
        services.AddSingleton<RegisterUpdateOrchestrator>();

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

        // client for Plaid aggregator
        services.AddSingleton<PlaidClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var baseUrl = config["Plaid:Environment"] switch
            {
                "Sandbox" => "https://sandbox.plaid.com",
                "Development" => "https://development.plaid.com",
                "Production" => "https://production.plaid.com",
                _ => "https://sandbox.plaid.com"
            };

            return new PlaidClient(
                new HttpClient(),
                new PlaidClientOptions
                {
                    ClientId = config["Plaid:ClientId"],
                    Secret = config["Plaid:Secret"],
                    BaseUrl = baseUrl
                });
        });


        // Later, switch implementations without touching forms:
        // services.AddSingleton<IVehicleDataStore, SQLiteVehicleDataStore>();

        return services.BuildServiceProvider();
    }
}
