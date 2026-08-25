using THMS.Domain.Finance.Transactions;
using THMS.Domain.Finance.Accounts;
using THMS.External.Plaid;
using THMS.External;
using THMS.Logic.Mapping;

namespace THMS.Logic.Orchestrators
{
    public class ExternalTransactionAccess
    {
        private readonly IExternalTransactionFetcher _fetcher;

        public ExternalTransactionAccess(IExternalTransactionFetcher fetcher)
        {
            _fetcher = fetcher;
        }

        public async Task<List<PostedTransaction>> FetchPostedTransactionsAsync(
            Account account,
            DateTime start,
            DateTime end)
        {
            if (account.ExternalLink == null ||
                string.IsNullOrWhiteSpace(account.ExternalLink.AccessToken))
                return new List<PostedTransaction>();

            AccountDto accountDto = account.ToDto();

            var dtos = await _fetcher.FetchTransactionsAsync(
                accountDto,
                start,
                end);

            return dtos.Select(dto => new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Date = dto.Date ?? DateTime.Today,
                Amount = dto.Amount,
                Description = dto.Name,
                PlaidCategory = dto.Category,
                Category = null
            }).ToList();
        }
    }
}
