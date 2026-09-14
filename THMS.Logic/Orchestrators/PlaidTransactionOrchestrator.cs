using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.External;
using THMS.Logic.Mapping;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class PlaidTransactionOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly IExternalTransactionFetcher _transactionFetcher;
        private readonly TransactionImportOrchestrator _importOrchestrator;

        public PlaidTransactionOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new ExternalFetcherFactory().GetTransactionFetcher(),
                new DataStoreFactory().GetTransactionStore())
        {
        }

        public PlaidTransactionOrchestrator(
            IAccountDataStore accounts,
            IExternalTransactionFetcher transactionFetcher,
            ITransactionDataStore transactions)
            : this(accounts, transactionFetcher, new TransactionImportOrchestrator(transactionFetcher, transactions, accounts))
        {
        }

        public PlaidTransactionOrchestrator(
            IAccountDataStore accounts,
            IExternalTransactionFetcher transactionFetcher,
            TransactionImportOrchestrator importOrchestrator)
        {
            _accounts = accounts;
            _transactionFetcher = transactionFetcher;
            _importOrchestrator = importOrchestrator;
        }

        public async Task<List<PlaidTransactionViewModel>> DownloadNewTransactions(DateTime start, DateTime end)
        {
            var linked = _accounts.GetAllAccounts()
                .Where(HasPlaidLink)
                .ToList();
            if (linked.Count == 0)
                throw new InvalidOperationException(
                    "No Plaid-linked accounts. Connect accounts to Plaid first.");

            var results = new List<PlaidTransactionViewModel>();
            foreach (var group in linked.GroupBy(a => a.ExternalLink!.AccessToken))
            {
                var representative = group.First();
                var dtos = await _transactionFetcher.FetchTransactionsAsync(
                    representative.ToDto(),
                    start,
                    end);

                foreach (var dto in dtos)
                {
                    var match = group.FirstOrDefault(a =>
                        string.Equals(a.ExternalLink!.PlaidAccountId, dto.AccountId, StringComparison.Ordinal));
                    if (match is null)
                        continue;

                    var date = dto.Date ?? start;
                    if (date.Date < start.Date || date.Date > end.Date)
                        continue;

                    results.Add(new PlaidTransactionViewModel
                    {
                        Date = date,
                        Description = dto.Name,
                        Amount = dto.Amount,
                        Account = match.Name,
                        Category = dto.Category ?? "",
                        Pending = dto.Pending,
                        AccountId = match.Id,
                        PlaidAccountId = dto.AccountId
                    });
                }
            }

            return results.OrderBy(r => r.Date).ThenBy(r => r.Description).ToList();
        }

        public ImportResult ImportTransactions(IEnumerable<PlaidTransactionViewModel> previewRows) =>
            ImportTransactions(previewRows, progress: null);

        public ImportResult ImportTransactions(
            IEnumerable<PlaidTransactionViewModel> previewRows,
            IProgress<ImportProgress>? progress)
        {
            ArgumentNullException.ThrowIfNull(previewRows);
            var mapped = previewRows
                .Where(row => !row.Pending)
                .Select(row => new TransactionImportPreview
                {
                    Date = row.Date,
                    Description = row.Description,
                    Amount = row.Amount,
                    Account = row.Account,
                    Category = row.Category,
                    AccountId = row.AccountId
                });

            return _importOrchestrator.ImportTransactions(mapped, progress);
        }

        private static bool HasPlaidLink(Account account) =>
            account.ExternalLink is { } link
            && !string.IsNullOrWhiteSpace(link.AccessToken)
            && !string.IsNullOrWhiteSpace(link.PlaidAccountId);
    }
}
