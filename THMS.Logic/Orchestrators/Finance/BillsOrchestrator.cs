using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class BillsOrchestrator
    {
        public const int MatchDayTolerance = 4;

        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly IAccountStatementDataStore _statements;

        public BillsOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public BillsOrchestrator(
            IAccountDataStore accounts,
            ITransactionDataStore transactions,
            IAccountStatementDataStore statements)
        {
            _accounts = accounts;
            _transactions = transactions;
            _statements = statements;
        }

        public List<BillRow> GetBills(DateTime? asOf = null)
        {
            var accounts = _accounts.GetAllAccounts().ToList();
            var names = accounts.ToDictionary(a => a.Id, a => a.Name);
            var scheduled = _transactions.GetScheduledPaymentIntents().ToList();
            var rows = new List<BillRow>();

            foreach (var intent in scheduled)
                rows.Add(FromIntent(intent, names, BillStatuses.Scheduled));

            var coveredDestinations = new HashSet<Guid>();
            foreach (var statement in accounts
                         .SelectMany(account => _statements.GetForAccount(account.Id))
                         .Where(s => s is not BankStatement)
                         .Where(s => s.AmountDue > 0)
                         .OrderBy(s => s.DueDate))
            {
                coveredDestinations.Add(statement.AccountId);
                if (IsCovered(scheduled, PaymentIntentSource.Statement, statement.Id, statement.AccountId, statement.DueDate.Date))
                    continue;

                rows.Add(new BillRow
                {
                    Source = PaymentIntentSource.Statement,
                    SourceId = statement.Id,
                    StatementId = statement.Id,
                    DestinationAccountId = statement.AccountId,
                    DestinationName = names.GetValueOrDefault(statement.AccountId, ""),
                    FundingAccountId = Guid.Empty,
                    FundingName = "",
                    Kind = BillKinds.Statement,
                    DueDate = statement.DueDate.Date,
                    Amount = Math.Abs(statement.AmountDue),
                    Status = BillStatuses.Due,
                    Notes = statement.Notes ?? ""
                });
            }

            foreach (var rule in _transactions.GetAllRecurringSingleRules()
                         .Where(r => r.IsActive && r.Amount < 0))
            {
                if (IsCovered(scheduled, PaymentIntentSource.RecurringSingle, rule.Id, rule.AccountId, rule.NextOccurrence.Date))
                    continue;

                rows.Add(new BillRow
                {
                    Source = PaymentIntentSource.RecurringSingle,
                    SourceId = rule.Id,
                    DestinationAccountId = rule.AccountId,
                    DestinationName = names.GetValueOrDefault(rule.AccountId, ""),
                    FundingAccountId = rule.AccountId,
                    FundingName = names.GetValueOrDefault(rule.AccountId, ""),
                    Kind = BillKinds.Recurring,
                    DueDate = rule.NextOccurrence.Date,
                    Amount = Math.Abs(rule.Amount),
                    Status = BillStatuses.Due,
                    Notes = rule.Description ?? ""
                });
            }

            foreach (var rule in _transactions.GetAllRecurringTransferRules()
                         .Where(r => r.IsActive))
            {
                if (coveredDestinations.Contains(rule.ToAccountId))
                    continue;
                if (rule.FromAccountId == rule.ToAccountId)
                    continue;
                if (IsCovered(scheduled, PaymentIntentSource.RecurringTransfer, rule.Id, rule.ToAccountId, rule.NextOccurrence.Date))
                    continue;

                rows.Add(new BillRow
                {
                    Source = PaymentIntentSource.RecurringTransfer,
                    SourceId = rule.Id,
                    DestinationAccountId = rule.ToAccountId,
                    DestinationName = names.GetValueOrDefault(rule.ToAccountId, ""),
                    FundingAccountId = rule.FromAccountId,
                    FundingName = names.GetValueOrDefault(rule.FromAccountId, ""),
                    Kind = BillKinds.Transfer,
                    DueDate = rule.NextOccurrence.Date,
                    Amount = Math.Abs(rule.Amount),
                    Status = BillStatuses.Due,
                    Notes = rule.Description ?? ""
                });
            }

            return rows
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.DestinationName)
                .ThenBy(r => r.Notes)
                .ToList();
        }

        public decimal CashRemaining(IEnumerable<BillRow>? pending = null)
        {
            var banks = _accounts.GetAllAccounts().OfType<BankAccount>().ToList();
            var bankIds = banks.Select(b => b.Id).ToHashSet();
            var current = banks.Sum(b => b.PostedBalance);
            var held = _transactions.GetScheduledPaymentIntents()
                .Where(p => bankIds.Contains(p.FundingAccountId))
                .Sum(p => Math.Abs(p.Amount));
            var extra = 0m;
            if (pending is not null)
            {
                extra = pending
                    .Where(r => r.Pay
                        && r.Status == BillStatuses.Due
                        && bankIds.Contains(r.FundingAccountId))
                    .Sum(r => Math.Abs(r.Amount));
            }

            return current - held - extra;
        }

        public IReadOnlyList<Account> GetFundingAccounts() =>
            _accounts.GetAllAccounts()
                .Where(a => a is BankAccount or CreditAccount)
                .OrderBy(a => a.Name)
                .ToList();

        public IReadOnlyList<PayFromChoice> GetPayFromChoices()
        {
            var choices = new List<PayFromChoice> { PayFromChoice.Unspecified };
            choices.AddRange(GetFundingAccounts().Select(PayFromChoice.From));
            return choices;
        }

        public IReadOnlyList<Account> GetAccounts() =>
            _accounts.GetAllAccounts().OrderBy(a => a.Name).ToList();

        public List<PaymentIntent> Schedule(IEnumerable<BillRow> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            var created = new List<PaymentIntent>();
            foreach (var row in rows.Where(r => r.Pay))
            {
                if (row.Status == BillStatuses.Scheduled && row.IntentId is Guid existingId)
                {
                    var existing = _transactions.GetPaymentIntent(existingId);
                    if (existing is null || existing.Status != PaymentIntentStatus.Scheduled)
                        continue;
                    ApplyRow(existing, row);
                    _transactions.UpdatePaymentIntent(existing);
                    created.Add(existing);
                    continue;
                }

                if (row.Amount <= 0)
                    continue;
                if (row.FundingAccountId == Guid.Empty)
                    throw new InvalidOperationException($"Select a pay-from account for {row.DestinationName}.");
                if (row.DestinationAccountId == Guid.Empty)
                    throw new InvalidOperationException("A destination account is required.");

                var scheduled = _transactions.GetScheduledPaymentIntents()
                    .FirstOrDefault(p => SameBill(p, row));
                if (scheduled is not null)
                {
                    ApplyRow(scheduled, row);
                    _transactions.UpdatePaymentIntent(scheduled);
                    created.Add(scheduled);
                    continue;
                }

                var intent = new PaymentIntent();
                ApplyRow(intent, row);
                intent.Status = PaymentIntentStatus.Scheduled;
                _transactions.AddPaymentIntent(intent);
                created.Add(intent);
            }

            return created;
        }

        public void Unschedule(Guid intentId)
        {
            var intent = _transactions.GetPaymentIntent(intentId)
                ?? throw new InvalidOperationException("Scheduled payment was not found.");
            if (intent.Status != PaymentIntentStatus.Scheduled)
                throw new InvalidOperationException("Only scheduled payments can be unscheduled.");
            _transactions.DeletePaymentIntent(intentId);
        }

        public PaymentIntent AddManual(Guid destinationAccountId, Guid fundingAccountId, decimal amount, DateTime payDate, string? notes)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");
            if (fundingAccountId == Guid.Empty)
                throw new InvalidOperationException("Select a pay-from account.");
            if (destinationAccountId == Guid.Empty)
                throw new InvalidOperationException("Select an account.");

            var intent = new PaymentIntent
            {
                Source = PaymentIntentSource.Manual,
                DestinationAccountId = destinationAccountId,
                FundingAccountId = fundingAccountId,
                Amount = amount,
                PayDate = payDate.Date,
                Status = PaymentIntentStatus.Scheduled,
                Description = string.IsNullOrWhiteSpace(notes) ? "Manual" : notes.Trim()
            };
            _transactions.AddPaymentIntent(intent);
            return intent;
        }

        public int MatchScheduled()
        {
            var scheduled = _transactions.GetScheduledPaymentIntents().ToList();
            if (scheduled.Count == 0)
                return 0;

            var posted = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var transfers = _transactions.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var used = scheduled
                .Where(p => p.MatchedPostedTransactionId is Guid)
                .Select(p => p.MatchedPostedTransactionId!.Value)
                .ToHashSet();
            var matched = 0;

            foreach (var intent in scheduled)
            {
                var hit = FindMatch(intent, posted, transfers, used);
                if (hit is null)
                    continue;

                intent.Status = PaymentIntentStatus.Matched;
                intent.MatchedPostedTransactionId = hit.Value.PostedId;
                intent.MatchedCounterpartTransactionId = hit.Value.CounterpartId;
                _transactions.UpdatePaymentIntent(intent);
                used.Add(hit.Value.PostedId);
                AdvanceRule(intent, hit.Value.PostedDate);
                matched++;
            }

            return matched;
        }

        private void AdvanceRule(PaymentIntent intent, DateTime postedDate)
        {
            if (intent.Source == PaymentIntentSource.RecurringSingle && intent.SourceId is Guid singleId)
            {
                var rule = _transactions.GetRecurringSingleRule(singleId);
                if (rule is null || !rule.IsActive)
                    return;
                if (rule.NextOccurrence.Date > postedDate.Date.AddDays(MatchDayTolerance))
                    return;
                rule.LastOccurrence = postedDate.Date;
                rule.NextOccurrence = postedDate.Date.AddFrequency(rule.Frequency);
                _transactions.UpdateRecurringSingleRule(rule);
                return;
            }

            if (intent.Source == PaymentIntentSource.RecurringTransfer && intent.SourceId is Guid transferId)
            {
                var rule = _transactions.GetRecurringTransferRule(transferId);
                if (rule is null || !rule.IsActive)
                    return;
                if (rule.NextOccurrence.Date > postedDate.Date.AddDays(MatchDayTolerance))
                    return;
                rule.LastOccurrence = postedDate.Date;
                rule.NextOccurrence = postedDate.Date.AddFrequency(rule.Frequency);
                _transactions.UpdateRecurringTransferRule(rule);
            }
        }

        private static (Guid PostedId, Guid? CounterpartId, DateTime PostedDate)? FindMatch(
            PaymentIntent intent,
            List<PostedTransaction> posted,
            List<PostedTransferTransaction> transfers,
            HashSet<Guid> used)
        {
            foreach (var tx in transfers.Cast<PostedTransaction>().Concat(posted).OrderBy(t => t.Date))
            {
                if (used.Contains(tx.Id))
                    continue;
                if (!AccountMatches(intent, tx.AccountId))
                    continue;
                if (!AmountsEqual(tx.Amount, intent.Amount))
                    continue;
                if (!DatesClose(tx.Date, intent.PayDate))
                    continue;

                Guid? counterpart = tx is PostedTransferTransaction transfer
                    ? transfer.RelatedPostedTransactionId
                    : null;
                return (tx.Id, counterpart, tx.Date.Date);
            }

            return null;
        }

        private static bool AccountMatches(PaymentIntent intent, Guid accountId) =>
            accountId == intent.FundingAccountId || accountId == intent.DestinationAccountId;

        private static bool AmountsEqual(decimal posted, decimal intent) =>
            Math.Abs(Math.Abs(posted) - Math.Abs(intent)) <= RecurringRulePattern.AmountTolerance;

        private static bool DatesClose(DateTime posted, DateTime payDate) =>
            Math.Abs((posted.Date - payDate.Date).TotalDays) <= MatchDayTolerance;

        private static bool IsCovered(
            IEnumerable<PaymentIntent> scheduled,
            PaymentIntentSource source,
            Guid sourceId,
            Guid destinationId,
            DateTime due) =>
            scheduled.Any(p =>
                (p.Source == source && p.SourceId == sourceId)
                || (p.DestinationAccountId == destinationId && p.PayDate.Date == due.Date));

        private static bool SameBill(PaymentIntent intent, BillRow row)
        {
            if (row.IntentId is Guid id && intent.Id == id)
                return true;
            if (row.SourceId is Guid sourceId && intent.Source == row.Source && intent.SourceId == sourceId)
                return true;
            return intent.DestinationAccountId == row.DestinationAccountId
                && intent.PayDate.Date == row.DueDate.Date
                && intent.Source == row.Source;
        }

        private static void ApplyRow(PaymentIntent intent, BillRow row)
        {
            intent.Source = row.Source;
            intent.SourceId = row.SourceId;
            intent.DestinationAccountId = row.DestinationAccountId;
            intent.FundingAccountId = row.FundingAccountId;
            intent.Amount = Math.Abs(row.Amount);
            intent.PayDate = row.DueDate == default ? DateTime.Today : row.DueDate.Date;
            intent.Description = string.IsNullOrWhiteSpace(row.Notes) ? row.DestinationName : row.Notes.Trim();
            intent.Status = PaymentIntentStatus.Scheduled;
        }

        private static BillRow FromIntent(PaymentIntent intent, Dictionary<Guid, string> names, string status) =>
            new()
            {
                Pay = status == BillStatuses.Scheduled,
                IntentId = intent.Id,
                Source = intent.Source,
                SourceId = intent.SourceId,
                StatementId = intent.Source == PaymentIntentSource.Statement ? intent.SourceId : null,
                DestinationAccountId = intent.DestinationAccountId,
                DestinationName = names.GetValueOrDefault(intent.DestinationAccountId, ""),
                FundingAccountId = intent.FundingAccountId,
                FundingName = names.GetValueOrDefault(intent.FundingAccountId, ""),
                Kind = intent.Source switch
                {
                    PaymentIntentSource.Statement => BillKinds.Statement,
                    PaymentIntentSource.RecurringTransfer => BillKinds.Transfer,
                    PaymentIntentSource.Manual => BillKinds.Manual,
                    _ => BillKinds.Recurring
                },
                DueDate = intent.PayDate.Date,
                Amount = Math.Abs(intent.Amount),
                Status = status,
                Notes = intent.Description ?? ""
            };
    }
}
