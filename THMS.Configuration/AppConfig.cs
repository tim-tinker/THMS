using Microsoft.Extensions.Configuration;

namespace THMS.Configuration
{
    public sealed class AppConfig
    {
        private static readonly Lazy<AppConfig> _instance =
            new(() => new AppConfig());

        public static AppConfig Instance => _instance.Value;

        public string Environment { get; }

        public string SQLiteDataBase { get; }

        public ISecretProvider Secrets { get; }

        public string PlaidClientId { get; }
        public string PlaidEnvironment { get; }

        private AppConfig()
        {
            // for DPAPI design, use DpapiSecretProvider
            // for embedded encrypted secrets, use EmbeddedSecretProvider (TBD) (share app with trusted friends)
            // for backend server design, use BackendSecretProvider (TBD) (general availability)
            Secrets = new DpapiSecretProvider();

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            Environment = config["App:Environment"] ?? "Development";

            SQLiteDataBase = config["SQLite:DataBasePath"] ?? string.Empty;

            PlaidClientId = config["Plaid:ClientId"]!;
            PlaidEnvironment = config["Plaid:Environment"] ?? "Sandbox";
        }
    }
}
