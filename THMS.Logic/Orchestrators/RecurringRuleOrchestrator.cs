using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class RecurringRuleOrchestrator
    {
        private readonly ITransactionDataStore _store;

        public RecurringRuleOrchestrator()
            : this(new DataStoreFactory().GetTransactionStore())
        {
        }

        public RecurringRuleOrchestrator(ITransactionDataStore store)
        {
            _store = store;
        }

        public List<RecurringSingleTransactionRule> GetSingleRules(Guid accountId) =>
            _store.GetRecurringSingleRules(accountId).ToList();

        public List<RecurringSingleTransactionRule> GetAllSingleRules() =>
            _store.GetAllRecurringSingleRules().ToList();

        public List<RecurringTransferRule> GetTransferRules(Guid accountId) =>
            _store.GetRecurringTransferRules(accountId).ToList();

        public RecurringSingleTransactionRule? GetSingleRule(Guid ruleId) =>
            _store.GetRecurringSingleRule(ruleId);

        public RecurringTransferRule? GetTransferRule(Guid ruleId) =>
            _store.GetRecurringTransferRule(ruleId);

        public IEnumerable<ExpenseCategory> GetCategories()
        {
            _store.EnsureDefaultCategories();
            return _store.GetAllCategories();
        }

        public DateTime? GetNextPaymentDate(Guid accountId)
        {
            var dates = GetAllSingleRules()
                .Where(r => r.IsActive &&
                    (r.AccountId == accountId || r.Splits.Any(s => SplitTransactionMath.IsTransferTo(s, accountId))))
                .Select(r => r.NextOccurrence)
                .Concat(GetTransferRules(accountId).Where(r => r.IsActive).Select(r => r.NextOccurrence))
                .ToList();

            return dates.Count == 0 ? null : dates.Min();
        }

        public void AddSingleRule(RecurringSingleTransactionRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            if (rule.Id == Guid.Empty)
                rule.Id = Guid.NewGuid();
            rule.IsUserCreated = true;
            _store.AddRecurringSingleRule(rule);
        }

        public void UpdateSingleRule(RecurringSingleTransactionRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _store.UpdateRecurringSingleRule(rule);
        }

        public void DeleteSingleRule(Guid ruleId) =>
            _store.DeleteRecurringSingleRule(ruleId);

        public void AddTransferRule(RecurringTransferRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            if (rule.Id == Guid.Empty)
                rule.Id = Guid.NewGuid();
            rule.IsUserCreated = true;
            _store.AddRecurringTransferRule(rule);
        }

        public void UpdateTransferRule(RecurringTransferRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _store.UpdateRecurringTransferRule(rule);
        }

        public void DeleteTransferRule(Guid ruleId) =>
            _store.DeleteRecurringTransferRule(ruleId);
    }
}
