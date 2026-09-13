using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Budget;

namespace THMS.Logic.Orchestrators
{
    public class BudgetOrchestrator
    {
        private readonly ITransactionDataStore _store;
        private readonly ExpenseBudgetDetector _detector = new();

        public BudgetOrchestrator()
            : this(new DataStoreFactory().GetTransactionStore())
        {
        }

        public BudgetOrchestrator(ITransactionDataStore store)
        {
            _store = store;
        }

        public List<ExpenseBudgetRule> GetRules(Guid accountId) =>
            _store.GetExpenseBudgetRules(accountId).ToList();

        public ExpenseBudgetRule? GetRule(Guid ruleId) =>
            _store.GetExpenseBudgetRule(ruleId);

        public IEnumerable<ExpenseCategory> GetCategories()
        {
            _store.EnsureDefaultCategories();
            return _store.GetAllCategories();
        }

        public int EnsureSuggestedRules(Guid accountId)
        {
            if (_store.GetExpenseBudgetRules(accountId).Any())
                return 0;

            _store.EnsureDefaultCategories();
            var latest = _store.GetLatestPostedTransactionDate(accountId);
            if (latest is null)
                return 0;

            var start = latest.Value.Date.AddMonths(-13);
            var posted = _store.GetPostedTransactions(accountId, start, latest.Value.Date).ToList();
            if (posted.Count == 0)
                return 0;

            var catalog = _store.GetAllCategories(includeInactive: true).ToList();
            var suggestions = new List<(Guid CategoryId, string Name, decimal Amount)>();
            foreach (var (categoryId, name) in CollectBudgetCategories(posted, catalog))
            {
                if (categoryId == DefaultExpenseCategories.PaymentId)
                    continue;

                var recommended = _detector.ComputeRecommendedAmount(
                    [],
                    BudgetFrequency.Monthly,
                    posted,
                    [categoryId],
                    catalog);
                if (recommended < 15m)
                    continue;

                suggestions.Add((categoryId, name, recommended));
            }

            var created = 0;
            foreach (var suggestion in suggestions.OrderByDescending(s => s.Amount).Take(20))
            {
                _store.AddExpenseBudgetRule(new ExpenseBudgetRule
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    BudgetName = suggestion.Name,
                    IncludedCategoryIds = [suggestion.CategoryId],
                    BudgetFrequency = BudgetFrequency.Monthly,
                    DefaultBudgetAmount = suggestion.Amount,
                    IsActive = true
                });
                created++;
            }

            if (created > 0)
                RefreshAccount(accountId);

            return created;
        }

        private static Dictionary<Guid, string> CollectBudgetCategories(
            IEnumerable<PostedTransaction> posted,
            IReadOnlyList<ExpenseCategory> catalog)
        {
            var result = new Dictionary<Guid, string>();
            foreach (var transaction in posted)
            {
                if (transaction.HasSplits)
                {
                    foreach (var split in transaction.Splits)
                    {
                        if (!SplitTransactionMath.AffectsBudget(split.Type))
                            continue;
                        TryAddCategory(result, split.CategoryId, split.Category, catalog);
                    }

                    continue;
                }

                TryAddCategory(result, transaction.CategoryId, transaction.Category, catalog);
            }

            return result;
        }

        private static void TryAddCategory(
            Dictionary<Guid, string> result,
            Guid? categoryId,
            string? category,
            IReadOnlyList<ExpenseCategory> catalog)
        {
            Guid resolved;
            if (categoryId is Guid id && id != Guid.Empty)
            {
                resolved = id;
            }
            else
            {
                var canonical = DefaultExpenseCategories.CanonicalName(category);
                var named = catalog.FirstOrDefault(c =>
                    string.Equals(c.Name, canonical, StringComparison.OrdinalIgnoreCase));
                if (named is null)
                    return;
                resolved = named.Id;
            }

            if (result.ContainsKey(resolved))
                return;

            result[resolved] = catalog.FirstOrDefault(c => c.Id == resolved)?.Name
                ?? DefaultExpenseCategories.CanonicalName(category);
        }

        public void AddRule(ExpenseBudgetRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            if (rule.Id == Guid.Empty)
                rule.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(rule.BudgetName))
                throw new ArgumentException("Budget name is required.");
            if (rule.IncludedCategoryIds.Count == 0)
                throw new ArgumentException("At least one category is required.");

            _store.AddExpenseBudgetRule(rule);
            RefreshRule(rule);
        }

        public void UpdateRule(ExpenseBudgetRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _store.UpdateExpenseBudgetRule(rule);
            if (rule.IsActive)
                RefreshRule(rule);
        }

        public void DeleteRule(Guid ruleId) =>
            _store.DeleteExpenseBudgetRule(ruleId);

        public void SetActive(Guid ruleId, bool isActive)
        {
            var rule = _store.GetExpenseBudgetRule(ruleId)
                ?? throw new InvalidOperationException($"Budget rule {ruleId} was not found.");
            rule.IsActive = isActive;
            _store.UpdateExpenseBudgetRule(rule);
            if (isActive)
                RefreshRule(rule);
        }

        public ExpenseBudgetHistory? GetActivePeriod(Guid ruleId) =>
            _store.GetActiveBudgetHistory(ruleId);

        public List<ExpenseBudgetHistory> GetHistory(Guid ruleId) =>
            _store.GetExpenseBudgetHistory(ruleId).ToList();

        public void SavePeriod(ExpenseBudgetHistory history)
        {
            ArgumentNullException.ThrowIfNull(history);
            history.RecalculateRemaining();
            _store.UpdateExpenseBudgetHistory(history);
        }

        public void SetStartingBalance(Guid historyId, decimal startingBalance)
        {
            var history = RequireOpenPeriod(historyId);
            history.StartingBalance = startingBalance;
            history.RecalculateRemaining();
            _store.UpdateExpenseBudgetHistory(history);
        }

        public void TransferBalance(Guid fromRuleId, Guid toRuleId, decimal amount)
        {
            if (fromRuleId == toRuleId)
                throw new ArgumentException("Choose two different budgets.");
            if (amount <= 0)
                throw new ArgumentException("Transfer amount must be greater than zero.");

            var fromRule = _store.GetExpenseBudgetRule(fromRuleId)
                ?? throw new InvalidOperationException($"Budget rule {fromRuleId} was not found.");
            var toRule = _store.GetExpenseBudgetRule(toRuleId)
                ?? throw new InvalidOperationException($"Budget rule {toRuleId} was not found.");
            if (fromRule.AccountId != toRule.AccountId)
                throw new InvalidOperationException("Budgets must belong to the same account.");

            var fromPeriod = RequireActiveOpenPeriod(fromRuleId);
            var toPeriod = RequireActiveOpenPeriod(toRuleId);

            fromPeriod.StartingBalance -= amount;
            toPeriod.StartingBalance += amount;
            fromPeriod.RecalculateRemaining();
            toPeriod.RecalculateRemaining();
            _store.UpdateExpenseBudgetHistory(fromPeriod);
            _store.UpdateExpenseBudgetHistory(toPeriod);
        }

        private ExpenseBudgetHistory RequireActiveOpenPeriod(Guid ruleId)
        {
            var period = _store.GetActiveBudgetHistory(ruleId);
            if (period is null || period.IsClosed)
                throw new InvalidOperationException("Both budgets need an open period.");
            return period;
        }

        private ExpenseBudgetHistory RequireOpenPeriod(Guid historyId)
        {
            var history = _store.GetExpenseBudgetHistoryById(historyId)
                ?? throw new InvalidOperationException($"Budget period {historyId} was not found.");
            if (history.IsClosed)
                throw new InvalidOperationException("Closed periods cannot change starting balance.");
            return history;
        }

        public void ClosePeriod(Guid historyId)
        {
            var history = _store.GetExpenseBudgetHistoryById(historyId);
            if (history is null)
                return;

            var rule = _store.GetExpenseBudgetRule(history.BudgetRuleId);
            if (rule is not null)
                RefreshPeriod(rule, history);

            history.IsClosed = true;
            history.RecalculateRemaining();
            _store.UpdateExpenseBudgetHistory(history);
        }

        public ExpenseBudgetHistory RollForward(Guid ruleId)
        {
            var rule = _store.GetExpenseBudgetRule(ruleId)
                ?? throw new InvalidOperationException($"Budget rule {ruleId} was not found.");

            var active = _store.GetActiveBudgetHistory(ruleId);
            ExpenseBudgetHistory? previous = active;
            if (active is not null)
            {
                RefreshPeriod(rule, active);
                active.IsClosed = true;
                active.RecalculateRemaining();
                _store.UpdateExpenseBudgetHistory(active);
                previous = active;
            }

            var lastEnd = previous?.PeriodEnd
                ?? _store.GetExpenseBudgetHistory(ruleId).Select(h => h.PeriodEnd).DefaultIfEmpty(DateTime.Today.AddDays(-1)).Max();
            var created = CreatePeriod(rule, BudgetPeriodCalculator.NextPeriod(lastEnd, rule.BudgetFrequency), previous);
            RefreshPeriod(rule, created);
            return created;
        }

        public void RefreshAccount(Guid accountId)
        {
            foreach (var rule in _store.GetExpenseBudgetRules(accountId).Where(r => r.IsActive))
                RefreshRule(rule);
        }

        public void RefreshAllActive()
        {
            foreach (var rule in _store.GetAllExpenseBudgetRules().Where(r => r.IsActive))
                RefreshRule(rule);
        }

        private void RefreshRule(ExpenseBudgetRule rule)
        {
            var period = EnsureActivePeriod(rule);
            if (period is not null && !period.IsClosed)
                RefreshPeriod(rule, period);
        }

        private ExpenseBudgetHistory? EnsureActivePeriod(ExpenseBudgetRule rule)
        {
            var active = _store.GetActiveBudgetHistory(rule.Id);
            while (active is not null && DateTime.Today > active.PeriodEnd.Date)
            {
                RefreshPeriod(rule, active);
                active.IsClosed = true;
                active.RecalculateRemaining();
                _store.UpdateExpenseBudgetHistory(active);
                active = CreatePeriod(
                    rule,
                    BudgetPeriodCalculator.NextPeriod(active.PeriodEnd, rule.BudgetFrequency),
                    previous: active);
            }

            if (active is not null)
                return active;

            var last = _store.GetExpenseBudgetHistory(rule.Id)
                .OrderByDescending(h => h.PeriodEnd)
                .FirstOrDefault();
            if (last is null)
                return CreatePeriod(rule, BudgetPeriodCalculator.PeriodContaining(DateTime.Today, rule.BudgetFrequency));

            if (DateTime.Today > last.PeriodEnd.Date)
                return CreatePeriod(rule, BudgetPeriodCalculator.NextPeriod(last.PeriodEnd, rule.BudgetFrequency), last);

            return null;
        }

        private ExpenseBudgetHistory CreatePeriod(
            ExpenseBudgetRule rule,
            (DateTime Start, DateTime End) window,
            ExpenseBudgetHistory? previous = null)
        {
            var history = new ExpenseBudgetHistory
            {
                Id = Guid.NewGuid(),
                BudgetRuleId = rule.Id,
                PeriodStart = window.Start,
                PeriodEnd = window.End,
                StartingBalance = previous?.EndingBalance ?? 0,
                BudgetAmount = Math.Abs(rule.DefaultBudgetAmount),
                ActualExpenses = 0,
                IsClosed = false
            };
            history.RecalculateRemaining();
            _store.AddExpenseBudgetHistory(history);
            return history;
        }

        private void RefreshPeriod(ExpenseBudgetRule rule, ExpenseBudgetHistory history)
        {
            var posted = _store.GetPostedTransactions(rule.AccountId).ToList();
            var catalog = _store.GetAllCategories(includeInactive: true).ToList();
            history.ActualExpenses = _detector.ComputeActualExpenses(
                posted,
                rule.IncludedCategoryIds,
                history.PeriodStart,
                history.PeriodEnd,
                catalog);

            var periods = _store.GetExpenseBudgetHistory(rule.Id)
                .Select(h => h.Id == history.Id ? history : h)
                .ToList();
            history.RecommendedAmount = _detector.ComputeRecommendedAmount(
                periods,
                rule.BudgetFrequency,
                posted,
                rule.IncludedCategoryIds,
                catalog);
            history.RecalculateRemaining();
            _store.UpdateExpenseBudgetHistory(history);
        }
    }
}
