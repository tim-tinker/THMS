namespace THMS.External
{
    public interface IPlaidLinkSession
    {
        Task<PlaidLinkTokenResult> CreateLinkTokenAsync(string userId, bool hostedLink = false);
        Task<string?> GetPublicTokenFromLinkSessionAsync(string linkToken);
        Task<PlaidItemConnection> ExchangeForItemAsync(string publicToken);
        Task<string> CreateSandboxPublicTokenAsync(string institutionId);
        Task<PlaidInstitutionInfo> GetInstitutionAsync(string accessToken);
    }

    public sealed class PlaidLinkTokenResult
    {
        public string LinkToken { get; init; } = "";
        public string? HostedLinkUrl { get; init; }
    }

    public sealed class PlaidItemConnection
    {
        public string AccessToken { get; init; } = "";
        public string ItemId { get; init; } = "";
    }

    public sealed class PlaidInstitutionInfo
    {
        public string InstitutionId { get; init; } = "";
        public string ItemId { get; init; } = "";
        public string Name { get; init; } = "";
    }
}
