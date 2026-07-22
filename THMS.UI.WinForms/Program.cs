using Microsoft.Extensions.DependencyInjection;
using THMS.UI.WinForms.Navigation;
using THMS.Logic.ViewModels.Home;
using THMS.UI.WinForms.Views.Home;

namespace THMS.UI.WinForms;

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
