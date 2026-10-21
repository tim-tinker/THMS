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

        public List<ExpenseBudgetRule> GetRules() =>
            _store.GetAllExpenseBudgetRules().ToList();

        public ExpenseBudgetRule? GetRule(Guid ruleId) =>
            _store.GetExpenseBudgetRule(ruleId);

        public IEnumerable<ExpenseCategory> GetCategories()
        {
            _store.EnsureDefaultCategories();
            return _store.GetAllCategories();
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
            if (rule.IsActive)
                RebuildFromPeriodStart(rule);
        }

        public void UpdateRule(ExpenseBudgetRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _store.UpdateExpenseBudgetRule(rule);
            if (rule.IsActive)
                RebuildFromPeriodStart(rule);
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

        public ExpenseBudgetHistory? GetPeriod(Guid historyId) =>
            _store.GetExpenseBudgetHistoryById(historyId);

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
            var fromPeriod = RequireActiveOpenPeriod(fromRuleId);
            var toPeriod = RequireActiveOpenPeriod(toRuleId);
            if (amount > fromPeriod.StartingBalance)
                throw new ArgumentException("Transfer amount cannot exceed the starting balance.");

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
            var created = CreatePeriod(rule, BudgetPeriodCalculator.NextPeriod(lastEnd, rule), previous);
            RefreshPeriod(rule, created);
            return created;
        }

        public void RefreshAllActive()
        {
            foreach (var rule in _store.GetAllExpenseBudgetRules().Where(r => r.IsActive))
                RefreshRule(rule);
        }

        public void RefreshAffected(IEnumerable<PostedTransaction> posted)
        {
            var txs = posted as IList<PostedTransaction> ?? posted.ToList();
            if (txs.Count == 0)
                return;

            var catalog = _store.GetAllCategories(includeInactive: true).ToList();
            foreach (var rule in _store.GetAllExpenseBudgetRules().Where(r => r.IsActive))
            {
                if (!txs.Any(tx => _detector.IncludesTransaction(tx, rule.IncludedCategoryIds, catalog)))
                    continue;
                RefreshRule(rule);
            }
        }

        private void RefreshRule(ExpenseBudgetRule rule)
        {
            var open = EnsureActivePeriod(rule);
            if (open is not null && !open.IsClosed)
                RefreshPeriod(rule, open);
        }

        private void RebuildFromPeriodStart(ExpenseBudgetRule rule)
        {
            var existing = _store.GetExpenseBudgetHistory(rule.Id)
                .OrderBy(h => h.PeriodStart)
                .ThenBy(h => h.PeriodEnd)
                .ToList();
            var anchor = rule.PeriodStart?.Date
                ?? existing.FirstOrDefault()?.PeriodStart.Date
                ?? DateTime.Today;
            var windows = PeriodsFrom(rule, anchor);
            if (windows.Count == 0)
                return;

            var seriesStart = windows[0].Start.Date;
            foreach (var period in existing.Where(h => h.PeriodEnd.Date >= seriesStart))
                _store.DeleteExpenseBudgetHistory(period.Id);

            var previous = existing.LastOrDefault(h => h.PeriodEnd.Date < seriesStart);
            var posted = _store.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var catalog = _store.GetAllCategories(includeInactive: true).ToList();
            var created = new List<ExpenseBudgetHistory>();

            for (var i = 0; i < windows.Count; i++)
            {
                var contiguous = previous is not null &&
                    previous.PeriodEnd.Date.AddDays(1) == windows[i].Start.Date;
                var history = CreatePeriod(rule, windows[i], contiguous ? previous : null);
                history.ActualExpenses = _detector.ComputeActualExpenses(
                    posted,
                    rule.IncludedCategoryIds,
                    history.PeriodStart,
                    history.PeriodEnd,
                    catalog);
                history.IsClosed = i < windows.Count - 1;
                history.RecalculateRemaining();
                _store.UpdateExpenseBudgetHistory(history);
                created.Add(history);
                previous = history;
            }

            if (created.Count == 0)
                return;

            var periods = _store.GetExpenseBudgetHistory(rule.Id)
                .Select(h => created.FirstOrDefault(c => c.Id == h.Id) ?? h)
                .ToList();
            var recommended = _detector.ComputeRecommendedAmount(
                periods,
                rule.BudgetFrequency,
                posted,
                rule.IncludedCategoryIds,
                catalog,
                rule.PeriodStart);
            foreach (var history in created)
            {
                history.RecommendedAmount = recommended;
                history.RecalculateRemaining();
                _store.UpdateExpenseBudgetHistory(history);
            }
        }

        private static List<(DateTime Start, DateTime End)> PeriodsFrom(ExpenseBudgetRule rule, DateTime anchor)
        {
            var windows = new List<(DateTime Start, DateTime End)>();
            var window = BudgetPeriodCalculator.PeriodContaining(anchor, rule);
            var today = DateTime.Today;
            if (window.Start.Date > today)
            {
                windows.Add(window);
                return windows;
            }

            while (windows.Count < 10000)
            {
                windows.Add(window);
                if (window.End.Date >= today)
                    break;

                var next = BudgetPeriodCalculator.NextPeriod(window.End, rule);
                if (next.Start.Date <= window.Start.Date)
                    break;

                window = next;
            }

            return windows;
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
                    BudgetPeriodCalculator.NextPeriod(active.PeriodEnd, rule),
                    previous: active);
            }

            if (active is not null)
                return active;

            var last = _store.GetExpenseBudgetHistory(rule.Id)
                .OrderByDescending(h => h.PeriodEnd)
                .FirstOrDefault();
            if (last is null)
                return CreatePeriod(rule, BudgetPeriodCalculator.PeriodContaining(DateTime.Today, rule));

            if (DateTime.Today > last.PeriodEnd.Date)
                return CreatePeriod(rule, BudgetPeriodCalculator.NextPeriod(last.PeriodEnd, rule), last);

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
            var posted = _store.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var catalog = _store.GetAllCategories(includeInactive: true).ToList();
            RefreshPeriod(rule, history, posted, catalog);
        }

        private void RefreshPeriod(
            ExpenseBudgetRule rule,
            ExpenseBudgetHistory history,
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<ExpenseCategory> catalog)
        {
            history.ActualExpenses = _detector.ComputeActualExpenses(
                posted,
                rule.IncludedCategoryIds,
                history.PeriodStart,
                history.PeriodEnd,
                catalog);

            var periods = _store.GetExpenseBudgetHistory(rule.Id)
                .Select(h => h.Id == history.Id ? history : h)
                .ToList();
            var recommended = _detector.ComputeRecommendedAmount(
                periods,
                rule.BudgetFrequency,
                posted,
                rule.IncludedCategoryIds,
                catalog,
                rule.PeriodStart);
            foreach (var period in periods)
                period.RecommendedAmount = recommended;
            history.RecalculateRemaining();
            foreach (var period in periods)
                _store.UpdateExpenseBudgetHistory(period);
        }
    }
}
