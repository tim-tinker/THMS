namespace THMS.External
{
    public interface IPlaidLinkSession
    {
        Task<string> CreateLinkTokenAsync(string userId);
        Task<PlaidItemConnection> ExchangeForItemAsync(string publicToken);
        Task<string> CreateSandboxPublicTokenAsync(string institutionId);
        Task<PlaidInstitutionInfo> GetInstitutionAsync(string accessToken);
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
