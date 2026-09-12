using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators.Finance
{
    public class SplitDiagnosticsOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;

        public SplitDiagnosticsOrchestrator()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetTransactionStore())
        {
        }

        public SplitDiagnosticsOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
        }

        public List<string> Run()
        {
            var findings = new List<string>();
            var accountIds = _accounts.GetAllAccounts().Select(a => a.Id).ToHashSet();
            var parents = CollectParents();

            foreach (var parent in parents.Where(p => p.HasSplits))
                Inspect(parent, accountIds, findings);

            if (findings.Count == 0)
                findings.Add("No issues found.");

            return findings;
        }

        private List<BaseTransaction> CollectParents()
        {
            var parents = new List<BaseTransaction>();
            parents.AddRange(_transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue));
            parents.AddRange(_transactions.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue));
            parents.AddRange(_transactions.GetAllFutureSingleTransactions());
            parents.AddRange(_transactions.GetAllFutureTransferTransactions());
            parents.AddRange(_transactions.GetAllRecurringSingleRules());
            parents.AddRange(_transactions.GetAllRecurringTransferRules());
            return parents;
        }

        private static void Inspect(BaseTransaction parent, HashSet<Guid> accountIds, List<string> findings)
        {
            var splits = parent.Splits;
            var label = Describe(parent);

            if (splits.Count == 0)
            {
                findings.Add($"Split flag with no rows: {label}");
                return;
            }

            if (!SplitTransactionMath.AmountsMatch(parent.Amount, splits))
            {
                var sum = splits.Sum(s => s.Amount);
                findings.Add($"Split sum mismatch {label}: splits {sum:c2} vs parent {parent.Amount:c2}");
            }

            foreach (var split in splits)
            {
                if (split.Type == SplitType.Transfer)
                {
                    if (split.TransferAccountId is not Guid accountId || accountId == Guid.Empty)
                        findings.Add($"Transfer split missing destination: {label}");
                    else if (!accountIds.Contains(accountId))
                        findings.Add($"Transfer split destination missing: {label}");
                    continue;
                }

                if (SplitTransactionMath.RequiresCategory(split.Type) &&
                    SplitTransactionMath.IsUncategorized(split.CategoryId, split.Category))
                {
                    findings.Add($"Uncategorized {split.Type.ToString().ToLowerInvariant()} split: {label}");
                }
            }
        }

        private static string Describe(BaseTransaction transaction) =>
            $"{transaction.Description} ({transaction.Date:d})";
    }
}
