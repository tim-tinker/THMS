using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Aggregation;

namespace THMS.Logic.Orchestrators.Finance
{
    public class TransactionDiagnosticsOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;

        public TransactionDiagnosticsOrchestrator()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetTransactionStore())
        {
        }

        public TransactionDiagnosticsOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
        }

        public List<string> Run()
        {
            var findings = new List<string>();
            var accountIds = _accounts.GetAllAccounts().Select(a => a.Id).ToHashSet();
            var categoryIds = _transactions.GetAllCategories(includeInactive: true).Select(c => c.Id).ToHashSet();
            var posted = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var transfers = _transactions.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var futureSingles = _transactions.GetAllFutureSingleTransactions().ToList();
            var futureTransfers = _transactions.GetAllFutureTransferTransactions().ToList();

            FindUncategorized(posted, findings);
            FindDuplicates(posted, findings);
            FindZeroAmounts(posted, transfers, futureSingles, futureTransfers, findings);
            FindFuturePosted(posted, findings);
            FindMissingDescriptions(posted, transfers, findings);
            FindUnknownCategories(posted, categoryIds, findings);
            FindOrphans(posted, transfers, futureSingles, futureTransfers, accountIds, findings);

            if (findings.Count == 0)
                findings.Add("No issues found.");

            return findings;
        }

        private static void FindUncategorized(IReadOnlyList<PostedTransaction> posted, List<string> findings)
        {
            foreach (var transaction in posted.Where(FinanceDashboardComposer.IsUncategorized))
                findings.Add($"Uncategorized transaction {Describe(transaction)}");
        }

        private static void FindDuplicates(IReadOnlyList<PostedTransaction> posted, List<string> findings)
        {
            foreach (var group in posted
                         .GroupBy(t => (
                             t.AccountId,
                             t.Date.Date,
                             t.Amount,
                             (t.Description ?? "").Trim().ToLowerInvariant()))
                         .Where(g => g.Count() > 1))
            {
                var sample = group.First();
                findings.Add($"Duplicate transaction {Describe(sample)} ({group.Count()})");
            }
        }

        private static void FindZeroAmounts(
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<PostedTransferTransaction> transfers,
            IReadOnlyList<FutureSingleTransaction> futureSingles,
            IReadOnlyList<FutureTransferTransaction> futureTransfers,
            List<string> findings)
        {
            foreach (var transaction in posted.Where(t => t.Amount == 0))
                findings.Add($"Zero-amount transaction {Describe(transaction)}");
            foreach (var transaction in transfers.Where(t => t.Amount == 0))
                findings.Add($"Zero-amount transfer {Describe(transaction)}");
            foreach (var transaction in futureSingles.Where(t => t.Amount == 0))
                findings.Add($"Zero-amount future transaction {Describe(transaction)}");
            foreach (var transaction in futureTransfers.Where(t => t.Amount == 0))
                findings.Add($"Zero-amount future transfer {Describe(transaction)}");
        }

        private static void FindFuturePosted(IReadOnlyList<PostedTransaction> posted, List<string> findings)
        {
            foreach (var transaction in posted.Where(t => t.Date.Date > DateTime.Today))
                findings.Add($"Posted transaction is in the future: {Describe(transaction)}");
        }

        private static void FindMissingDescriptions(
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<PostedTransferTransaction> transfers,
            List<string> findings)
        {
            foreach (var transaction in posted.Where(t => string.IsNullOrWhiteSpace(t.Description)))
                findings.Add($"Missing description ({transaction.Date:d})");
            foreach (var transaction in transfers.Where(t => string.IsNullOrWhiteSpace(t.Description)))
                findings.Add($"Missing transfer description ({transaction.Date:d})");
        }

        private static void FindUnknownCategories(
            IReadOnlyList<PostedTransaction> posted,
            HashSet<Guid> categoryIds,
            List<string> findings)
        {
            foreach (var transaction in posted)
            {
                if (transaction.HasSplits)
                {
                    foreach (var split in transaction.Splits.Where(s =>
                                 s.CategoryId is Guid id && id != Guid.Empty && !categoryIds.Contains(id)))
                        findings.Add($"Unknown split category on {Describe(transaction)}");
                    continue;
                }

                if (transaction.CategoryId is Guid id && id != Guid.Empty && !categoryIds.Contains(id))
                    findings.Add($"Unknown category on {Describe(transaction)}");
            }
        }

        private static void FindOrphans(
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<PostedTransferTransaction> transfers,
            IReadOnlyList<FutureSingleTransaction> futureSingles,
            IReadOnlyList<FutureTransferTransaction> futureTransfers,
            HashSet<Guid> accountIds,
            List<string> findings)
        {
            foreach (var transaction in posted.Where(t => !accountIds.Contains(t.AccountId)))
                findings.Add($"Orphaned transaction {Describe(transaction)}");
            foreach (var transaction in transfers.Where(t => !accountIds.Contains(t.AccountId)))
                findings.Add($"Orphaned transfer {Describe(transaction)}");
            foreach (var transaction in futureSingles.Where(t => !accountIds.Contains(t.AccountId)))
                findings.Add($"Orphaned future transaction {Describe(transaction)}");
            foreach (var transaction in futureTransfers.Where(t =>
                         !accountIds.Contains(t.FromAccountId) || !accountIds.Contains(t.ToAccountId)))
                findings.Add($"Orphaned future transfer {Describe(transaction)}");
        }

        private static string Describe(BaseTransaction transaction) =>
            $"{transaction.Description} ({transaction.Date:d})";
    }
}
