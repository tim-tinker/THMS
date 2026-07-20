using Microsoft.Extensions.DependencyInjection;
using TPFS.UI.WinForms.Navigation;
using TPFS.Logic.ViewModels.Home;
using TPFS.UI.WinForms.Views.Home;

namespace TPFS.UI.WinForms;

internal static class Program
{
    public static ServiceProvider Services { get; private set; } = null!;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        ConfigureServices();

        Application.Run(Services.GetRequiredService<MainForm>());
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<NavigationService>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<HomeView>();
        services.AddSingleton<MainForm>();

        // Later:
        // services.AddSingleton<IAnalyticsService, AnalyticsService>();
        // services.AddSingleton<IIngestionService, IngestionService>();
        // services.AddSingleton<IRepository, Repository>();

        Services = services.BuildServiceProvider();
    }
}
