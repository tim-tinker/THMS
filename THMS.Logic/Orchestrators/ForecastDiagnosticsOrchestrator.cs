using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;

namespace THMS.Logic.Orchestrators.Finance
{
    public class ForecastDiagnosticsOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly ForecastGenerator _forecastGenerator = new();

        public ForecastDiagnosticsOrchestrator()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetTransactionStore())
        {
        }

        public ForecastDiagnosticsOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
        }

        public List<string> Run()
        {
            var findings = new List<string>();
            var accounts = _accounts.GetAllAccounts().ToList();
            var accountIds = accounts.Select(a => a.Id).ToHashSet();
            var names = accounts.ToDictionary(a => a.Id, a => a.Name);
            var singles = _transactions.GetAllRecurringSingleRules().ToList();
            var transfers = _transactions.GetAllRecurringTransferRules().ToList();
            var today = DateTime.Today;

            FindStaleAndEndedRules(singles, transfers, today, findings);
            FindMissingAccounts(singles, transfers, accountIds, findings);
            FindInvalidTransfers(transfers, findings);
            FindSplitMismatches(singles, transfers, findings);
            FindEmptyForecasts(names, singles, transfers, today, findings);

            if (findings.Count == 0)
                findings.Add("No issues found.");

            return findings;
        }

        private static void FindStaleAndEndedRules(
            IReadOnlyList<RecurringSingleTransactionRule> singles,
            IReadOnlyList<RecurringTransferRule> transfers,
            DateTime today,
            List<string> findings)
        {
            foreach (var rule in singles.Where(r => r.IsActive))
                InspectSchedule(rule.Description, rule.NextOccurrence, rule.EndDate, today, findings);
            foreach (var rule in transfers.Where(r => r.IsActive))
                InspectSchedule(rule.Description, rule.NextOccurrence, rule.EndDate, today, findings);
        }

        private static void InspectSchedule(
            string? description,
            DateTime nextOccurrence,
            DateTime? endDate,
            DateTime today,
            List<string> findings)
        {
            var label = string.IsNullOrWhiteSpace(description) ? "(no description)" : description;
            if (nextOccurrence.Date < today)
                findings.Add($"Stale next occurrence: {label} ({nextOccurrence:d})");
            if (endDate is DateTime end && end.Date < today)
                findings.Add($"Active rule already ended: {label} ({end:d})");
            if (endDate is DateTime stop && nextOccurrence.Date > stop.Date)
                findings.Add($"Rule will never fire: {label} (next {nextOccurrence:d} after end {stop:d})");
        }

        private static void FindMissingAccounts(
            IReadOnlyList<RecurringSingleTransactionRule> singles,
            IReadOnlyList<RecurringTransferRule> transfers,
            HashSet<Guid> accountIds,
            List<string> findings)
        {
            foreach (var rule in singles.Where(r => !accountIds.Contains(r.AccountId)))
                findings.Add($"Recurring rule account missing: {Describe(rule)}");
            foreach (var rule in transfers.Where(r =>
                         !accountIds.Contains(r.FromAccountId) || !accountIds.Contains(r.ToAccountId)))
                findings.Add($"Recurring transfer account missing: {Describe(rule)}");
        }

        private static void FindInvalidTransfers(IReadOnlyList<RecurringTransferRule> transfers, List<string> findings)
        {
            foreach (var rule in transfers)
            {
                if (rule.FromAccountId == Guid.Empty || rule.ToAccountId == Guid.Empty)
                    findings.Add($"Recurring transfer missing from/to account: {Describe(rule)}");
                else if (rule.FromAccountId == rule.ToAccountId)
                    findings.Add($"Recurring transfer from and to are the same: {Describe(rule)}");
            }
        }

        private static void FindSplitMismatches(
            IReadOnlyList<RecurringSingleTransactionRule> singles,
            IReadOnlyList<RecurringTransferRule> transfers,
            List<string> findings)
        {
            foreach (var rule in singles.Where(r => r.HasSplits && !SplitTransactionMath.AmountsMatch(r.Amount, r.Splits)))
                findings.Add($"Recurring split sum mismatch: {Describe(rule)}");
            foreach (var rule in transfers.Where(r => r.HasSplits && !SplitTransactionMath.AmountsMatch(r.Amount, r.Splits)))
                findings.Add($"Recurring transfer split sum mismatch: {Describe(rule)}");
        }

        private void FindEmptyForecasts(
            IReadOnlyDictionary<Guid, string> names,
            IReadOnlyList<RecurringSingleTransactionRule> singles,
            IReadOnlyList<RecurringTransferRule> transfers,
            DateTime today,
            List<string> findings)
        {
            var from = today;
            var to = today.AddYears(1);
            foreach (var (accountId, name) in names)
            {
                var hasActive = singles.Any(r => r.IsActive && r.AccountId == accountId) ||
                                transfers.Any(r => r.IsActive &&
                                    (r.FromAccountId == accountId || r.ToAccountId == accountId));
                if (!hasActive)
                    continue;

                var forecast = _forecastGenerator.GenerateForecast(accountId, from, to, singles, transfers);
                if (forecast.Count == 0)
                    findings.Add($"Active recurring rules produce no forecast in the next year: {name}");
            }
        }

        private static string Describe(BaseTransaction transaction) =>
            $"{transaction.Description} ({transaction.Date:d})";
    }
}
