namespace THMS.Configuration
{
    public interface ISecretProvider
    {
        string GetSandboxSecret();
        string GetProductionSecret();
        string GetClientId();
    }
}
