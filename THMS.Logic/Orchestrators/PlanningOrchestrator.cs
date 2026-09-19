using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Planning;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class PlanningOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly IAccountStatementDataStore _statements;
        private readonly ForecastGenerator _forecastGenerator = new();

        public PlanningOrchestrator()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public PlanningOrchestrator(
            IAccountDataStore accounts,
            ITransactionDataStore transactions,
            IAccountStatementDataStore statements)
        {
            _accounts = accounts;
            _transactions = transactions;
            _statements = statements;
        }

        public List<UpcomingObligation> GetUpcomingObligations(DateTime asOf)
        {
            var asOfDate = asOf.Date;
            var accounts = _accounts.GetAllAccounts().ToList();
            var names = accounts.ToDictionary(a => a.Id, a => a.Name);
            var statements = _statements.GetUpcoming(asOfDate);
            var covered = new HashSet<Guid>();
            var rows = new List<UpcomingObligation>();

            foreach (var statement in statements
                         .Where(s => s is not BankStatement)
                         .Where(s => s.DueDate.Date >= asOfDate)
                         .OrderBy(s => s.DueDate))
            {
                covered.Add(statement.AccountId);
                var promotions = Promotions(statement);
                var promoDue = promotions
                    .Where(p => p.Deadline.Date >= asOfDate)
                    .Sum(p => p.Amount);
                rows.Add(new UpcomingObligation
                {
                    AccountId = statement.AccountId,
                    StatementId = statement.Id,
                    AccountName = names.GetValueOrDefault(statement.AccountId, ""),
                    DueDate = statement.DueDate.Date,
                    AmountDue = statement.AmountDue,
                    PromotionalDue = promoDue,
                    Notes = ObligationNote(statement, promoDue)
                });
            }

            foreach (var account in accounts.Where(a => !covered.Contains(a.Id)))
            {
                var synthesized = SynthesizeObligation(account, asOfDate);
                if (synthesized is not null)
                {
                    covered.Add(account.Id);
                    rows.Add(synthesized);
                }
            }

            AddRecurringForecastObligations(asOfDate, accounts, names, covered, rows);

            return rows.OrderBy(r => r.DueDate).ThenBy(r => r.AccountName).ToList();
        }

        public List<PlannedPaymentView> GetPlannedPaymentViews()
        {
            var names = _accounts.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);
            var singles = _transactions.GetAllPlannedPayments()
                .Where(p => !p.IsRealized)
                .Select(p => new PlannedPaymentView
                {
                    Id = p.Id,
                    AccountId = p.AccountId,
                    AccountName = names.GetValueOrDefault(p.AccountId, ""),
                    PlannedAmount = p.Amount,
                    PlannedDate = p.Date.Date,
                    Notes = p.PlanningNote ?? "",
                    IsRealized = p.IsRealized,
                    StatementId = p.StatementId
                });
            var transfers = _transactions.GetAllPlannedTransfers()
                .Where(p => !p.IsRealized)
                .Select(p => new PlannedPaymentView
                {
                    Id = p.Id,
                    AccountId = p.FromAccountId,
                    AccountName =
                        $"{names.GetValueOrDefault(p.FromAccountId, "")} → {names.GetValueOrDefault(p.ToAccountId, "")}",
                    PlannedAmount = Math.Abs(p.Amount),
                    PlannedDate = p.Date.Date,
                    Notes = p.PlanningNote ?? "",
                    IsRealized = p.IsRealized,
                    StatementId = p.StatementId
                });
            return singles.Concat(transfers)
                .OrderBy(p => p.PlannedDate)
                .ThenBy(p => p.AccountName)
                .ToList();
        }

        public IReadOnlyList<Account> GetFundingAccounts(Guid? excludeAccountId = null) =>
            _accounts.GetAllAccounts()
                .Where(a => a is BankAccount or CreditAccount)
                .Where(a => excludeAccountId is null || a.Id != excludeAccountId)
                .OrderBy(a => a.Name)
                .ToList();

        public Guid? FundingAccountForStatement(Guid statementId)
        {
            var transfer = _transactions.GetAllPlannedTransfers()
                .FirstOrDefault(t => !t.IsRealized && t.StatementId == statementId);
            if (transfer is not null)
                return transfer.FromAccountId;

            var single = _transactions.GetAllPlannedPayments()
                .FirstOrDefault(t => !t.IsRealized && t.StatementId == statementId);
            return single?.AccountId;
        }

        public void EnsureStatementPayment(AccountStatement statement, Guid? fundingAccountId)
        {
            ArgumentNullException.ThrowIfNull(statement);
            if (statement is BankStatement || statement.AmountDue <= 0)
            {
                RemovePlannedForStatement(statement.Id);
                return;
            }

            if (fundingAccountId is not Guid fromId || fromId == Guid.Empty)
                throw new InvalidOperationException("Select the account to pay this statement from.");

            var funding = RequireFundingAccount(fromId);
            if (funding.Id == statement.AccountId)
                throw new InvalidOperationException("Pay-from account must be different from the statement account.");

            var obligation = RequireAccount(statement.AccountId);
            RemovePlannedForStatement(statement.Id);

            var note = "Statement payment";
            var date = statement.DueDate.Date;
            if (obligation is CreditAccount or LoanAccount or MortgageAccount)
            {
                _transactions.AddFutureTransferTransaction(new FutureTransferTransaction
                {
                    FromAccountId = funding.Id,
                    ToAccountId = obligation.Id,
                    Date = date,
                    Description = $"{note}: {obligation.Name}",
                    Amount = Math.Abs(statement.AmountDue),
                    Category = DefaultExpenseCategories.Payment,
                    CategoryId = DefaultExpenseCategories.PaymentId,
                    IsUserCreated = true,
                    IsPlannedPayment = true,
                    StatementId = statement.Id,
                    PlanningNote = note
                });
                return;
            }

            _transactions.AddFutureSingleTransaction(new FutureSingleTransaction
            {
                AccountId = funding.Id,
                Date = date,
                Description = $"{note}: {obligation.Name}",
                Amount = -Math.Abs(statement.AmountDue),
                Category = DefaultExpenseCategories.Payment,
                CategoryId = DefaultExpenseCategories.PaymentId,
                IsUserCreated = true,
                IsPlannedPayment = true,
                StatementId = statement.Id,
                PlanningNote = note
            });
        }

        public FutureSingleTransaction AddPlannedPayment(
            Guid accountId,
            decimal displayAmount,
            DateTime date,
            string? note,
            Guid? statementId = null,
            Guid? promotionalBalanceId = null)
        {
            var account = RequireAccount(accountId);
            if (displayAmount <= 0)
                throw new InvalidOperationException("Planned payment amount must be greater than zero.");
            if (date == default)
                throw new InvalidOperationException("Planned payment date is required.");
            var planned = NewPlannedPayment(
                account,
                displayAmount,
                date.Date,
                note ?? "Planned payment",
                statementId,
                promotionalBalanceId);
            _transactions.AddFutureSingleTransaction(planned);
            return planned;
        }

        public void DeletePlannedPayment(Guid futureTransactionId)
        {
            var planned = _transactions.GetFutureSingleTransaction(futureTransactionId);
            if (planned is not null)
            {
                if (planned.IsRealized)
                    throw new InvalidOperationException("A realized planned payment cannot be deleted.");
                _transactions.DeleteFutureSingleTransaction(futureTransactionId);
                return;
            }

            var transfer = _transactions.GetFutureTransferTransaction(futureTransactionId)
                ?? throw new InvalidOperationException("Planned payment was not found.");
            if (transfer.IsRealized)
                throw new InvalidOperationException("A realized planned payment cannot be deleted.");
            _transactions.DeleteFutureTransferTransaction(futureTransactionId);
        }

        public List<FutureSingleTransaction> GeneratePayAllDue(DateTime until) =>
            GenerateFromObligations(until, obligation => obligation.AmountDue, "Full Payment");

        public IAccountStatementDataStore GetStatementStore() => _statements;

        public AccountStatement? GetStatement(Guid id) => _statements.Get(id);

        public List<AccountStatement> GetAllStatements() =>
            _accounts.GetAllAccounts()
                .SelectMany(account => _statements.GetForAccount(account.Id))
                .OrderByDescending(s => s.StatementDate)
                .ThenBy(s => s.Type.ToString())
                .ToList();

        public List<AccountStatementListRow> GetStatementListRows(Guid accountId)
        {
            return _statements.GetForAccount(accountId)
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.DueDate)
                .Select(AccountStatementListRow.From)
                .ToList();
        }

        public void DeleteStatement(Guid id)
        {
            if (_statements.Get(id) is null)
                throw new InvalidOperationException("Statement was not found.");
            RemovePlannedForStatement(id);
            _statements.Delete(id);
        }

        public void SaveStatement(AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);
            AccountStatementValidator.EnsureValid(statement);
            _statements.Save(statement);
        }

        public List<FutureSingleTransaction> GeneratePromotionPayments(DateTime until)
        {
            RequireUntil(until);
            var untilDate = until.Date;
            var created = new List<FutureSingleTransaction>();
            var existing = _transactions.GetAllPlannedPayments()
                .Where(p => !p.IsRealized && p.PromotionalBalanceId is not null)
                .Select(p => p.PromotionalBalanceId!.Value)
                .ToHashSet();

            foreach (var statement in _statements.GetUpcoming(DateTime.Today).OfType<CreditCardStatement>())
            {
                var account = RequireAccount(statement.AccountId);
                foreach (var promo in statement.Promotions.Where(p => p.Deadline.Date <= untilDate && p.CurrentBalance > 0))
                {
                    if (existing.Contains(promo.Id))
                        continue;

                    foreach (var installment in PromotionInstallments(promo, untilDate))
                    {
                        var planned = NewPlannedPayment(
                            account,
                            installment.Amount,
                            installment.Date,
                            $"Promotion ({promo.Type})",
                            statement.Id,
                            promo.Id);
                        _transactions.AddFutureSingleTransaction(planned);
                        created.Add(planned);
                    }
                }
            }

            return created;
        }

        public List<FutureSingleTransaction> GenerateExtraPrincipalPayments(decimal extraAmount)
        {
            RequirePositiveAmount(extraAmount, "Extra principal");

            var created = new List<FutureSingleTransaction>();
            var note = "Extra Principal";
            foreach (var target in _accounts.GetAllAccounts()
                         .Where(a => a is LoanAccount or MortgageAccount)
                         .OrderByDescending(InterestRate))
            {
                var due = NextDueDate(target, DateTime.Today) ?? DateTime.Today.AddDays(7);
                var existing = _transactions.GetPlannedPayments(target.Id)
                    .Where(p => !p.IsRealized && string.Equals(p.PlanningNote, note, StringComparison.OrdinalIgnoreCase));
                foreach (var duplicate in existing.ToList())
                    _transactions.DeleteFutureSingleTransaction(duplicate.Id);

                var planned = NewPlannedPayment(target, extraAmount, due, note);
                _transactions.AddFutureSingleTransaction(planned);
                created.Add(planned);
            }

            return created;
        }

        public CashFlowForecast ComputeCashFlow(DateTime until)
        {
            RequireUntil(until);
            var asOf = DateTime.Today;
            var untilDate = until.Date;
            var accounts = _accounts.GetAllAccounts().ToList();
            var bankIds = accounts.OfType<BankAccount>().Select(a => a.Id).ToHashSet();
            var current = accounts.OfType<BankAccount>().Sum(b => b.PostedBalance);
            var paydays = ResolvePaydays(asOf, accounts);
            var nextPayday = paydays.Count > 0 ? paydays[0] : (DateTime?)null;
            var secondPayday = paydays.Count > 1 ? paydays[1] : nextPayday?.AddDays(14);

            return new CashFlowForecast
            {
                CurrentBalance = current,
                ForecastedBalanceNextPayday = ForecastAt(current, asOf, nextPayday ?? untilDate, accounts, bankIds, includePlanned: true),
                ForecastedBalanceTwoPaydays = ForecastAt(current, asOf, secondPayday ?? untilDate, accounts, bankIds, includePlanned: true),
                ForecastedBalanceAfterPlanned = ForecastAt(current, asOf, untilDate, accounts, bankIds, includePlanned: true),
                NextPayday = nextPayday,
                SecondPayday = secondPayday
            };
        }

        public int CommitPlannedPayments(List<FutureSingleTransaction> planned)
        {
            ArgumentNullException.ThrowIfNull(planned);
            var today = DateTime.Today;
            var committed = 0;
            foreach (var item in planned)
            {
                var stored = _transactions.GetFutureSingleTransaction(item.Id) ?? item;
                if (stored.IsRealized)
                    continue;
                if (stored.Date.Date > today)
                    continue;

                Realize(stored, stored.Date.Date);
                committed++;
            }

            return committed;
        }

        public int CommitDuePlannedPayments()
        {
            var committed = CommitPlannedPayments(
                _transactions.GetAllPlannedPayments().Where(p => !p.IsRealized).ToList());
            var today = DateTime.Today;
            foreach (var transfer in _transactions.GetAllPlannedTransfers().Where(t => !t.IsRealized).ToList())
            {
                if (transfer.Date.Date > today)
                    continue;
                Realize(transfer, transfer.Date.Date);
                committed++;
            }

            return committed;
        }

        public int ReconcileManualPayment(Guid futureTransactionId, DateTime postedDate)
        {
            if (futureTransactionId == Guid.Empty)
                throw new InvalidOperationException("A planned payment is required.");
            if (postedDate == default)
                throw new InvalidOperationException("A posted date is required.");

            var planned = _transactions.GetFutureSingleTransaction(futureTransactionId)
                ?? throw new InvalidOperationException("Planned payment was not found.");
            if (planned.IsRealized)
                throw new InvalidOperationException("That planned payment is already realized.");

            Realize(planned, postedDate.Date);
            return 1;
        }

        public IReadOnlyList<Account> GetAccounts() => _accounts.GetAllAccounts().ToList();

        public DateTime SuggestPlanUntil()
        {
            var paydays = ResolvePaydays(DateTime.Today, _accounts.GetAllAccounts().ToList());
            if (paydays.Count >= 1)
                return paydays[0];
            return DateTime.Today.AddDays(14);
        }

        private List<FutureSingleTransaction> GenerateFromObligations(
            DateTime until,
            Func<UpcomingObligation, decimal> amountSelector,
            string note)
        {
            RequireUntil(until);
            var untilDate = until.Date;
            var created = new List<FutureSingleTransaction>();
            var existing = _transactions.GetAllPlannedPayments()
                .Where(p => !p.IsRealized && string.Equals(p.PlanningNote, note, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.StatementId ?? p.AccountId)
                .Concat(StatementPaymentKeys())
                .ToHashSet();

            foreach (var obligation in GetUpcomingObligations(DateTime.Today).Where(o => o.DueDate <= untilDate))
            {
                var amount = amountSelector(obligation);
                if (amount <= 0)
                    continue;

                var key = obligation.StatementId ?? obligation.AccountId;
                if (existing.Contains(key))
                    continue;

                var account = RequireAccount(obligation.AccountId);
                var planned = NewPlannedPayment(
                    account,
                    amount,
                    obligation.DueDate,
                    note,
                    obligation.StatementId);
                _transactions.AddFutureSingleTransaction(planned);
                created.Add(planned);
            }

            return created;
        }

        private void Realize(FutureSingleTransaction planned, DateTime postedDate)
        {
            var posted = new PostedTransaction
            {
                AccountId = planned.AccountId,
                Date = postedDate,
                Description = planned.Description,
                Amount = planned.Amount,
                Category = planned.Category,
                CategoryId = planned.CategoryId
            };

            _transactions.AddPostedTransaction(posted);
            planned.IsRealized = true;
            planned.PostedTransactionId = posted.Id;
            _transactions.DeleteFutureSingleTransaction(planned.Id);
        }

        private void Realize(FutureTransferTransaction planned, DateTime postedDate)
        {
            var from = RequireAccount(planned.FromAccountId);
            var to = RequireAccount(planned.ToAccountId);
            var display = Math.Abs(planned.Amount);
            var outgoingId = Guid.NewGuid();
            var incomingId = Guid.NewGuid();
            var incomingAmount = ToLedgerPayment(to, display);
            var incoming = new PostedTransaction
            {
                Id = incomingId,
                AccountId = to.Id,
                Date = postedDate,
                Description = planned.Description,
                Amount = incomingAmount,
                Category = planned.Category,
                CategoryId = planned.CategoryId
            };

            var outgoing = new PostedTransferTransaction(
                new PostedTransaction
                {
                    Id = outgoingId,
                    AccountId = from.Id,
                    Date = postedDate,
                    Description = planned.Description,
                    Amount = -display,
                    Category = planned.Category,
                    CategoryId = planned.CategoryId
                },
                incomingId,
                TransferDirection.Outgoing);
            var incomingTransfer = new PostedTransferTransaction(incoming, outgoingId, TransferDirection.Incoming);
            incomingTransfer.Splits = incoming.Splits;
            _transactions.AddPostedTransferTransaction(outgoing);
            _transactions.AddPostedTransferTransaction(incomingTransfer);
            _transactions.DeleteFutureTransferTransaction(planned.Id);
        }

        private void RemovePlannedForStatement(Guid statementId)
        {
            foreach (var planned in _transactions.GetAllPlannedPayments()
                         .Where(p => p.StatementId == statementId && !p.IsRealized)
                         .ToList())
                _transactions.DeleteFutureSingleTransaction(planned.Id);

            foreach (var planned in _transactions.GetAllPlannedTransfers()
                         .Where(p => p.StatementId == statementId && !p.IsRealized)
                         .ToList())
                _transactions.DeleteFutureTransferTransaction(planned.Id);
        }

        private HashSet<Guid> StatementPaymentKeys() =>
            _transactions.GetAllPlannedPayments()
                .Where(p => !p.IsRealized && p.StatementId is not null &&
                            string.Equals(p.PlanningNote, "Statement payment", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.StatementId!.Value)
                .Concat(
                    _transactions.GetAllPlannedTransfers()
                        .Where(p => !p.IsRealized && p.StatementId is not null &&
                                    string.Equals(p.PlanningNote, "Statement payment", StringComparison.OrdinalIgnoreCase))
                        .Select(p => p.StatementId!.Value))
                .ToHashSet();

        private HashSet<Guid> PlannedStatementKeys() =>
            _transactions.GetAllPlannedPayments()
                .Where(p => !p.IsRealized && p.StatementId is not null)
                .Select(p => p.StatementId!.Value)
                .Concat(
                    _transactions.GetAllPlannedTransfers()
                        .Where(p => !p.IsRealized && p.StatementId is not null)
                        .Select(p => p.StatementId!.Value))
                .ToHashSet();

        private Account RequireFundingAccount(Guid accountId)
        {
            var account = RequireAccount(accountId);
            if (account is not BankAccount and not CreditAccount)
                throw new InvalidOperationException("Pay-from account must be a bank or credit account.");
            return account;
        }

        private FutureSingleTransaction NewPlannedPayment(
            Account account,
            decimal displayAmount,
            DateTime date,
            string note,
            Guid? statementId = null,
            Guid? promotionalBalanceId = null)
        {
            return new FutureSingleTransaction
            {
                AccountId = account.Id,
                Date = date.Date,
                Description = $"{note}: {account.Name}",
                Amount = ToLedgerPayment(account, displayAmount),
                Category = DefaultExpenseCategories.Payment,
                CategoryId = DefaultExpenseCategories.PaymentId,
                IsUserCreated = true,
                IsPlannedPayment = true,
                StatementId = statementId,
                PromotionalBalanceId = promotionalBalanceId,
                PlanningNote = note
            };
        }

        private decimal ForecastAt(
            decimal starting,
            DateTime asOf,
            DateTime until,
            IReadOnlyList<Account> accounts,
            HashSet<Guid> bankIds,
            bool includePlanned)
        {
            if (until < asOf)
                return starting;

            var balance = starting;
            foreach (var change in CashEvents(asOf, until, accounts, bankIds, includePlanned)
                         .OrderBy(e => e.Date)
                         .ThenBy(e => e.Amount))
            {
                balance += change.Amount;
            }

            return balance;
        }

        private IEnumerable<(DateTime Date, decimal Amount)> CashEvents(
            DateTime asOf,
            DateTime until,
            IReadOnlyList<Account> accounts,
            HashSet<Guid> bankIds,
            bool includePlanned)
        {
            var singles = _transactions.GetAllRecurringSingleRules().ToList();
            var transfers = _transactions.GetAllRecurringTransferRules().ToList();
            foreach (var bank in accounts.OfType<BankAccount>())
            {
                foreach (var view in _forecastGenerator.GenerateForecast(bank.Id, asOf.AddDays(1), until, singles, transfers))
                    yield return (view.Date.Date, view.Amount);
            }

            foreach (var future in _transactions.GetAllFutureSingleTransactions()
                         .Where(t => !t.IsRealized && t.Date.Date > asOf && t.Date.Date <= until))
            {
                if (future.IsPlannedPayment)
                {
                    if (!includePlanned)
                        continue;
                    yield return (future.Date.Date, PlannedCashImpact(future, bankIds));
                    continue;
                }

                if (bankIds.Contains(future.AccountId))
                    yield return (future.Date.Date, future.Amount);
            }

            foreach (var transfer in _transactions.GetAllFutureTransferTransactions()
                         .Where(t => !t.IsRealized && t.Date.Date > asOf && t.Date.Date <= until))
            {
                if (bankIds.Contains(transfer.FromAccountId))
                    yield return (transfer.Date.Date, -Math.Abs(transfer.Amount));
                if (bankIds.Contains(transfer.ToAccountId))
                    yield return (transfer.Date.Date, Math.Abs(transfer.Amount));
            }

            var plannedStatementIds = PlannedStatementKeys();
            foreach (var statement in _statements.GetUpcoming(asOf.AddDays(1))
                         .OfType<UtilityStatement>()
                         .Where(s => s.DueDate.Date <= until && !plannedStatementIds.Contains(s.Id)))
            {
                var amount = ForecastStatementAmount(statement);
                if (amount > 0)
                    yield return (statement.DueDate.Date, -Math.Abs(amount));
            }
        }

        private static decimal ForecastStatementAmount(AccountStatement statement)
        {
            if (statement is UtilityStatement utility && utility.Usage.Count > 0)
            {
                var usageTotal = utility.Usage.Sum(u => u.Amount * u.Rate);
                if (usageTotal > 0)
                    return usageTotal;
            }

            return statement.AmountDue;
        }

        private static decimal PlannedCashImpact(FutureSingleTransaction planned, HashSet<Guid> bankIds) =>
            bankIds.Contains(planned.AccountId) ? planned.Amount : -Math.Abs(planned.Amount);

        private List<DateTime> ResolvePaydays(DateTime asOf, IReadOnlyList<Account> accounts)
        {
            var bankIds = accounts.OfType<BankAccount>().Select(a => a.Id).ToHashSet();
            var dates = new List<DateTime>();
            foreach (var rule in _transactions.GetAllRecurringSingleRules()
                         .Where(r => r.IsActive && r.Amount > 0 && bankIds.Contains(r.AccountId)))
            {
                var next = rule.NextOccurrence.Date;
                var guard = 0;
                while (next < asOf && (rule.EndDate == null || next <= rule.EndDate.Value) && guard++ < 60)
                    next = next.AddFrequency(rule.Frequency);
                if (next >= asOf && (rule.EndDate == null || next <= rule.EndDate.Value))
                {
                    dates.Add(next);
                    var second = next.AddFrequency(rule.Frequency);
                    if (rule.EndDate == null || second <= rule.EndDate.Value)
                        dates.Add(second);
                }
            }

            dates = dates.Distinct().OrderBy(d => d).ToList();
            if (dates.Count >= 2)
                return dates.Take(2).ToList();
            if (dates.Count == 1)
                return [dates[0], dates[0].AddDays(14)];

            var friday = NextWeekday(asOf, DayOfWeek.Friday);
            return [friday, friday.AddDays(14)];
        }

        private static DateTime NextWeekday(DateTime asOf, DayOfWeek day)
        {
            var delta = ((int)day - (int)asOf.DayOfWeek + 7) % 7;
            return delta == 0 ? asOf : asOf.AddDays(delta);
        }

        private void AddRecurringForecastObligations(
            DateTime asOfDate,
            List<Account> accounts,
            Dictionary<Guid, string> names,
            HashSet<Guid> coveredAccounts,
            List<UpcomingObligation> rows)
        {
            var seen = rows
                .Select(r => (r.AccountId, r.DueDate, r.AmountDue, r.Notes))
                .ToHashSet();

            foreach (var rule in _transactions.GetAllRecurringSingleRules().Where(r => r.IsActive && r.Amount < 0))
            {
                var due = rule.NextOccurrence.Date;
                if (due < asOfDate)
                    continue;

                var amount = Math.Abs(rule.Amount);
                var notes = string.IsNullOrWhiteSpace(rule.Description)
                    ? "From recurring forecast"
                    : rule.Description.Trim();
                var key = (rule.AccountId, due, amount, notes);
                if (seen.Contains(key))
                    continue;

                rows.Add(new UpcomingObligation
                {
                    AccountId = rule.AccountId,
                    AccountName = names.GetValueOrDefault(rule.AccountId, ""),
                    DueDate = due,
                    AmountDue = amount,
                    PromotionalDue = 0,
                    Notes = notes
                });
                seen.Add(key);
            }

            var liabilityIds = accounts
                .Where(a => a is CreditAccount or LoanAccount or MortgageAccount)
                .Select(a => a.Id)
                .ToHashSet();

            foreach (var rule in _transactions.GetAllRecurringTransferRules().Where(r => r.IsActive))
            {
                if (!liabilityIds.Contains(rule.ToAccountId) || coveredAccounts.Contains(rule.ToAccountId))
                    continue;

                var due = rule.NextOccurrence.Date;
                if (due < asOfDate)
                    continue;

                var amount = Math.Abs(rule.Amount);
                var notes = string.IsNullOrWhiteSpace(rule.Description)
                    ? "From recurring forecast"
                    : rule.Description.Trim();
                var key = (rule.ToAccountId, due, amount, notes);
                if (seen.Contains(key))
                    continue;

                rows.Add(new UpcomingObligation
                {
                    AccountId = rule.ToAccountId,
                    AccountName = names.GetValueOrDefault(rule.ToAccountId, ""),
                    DueDate = due,
                    AmountDue = amount,
                    PromotionalDue = 0,
                    Notes = notes
                });
                seen.Add(key);
                coveredAccounts.Add(rule.ToAccountId);
            }
        }

        private UpcomingObligation? SynthesizeObligation(Account account, DateTime asOf)
        {
            DateTime? due = account switch
            {
                CreditAccount credit when credit.DueDate != default => RollForward(credit.DueDate.Date, asOf),
                MortgageAccount mortgage when mortgage.NextPaymentDate != default =>
                    mortgage.NextPaymentDate.Date >= asOf ? mortgage.NextPaymentDate.Date : null,
                _ => null
            };
            if (due is null)
                return null;

            var amountDue = account switch
            {
                CreditAccount credit => Math.Max(0, PostedBalanceCalculator.ToDisplayBalance(credit, credit.PostedBalance)),
                _ => 0m
            };

            return new UpcomingObligation
            {
                AccountId = account.Id,
                AccountName = account.Name,
                DueDate = due.Value,
                AmountDue = amountDue,
                PromotionalDue = 0,
                Notes = "From account metadata"
            };
        }

        private static DateTime RollForward(DateTime due, DateTime asOf)
        {
            var next = due;
            while (next < asOf)
                next = next.AddMonths(1);
            return next;
        }

        private static string ObligationNote(AccountStatement statement, decimal promoDue)
        {
            if (promoDue > 0)
                return $"{Promotions(statement).Count} promotion(s) due";

            return statement switch
            {
                MortgageStatement mortgage => MortgageNote(mortgage),
                UtilityStatement utility when utility.Usage.Count > 0 =>
                    string.Join(", ", utility.Usage.Select(u => $"{u.Amount:0.##} {u.Type}")),
                _ => statement.Notes ?? ""
            };
        }

        private static string MortgageNote(MortgageStatement mortgage) =>
            mortgage.EscrowBalance > 0
                ? $"Escrow {mortgage.EscrowBalance:c2}"
                : (mortgage.Notes ?? "");

        private static IReadOnlyList<PromotionalBalance> Promotions(AccountStatement statement) =>
            statement is CreditCardStatement card ? card.Promotions : [];

        private static IEnumerable<(DateTime Date, decimal Amount)> PromotionInstallments(PromotionalBalance promo, DateTime until)
        {
            var remainingBalance = promo.CurrentBalance;
            if (promo.Type == PromoType.LumpSum || promo.Deadline.Date <= DateTime.Today)
            {
                if (promo.Deadline.Date <= until)
                    yield return (promo.Deadline.Date, remainingBalance);
                yield break;
            }

            var months = Math.Max(1, ((promo.Deadline.Year - DateTime.Today.Year) * 12) + promo.Deadline.Month - DateTime.Today.Month);
            var installment = Math.Round(remainingBalance / months, 2, MidpointRounding.AwayFromZero);
            var remaining = remainingBalance;
            for (var i = 1; i <= months; i++)
            {
                var date = DateTime.Today.AddMonths(i);
                if (i == months)
                    date = promo.Deadline.Date;
                if (date > until)
                    yield break;
                var amount = i == months ? remaining : installment;
                remaining -= amount;
                yield return (date, amount);
            }
        }

        private static decimal ToLedgerPayment(Account account, decimal displayPayment) =>
            account is CreditAccount ? displayPayment : -Math.Abs(displayPayment);

        private static decimal InterestRate(Account account) =>
            account switch
            {
                LoanAccount loan => loan.InterestRate,
                MortgageAccount mortgage => mortgage.InterestRate,
                _ => 0
            };

        private DateTime? NextDueDate(Account account, DateTime asOf)
        {
            var fromStatement = _statements.GetUpcoming(asOf)
                .Where(s => s.AccountId == account.Id)
                .Select(s => (DateTime?)s.DueDate.Date)
                .FirstOrDefault();
            if (fromStatement is not null)
                return fromStatement;

            return account switch
            {
                MortgageAccount mortgage when mortgage.NextPaymentDate != default => mortgage.NextPaymentDate.Date,
                CreditAccount credit when credit.DueDate != default => RollForward(credit.DueDate.Date, asOf),
                _ => NextRecurringDue(account.Id, asOf)
            };
        }

        private DateTime? NextRecurringDue(Guid accountId, DateTime asOf)
        {
            var dates = _transactions.GetRecurringSingleRules(accountId)
                .Where(r => r.IsActive && r.Amount < 0 && r.NextOccurrence.Date >= asOf)
                .Select(r => r.NextOccurrence.Date)
                .Concat(_transactions.GetRecurringTransferRules(accountId)
                    .Where(r => r.IsActive && r.NextOccurrence.Date >= asOf)
                    .Select(r => r.NextOccurrence.Date))
                .ToList();
            return dates.Count == 0 ? null : dates.Min();
        }

        private Account RequireAccount(Guid accountId) =>
            _accounts.GetAllAccounts().FirstOrDefault(a => a.Id == accountId)
            ?? throw new InvalidOperationException("Account was not found.");

        private static void RequireUntil(DateTime until)
        {
            if (until == default)
                throw new InvalidOperationException("A plan-until date is required.");
        }

        private static void RequirePositiveAmount(decimal amount, string name)
        {
            if (amount <= 0)
                throw new InvalidOperationException($"{name} must be greater than zero.");
        }
    }
}
