using Microsoft.Extensions.DependencyInjection;
using THMS.Data.Stores;
using THMS.UI.WinForms.Navigation;

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

        services.AddSingleton<NavigationService>();
        services.AddSingleton<MainForm>();

        // Later, switch implementations without touching forms:
        // services.AddSingleton<IVehicleDataStore, SQLiteVehicleDataStore>();

        return services.BuildServiceProvider();
    }
}
