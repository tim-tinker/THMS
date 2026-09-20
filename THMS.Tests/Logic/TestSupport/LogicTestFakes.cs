using THMS.Domain.Finance.Accounts;
using THMS.Domain.Transportation;
using THMS.External;

namespace THMS.Tests.Logic.TestSupport
{
    public sealed class FakeAccountFetcher : IExternalAccountFetcher
    {
        public List<AccountDto> Accounts { get; set; } = [];
        public Exception? Exception { get; set; }
        public string? LastAccessToken { get; private set; }

        public Task<List<AccountDto>> FetchAccountsAsync(string accessToken)
        {
            LastAccessToken = accessToken;
            if (Exception is not null)
                throw Exception;
            return Task.FromResult(Accounts);
        }
    }

    public sealed class FakeTransactionFetcher : IExternalTransactionFetcher
    {
        public List<TransactionDto> Transactions { get; set; } = [];
        public Exception? Exception { get; set; }
        public AccountDto? LastAccount { get; private set; }

        public Task<List<TransactionDto>> FetchTransactionsAsync(
            AccountDto account,
            DateTime start,
            DateTime end)
        {
            LastAccount = account;
            if (Exception is not null)
                throw Exception;
            return Task.FromResult(Transactions);
        }
    }

    public sealed class FakePlaidLinkSession : IPlaidLinkSession
    {
        public string LinkToken { get; set; } = "link-sandbox";
        public string SandboxPublicToken { get; set; } = "public-sandbox";
        public string AccessToken { get; set; } = "access-sandbox";
        public string ItemId { get; set; } = "item-1";
        public string InstitutionId { get; set; } = "ins_109508";
        public string InstitutionName { get; set; } = "First Platypus Bank";
        public string HostedLinkUrl { get; set; } = "https://cdn.plaid.com/link/hosted";
        public string? SessionPublicToken { get; set; } = "public-from-session";
        public string? LastPublicToken { get; private set; }
        public string? LastLinkTokenGet { get; private set; }
        public bool CreatedSandboxToken { get; private set; }
        public bool CreatedLinkToken { get; private set; }
        public bool HostedLinkRequested { get; private set; }
        public int PublicTokenLookups { get; private set; }
        public int PublicTokenLookupsUntilAvailable { get; set; }

        public Task<PlaidLinkTokenResult> CreateLinkTokenAsync(string userId, bool hostedLink = false)
        {
            CreatedLinkToken = true;
            HostedLinkRequested = hostedLink;
            return Task.FromResult(new PlaidLinkTokenResult
            {
                LinkToken = LinkToken,
                HostedLinkUrl = hostedLink ? HostedLinkUrl : null
            });
        }

        public Task<string?> GetPublicTokenFromLinkSessionAsync(string linkToken)
        {
            LastLinkTokenGet = linkToken;
            PublicTokenLookups++;
            if (PublicTokenLookupsUntilAvailable > 0 && PublicTokenLookups < PublicTokenLookupsUntilAvailable)
                return Task.FromResult<string?>(null);
            return Task.FromResult(SessionPublicToken);
        }

        public Task<PlaidItemConnection> ExchangeForItemAsync(string publicToken)
        {
            LastPublicToken = publicToken;
            return Task.FromResult(new PlaidItemConnection
            {
                AccessToken = AccessToken,
                ItemId = ItemId
            });
        }

        public Task<string> CreateSandboxPublicTokenAsync(string institutionId)
        {
            CreatedSandboxToken = true;
            return Task.FromResult(SandboxPublicToken);
        }

        public Task<PlaidInstitutionInfo> GetInstitutionAsync(string accessToken)
        {
            return Task.FromResult(new PlaidInstitutionInfo
            {
                InstitutionId = InstitutionId,
                ItemId = ItemId,
                Name = InstitutionName
            });
        }
    }

    public sealed class UnknownAccount : Account
    {
    }

    public sealed class UnknownVehicle : VehicleBase
    {
    }

    public sealed class OtherEvChargeSession : BaseEvChargeSession
    {
    }

    public sealed class TestableBaseOrchestrator : THMS.Logic.Orchestrators.BaseOrchestrator
    {
        public DateTime CallGetStartDate(DateTime end, string period) => GetStartDate(end, period);
    }
}
