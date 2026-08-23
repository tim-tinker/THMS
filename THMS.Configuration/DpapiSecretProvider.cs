using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Configuration
{
    public sealed class DpapiSecretProvider : ISecretProvider
    {
        private readonly string _clientId;
        private readonly string _sandboxSecret;
        private readonly string _productionSecret;

        public DpapiSecretProvider()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            _clientId = config["Plaid:ClientId"]!;

            var secretsPath = Path.Combine(AppContext.BaseDirectory, "plaid.secrets.dat");

            if (!File.Exists(secretsPath))
            {
                throw new InvalidOperationException(
                    "Missing Plaid secrets. Please run THMS.SecretWriter to generate plaid.secrets.dat.");
            }

            using var fs = new FileStream(secretsPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            var sandboxLen = br.ReadInt32();
            _sandboxSecret = SecretProtector.Unprotect(br.ReadBytes(sandboxLen));

            var prodLen = br.ReadInt32();
            _productionSecret = SecretProtector.Unprotect(br.ReadBytes(prodLen));
        }

        public string GetSandboxSecret() => _sandboxSecret;
        public string GetProductionSecret() => _productionSecret;
        public string GetClientId() => _clientId;
    }
}
