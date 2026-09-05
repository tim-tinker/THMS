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
