using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Planning;
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

        public List<BillRow> GetBills(Guid accountId, DateTime? asOf = null)
        {
            DropSupersededStatementExpected();
            if (accountId == Guid.Empty)
                return [];

            var asOfDate = asOf ?? DateTime.Today;
            var accounts = _accounts.GetAllAccounts().ToList();
            var byId = accounts.ToDictionary(a => a.Id);
            var rows = new List<BillRow>();

            foreach (var item in UnmatchedSingles().Where(e => SplitTransactionMath.AffectsAccount(e, accountId)))
                rows.Add(FromExpected(item, byId, accountId, asOfDate));
            foreach (var item in UnmatchedTransfers().Where(e => SplitTransactionMath.AffectsAccount(e, accountId)))
                rows.Add(FromExpected(item, byId, accountId, asOfDate));

            var latest = PayableStatements.Latest(_statements.GetForAccount(accountId));
            if (latest is not null
                && !IsCovered(PaymentIntentSource.Statement, latest.Id, latest.AccountId, latest.DueDate.Date))
            {
                rows.Add(new BillRow
                {
                    Source = PaymentIntentSource.Statement,
                    SourceId = latest.Id,
                    StatementId = latest.Id,
                    DestinationAccountId = accountId,
                    DestinationName = NameOf(byId, accountId),
                    WebsiteUrl = UrlOf(byId, accountId),
                    FundingAccountId = Guid.Empty,
                    FundingName = "",
                    OtherAccountId = null,
                    OtherAccountName = "",
                    OtherWebsiteUrl = "",
                    CanChoosePayFrom = true,
                    Kind = BillKinds.Statement,
                    DueDate = latest.DueDate.Date,
                    Amount = Math.Abs(latest.AmountDue),
                    Status = BillStatuses.Due,
                    Notes = string.IsNullOrWhiteSpace(latest.Notes) ? "Statement due" : latest.Notes
                });
            }

            foreach (var rule in _transactions.GetAllRecurringSingleRules()
                         .Where(r => r.IsActive && r.AccountId == accountId))
            {
                if (IsCovered(PaymentIntentSource.RecurringSingle, rule.Id, rule.AccountId, rule.NextOccurrence.Date))
                    continue;

                rows.Add(new BillRow
                {
                    Source = PaymentIntentSource.RecurringSingle,
                    SourceId = rule.Id,
                    DestinationAccountId = rule.AccountId,
                    DestinationName = NameOf(byId, rule.AccountId),
                    WebsiteUrl = UrlOf(byId, rule.AccountId),
                    FundingAccountId = rule.AccountId,
                    FundingName = NameOf(byId, rule.AccountId),
                    Kind = BillKinds.Recurring,
                    DueDate = rule.NextOccurrence.Date,
                    Amount = rule.Amount,
                    Status = StatusOf(TransactionStatuses.ForExpected(
                        rule.NextOccurrence, realized: false, ExpectedStatus.Planned, asOfDate)),
                    Notes = rule.Description ?? ""
                });
            }

            foreach (var rule in _transactions.GetAllRecurringTransferRules()
                         .Where(r => r.IsActive && (r.FromAccountId == accountId || r.ToAccountId == accountId)))
            {
                if (rule.FromAccountId == rule.ToAccountId)
                    continue;
                if (IsCovered(PaymentIntentSource.RecurringTransfer, rule.Id, rule.ToAccountId, rule.NextOccurrence.Date))
                    continue;
                if (accountId == rule.ToAccountId
                    && latest is not null
                    && !IsCovered(PaymentIntentSource.Statement, latest.Id, latest.AccountId, latest.DueDate.Date))
                    continue;

                var otherId = accountId == rule.FromAccountId ? rule.ToAccountId : rule.FromAccountId;
                rows.Add(new BillRow
                {
                    Source = PaymentIntentSource.RecurringTransfer,
                    SourceId = rule.Id,
                    DestinationAccountId = rule.ToAccountId,
                    DestinationName = NameOf(byId, rule.ToAccountId),
                    WebsiteUrl = UrlOf(byId, otherId),
                    FundingAccountId = rule.FromAccountId,
                    FundingName = NameOf(byId, rule.FromAccountId),
                    OtherAccountId = otherId,
                    OtherAccountName = NameOf(byId, otherId),
                    OtherWebsiteUrl = UrlOf(byId, otherId),
                    Kind = BillKinds.Transfer,
                    DueDate = rule.NextOccurrence.Date,
                    Amount = SplitTransactionMath.TransferAmountForAccount(
                        rule.FromAccountId, rule.ToAccountId, rule.Amount, accountId),
                    Status = StatusOf(TransactionStatuses.ForExpected(
                        rule.NextOccurrence, realized: false, ExpectedStatus.Planned, asOfDate)),
                    Notes = rule.Description ?? ""
                });
            }

            return rows
                .OrderBy(r => r.DueDate)
                .ThenBy(r => r.Notes)
                .ThenBy(r => r.OtherAccountName)
                .ToList();
        }

        public decimal CashRemaining(IEnumerable<BillRow>? pending = null)
        {
            DropSupersededStatementExpected();
            var banks = _accounts.GetAllAccounts().OfType<BankAccount>().ToList();
            var bankIds = banks.Select(b => b.Id).ToHashSet();
            var current = banks.Sum(b => b.PostedBalance);
            var held = UnmatchedTransfers()
                .Where(p => p.Status == ExpectedStatus.Scheduled && bankIds.Contains(p.FromAccountId))
                .Sum(p => Math.Abs(p.Amount))
                + UnmatchedSingles()
                .Where(p => p.Status == ExpectedStatus.Scheduled && bankIds.Contains(p.AccountId))
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

        public List<Guid> Schedule(IEnumerable<BillRow> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            var created = new List<Guid>();
            foreach (var row in rows.Where(r => r.Pay))
            {
                if (row.Amount == 0)
                    continue;
                if (NeedsPayFrom(row) && row.FundingAccountId == Guid.Empty)
                    throw new InvalidOperationException($"Select a pay-from account for {DisplayName(row)}.");
                if (row.DestinationAccountId == Guid.Empty)
                    throw new InvalidOperationException("A destination account is required.");

                created.Add(EnsureExpected(row, ExpectedStatus.Scheduled));
            }

            return created;
        }

        public void Unschedule(Guid expectedId)
        {
            var transfer = _transactions.GetFutureTransferTransaction(expectedId);
            if (transfer is not null)
            {
                if (transfer.IsRealized)
                    throw new InvalidOperationException("Only scheduled payments can be unscheduled.");
                if (transfer.Origin is ExpectedOrigin.RecurringSingle
                    or ExpectedOrigin.RecurringTransfer
                    or ExpectedOrigin.Manual)
                {
                    transfer.Status = ExpectedStatus.Planned;
                    _transactions.UpdateFutureTransferTransaction(transfer);
                    return;
                }

                _transactions.DeleteFutureTransferTransaction(expectedId);
                return;
            }

            var single = _transactions.GetFutureSingleTransaction(expectedId)
                ?? throw new InvalidOperationException("Scheduled payment was not found.");
            if (single.IsRealized)
                throw new InvalidOperationException("Only scheduled payments can be unscheduled.");
            if (single.Origin is ExpectedOrigin.RecurringSingle or ExpectedOrigin.Manual)
            {
                single.Status = ExpectedStatus.Planned;
                _transactions.UpdateFutureSingleTransaction(single);
                return;
            }

            _transactions.DeleteFutureSingleTransaction(expectedId);
        }

        public FutureTransferTransaction AddManual(
            Guid destinationAccountId,
            Guid fundingAccountId,
            decimal amount,
            DateTime payDate,
            string? notes,
            Guid? statementId = null)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");
            if (fundingAccountId == Guid.Empty)
                throw new InvalidOperationException("Select a pay-from account.");
            if (destinationAccountId == Guid.Empty)
                throw new InvalidOperationException("Select an account.");

            var origin = statementId is Guid ? ExpectedOrigin.StatementPay : ExpectedOrigin.Pay;
            var existing = UnmatchedTransfers()
                .FirstOrDefault(p =>
                    p.Origin == origin
                    && (statementId is Guid sourceId ? p.OriginId == sourceId : p.ToAccountId == destinationAccountId && p.Date.Date == payDate.Date));
            if (existing is not null)
            {
                existing.FromAccountId = fundingAccountId;
                existing.ToAccountId = destinationAccountId;
                existing.Amount = Math.Abs(amount);
                existing.Date = payDate.Date;
                existing.Status = ExpectedStatus.Scheduled;
                if (!string.IsNullOrWhiteSpace(notes))
                    existing.Description = notes.Trim();
                _transactions.UpdateFutureTransferTransaction(existing);
                return existing;
            }

            var transfer = new FutureTransferTransaction
            {
                FromAccountId = fundingAccountId,
                ToAccountId = destinationAccountId,
                Amount = Math.Abs(amount),
                Date = payDate.Date,
                Status = ExpectedStatus.Scheduled,
                Origin = origin,
                OriginId = statementId,
                StatementId = statementId,
                IsUserCreated = true,
                IsPlannedPayment = true,
                Description = string.IsNullOrWhiteSpace(notes) ? "Manual" : notes.Trim()
            };
            ApplyPaymentCategory(transfer);
            _transactions.AddFutureTransferTransaction(transfer);
            return transfer;
        }

        public int MatchScheduled() =>
            new ReconciliationOrchestrator(_transactions).RecommendMatches();

        public List<ImportedTransactionView> GetUnreconciledImports(Guid accountId) =>
            new ReconciliationOrchestrator(_transactions).GetUnreconciled(accountId);

        public void MatchToImported(BillRow row, Guid importedId)
        {
            ArgumentNullException.ThrowIfNull(row);
            var expectedId = row.IntentId ?? EnsureExpected(row, ExpectedStatus.Planned);
            new ReconciliationOrchestrator(_transactions).AcceptMatch(importedId, expectedId);
        }

        private Guid EnsureExpected(BillRow row, ExpectedStatus status)
        {
            var origin = OriginOf(row.Source);
            if (row.Source == PaymentIntentSource.RecurringSingle
                || (row.FundingAccountId == row.DestinationAccountId && row.FundingAccountId != Guid.Empty)
                || (row.Source == PaymentIntentSource.Statement && row.FundingAccountId == Guid.Empty))
            {
                var existingSingle = UnmatchedSingles()
                    .FirstOrDefault(e => SameBill(e.Id, e.Origin, e.OriginId, e.AccountId, e.Date, row));
                if (existingSingle is not null)
                {
                    ApplySingle(existingSingle, row, origin, status);
                    _transactions.UpdateFutureSingleTransaction(existingSingle);
                    return existingSingle.Id;
                }

                var single = new FutureSingleTransaction();
                ApplySingle(single, row, origin, status);
                ApplyPaymentCategory(single);
                _transactions.AddFutureSingleTransaction(single);
                return single.Id;
            }

            var existing = UnmatchedTransfers()
                .FirstOrDefault(e => SameBill(e.Id, e.Origin, e.OriginId, e.ToAccountId, e.Date, row));
            if (existing is not null)
            {
                ApplyTransfer(existing, row, origin, status);
                _transactions.UpdateFutureTransferTransaction(existing);
                return existing.Id;
            }

            var transfer = new FutureTransferTransaction();
            ApplyTransfer(transfer, row, origin, status);
            ApplyPaymentCategory(transfer);
            _transactions.AddFutureTransferTransaction(transfer);
            return transfer.Id;
        }

        private static bool NeedsPayFrom(BillRow row) =>
            row.Source is PaymentIntentSource.Statement or PaymentIntentSource.RecurringTransfer or PaymentIntentSource.Manual
            && row.FundingAccountId == Guid.Empty
            && row.DestinationAccountId != Guid.Empty;

        private static string DisplayName(BillRow row) =>
            string.IsNullOrWhiteSpace(row.Notes) ? row.DestinationName : row.Notes;

        private void ApplyPaymentCategory(BaseTransaction transaction)
        {
            var category = _transactions.GetCategory(DefaultExpenseCategories.PaymentId)
                ?? DefaultExpenseCategories.All.First(c => c.Id == DefaultExpenseCategories.PaymentId);
            transaction.ApplyCategory(category);
        }

        private IEnumerable<FutureTransferTransaction> UnmatchedTransfers() =>
            _transactions.GetAllFutureTransferTransactions().Where(e => !e.IsRealized);

        private IEnumerable<FutureSingleTransaction> UnmatchedSingles() =>
            _transactions.GetAllFutureSingleTransactions().Where(e => !e.IsRealized);

        private void DropSupersededStatementExpected()
        {
            var latestByAccount = _accounts.GetAllAccounts()
                .ToDictionary(a => a.Id, a => PayableStatements.Latest(_statements.GetForAccount(a.Id)));

            foreach (var expected in UnmatchedTransfers()
                         .Where(p => p.Origin == ExpectedOrigin.StatementPay)
                         .ToList())
            {
                if (!latestByAccount.TryGetValue(expected.ToAccountId, out var latest)
                    || latest is null
                    || expected.OriginId != latest.Id)
                {
                    _transactions.DeleteFutureTransferTransaction(expected.Id);
                }
            }
        }

        private bool IsCovered(
            PaymentIntentSource source,
            Guid sourceId,
            Guid destinationId,
            DateTime due)
        {
            var origin = OriginOf(source);
            if (UnmatchedTransfers().Any(p =>
                    (p.Origin == origin && p.OriginId == sourceId)
                    || (p.ToAccountId == destinationId && p.Date.Date == due.Date)))
                return true;
            return UnmatchedSingles().Any(p =>
                (p.Origin == origin && p.OriginId == sourceId)
                || (p.AccountId == destinationId && p.Date.Date == due.Date));
        }

        private static bool SameBill(
            Guid id,
            ExpectedOrigin origin,
            Guid? originId,
            Guid destinationId,
            DateTime date,
            BillRow row)
        {
            if (row.IntentId is Guid existingId && id == existingId)
                return true;
            if (row.SourceId is Guid sourceId && origin == OriginOf(row.Source) && originId == sourceId)
                return true;
            return destinationId == row.DestinationAccountId
                && date.Date == row.DueDate.Date
                && origin == OriginOf(row.Source);
        }

        private static void ApplyTransfer(
            FutureTransferTransaction transfer,
            BillRow row,
            ExpectedOrigin origin,
            ExpectedStatus status)
        {
            transfer.Origin = origin;
            transfer.OriginId = row.SourceId;
            transfer.ToAccountId = row.DestinationAccountId;
            transfer.FromAccountId = row.FundingAccountId;
            transfer.Amount = Math.Abs(row.Amount);
            transfer.Date = row.DueDate == default ? DateTime.Today : row.DueDate.Date;
            transfer.Description = string.IsNullOrWhiteSpace(row.Notes) ? row.DestinationName : row.Notes.Trim();
            transfer.Status = status;
            transfer.IsUserCreated = true;
            transfer.IsPlannedPayment = true;
            transfer.StatementId = row.Source == PaymentIntentSource.Statement ? row.SourceId : transfer.StatementId;
        }

        private static void ApplySingle(
            FutureSingleTransaction single,
            BillRow row,
            ExpectedOrigin origin,
            ExpectedStatus status)
        {
            single.Origin = origin;
            single.OriginId = row.SourceId;
            single.AccountId = row.DestinationAccountId == Guid.Empty ? row.FundingAccountId : row.DestinationAccountId;
            single.Amount = row.Amount;
            single.Date = row.DueDate == default ? DateTime.Today : row.DueDate.Date;
            single.Description = string.IsNullOrWhiteSpace(row.Notes) ? row.DestinationName : row.Notes.Trim();
            single.Status = status;
            single.IsUserCreated = true;
        }

        private static ExpectedOrigin OriginOf(PaymentIntentSource source) =>
            source switch
            {
                PaymentIntentSource.Statement => ExpectedOrigin.StatementPay,
                PaymentIntentSource.RecurringSingle => ExpectedOrigin.RecurringSingle,
                PaymentIntentSource.RecurringTransfer => ExpectedOrigin.RecurringTransfer,
                _ => ExpectedOrigin.Pay
            };

        private static PaymentIntentSource SourceOf(ExpectedOrigin origin) =>
            origin switch
            {
                ExpectedOrigin.StatementPay => PaymentIntentSource.Statement,
                ExpectedOrigin.RecurringSingle => PaymentIntentSource.RecurringSingle,
                ExpectedOrigin.RecurringTransfer => PaymentIntentSource.RecurringTransfer,
                _ => PaymentIntentSource.Manual
            };

        private static string NameOf(IReadOnlyDictionary<Guid, Account> accounts, Guid id) =>
            accounts.TryGetValue(id, out var account) ? account.Name : "";

        private static string UrlOf(IReadOnlyDictionary<Guid, Account> accounts, Guid id) =>
            accounts.TryGetValue(id, out var account) ? account.WebsiteUrl ?? "" : "";

        private static BillRow FromExpected(
            FutureSingleTransaction expected,
            Dictionary<Guid, Account> accounts,
            Guid accountId,
            DateTime asOf)
        {
            var source = SourceOf(expected.Origin);
            var otherId = SplitTransactionMath.OtherAccountId(expected, accountId);
            return new BillRow
            {
                Pay = expected.Status == ExpectedStatus.Scheduled,
                IntentId = expected.Id,
                Source = source,
                SourceId = expected.OriginId,
                DestinationAccountId = expected.AccountId,
                DestinationName = NameOf(accounts, expected.AccountId),
                WebsiteUrl = UrlOf(accounts, otherId ?? expected.AccountId),
                FundingAccountId = expected.AccountId,
                FundingName = NameOf(accounts, expected.AccountId),
                OtherAccountId = otherId,
                OtherAccountName = otherId is Guid id ? NameOf(accounts, id) : "",
                OtherWebsiteUrl = otherId is Guid other ? UrlOf(accounts, other) : "",
                Kind = KindOf(source, isTransfer: otherId is not null),
                DueDate = expected.Date.Date,
                Amount = SplitTransactionMath.AmountForAccount(expected, accountId),
                Status = StatusOf(expected.DisplayStatus(asOf)),
                Notes = expected.Description ?? ""
            };
        }

        private static BillRow FromExpected(
            FutureTransferTransaction expected,
            Dictionary<Guid, Account> accounts,
            Guid accountId,
            DateTime asOf)
        {
            var source = SourceOf(expected.Origin);
            var otherId = SplitTransactionMath.OtherAccountId(expected, accountId);
            return new BillRow
            {
                Pay = expected.Status == ExpectedStatus.Scheduled,
                IntentId = expected.Id,
                Source = source,
                SourceId = expected.OriginId,
                StatementId = expected.StatementId ?? (source == PaymentIntentSource.Statement ? expected.OriginId : null),
                DestinationAccountId = expected.ToAccountId,
                DestinationName = NameOf(accounts, expected.ToAccountId),
                WebsiteUrl = UrlOf(accounts, otherId ?? expected.ToAccountId),
                FundingAccountId = expected.FromAccountId,
                FundingName = NameOf(accounts, expected.FromAccountId),
                OtherAccountId = otherId,
                OtherAccountName = otherId is Guid id ? NameOf(accounts, id) : "",
                OtherWebsiteUrl = otherId is Guid other ? UrlOf(accounts, other) : "",
                CanChoosePayFrom = expected.FromAccountId == Guid.Empty,
                Kind = KindOf(source, isTransfer: true),
                DueDate = expected.Date.Date,
                Amount = SplitTransactionMath.AmountForAccount(expected, accountId),
                Status = StatusOf(expected.DisplayStatus(asOf)),
                Notes = expected.Description ?? ""
            };
        }

        private static string KindOf(PaymentIntentSource source, bool isTransfer) =>
            source switch
            {
                PaymentIntentSource.Statement => BillKinds.Statement,
                PaymentIntentSource.RecurringTransfer => BillKinds.Transfer,
                PaymentIntentSource.Manual => isTransfer ? BillKinds.Transfer : BillKinds.Manual,
                _ => isTransfer ? BillKinds.Transfer : BillKinds.Recurring
            };

        private static string StatusOf(string displayStatus) =>
            displayStatus switch
            {
                TransactionStatuses.Scheduled => BillStatuses.Scheduled,
                TransactionStatuses.Pending => BillStatuses.Pending,
                _ => BillStatuses.Due
            };
    }
}
