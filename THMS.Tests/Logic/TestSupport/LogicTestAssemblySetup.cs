using System.Text;
using THMS.Configuration;

[SetUpFixture]
public class LogicTestAssemblySetup
{
    [OneTimeSetUp]
    public void CreateLocalSecrets()
    {
        Directory.CreateDirectory(AppPaths.Root);
        var secretsPath = AppPaths.PlaidSecrets;
        if (File.Exists(secretsPath))
            return;

        var sandbox = SecretProtector.Protect("sandbox-secret");
        var production = SecretProtector.Protect("production-secret");

        using var stream = File.Create(secretsPath);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(sandbox.Length);
        writer.Write(sandbox);
        writer.Write(production.Length);
        writer.Write(production);
    }
}
