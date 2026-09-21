using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators.Finance
{
    public class ReconciliationOrchestrator
    {
        private readonly ITransactionDataStore _transactions;
        private readonly MatchRecommender _recommender = new();

        public ReconciliationOrchestrator()
            : this(new DataStoreFactory().GetTransactionStore())
        {
        }

        public ReconciliationOrchestrator(ITransactionDataStore transactions)
        {
            _transactions = transactions;
        }

        public int RecommendMatches()
        {
            var singles = _transactions.GetAllFutureSingleTransactions().ToList();
            var transfers = _transactions.GetAllFutureTransferTransactions().ToList();
            var imported = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue)
                .Concat(_transactions.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue))
                .ToList();

            foreach (var row in imported.Where(p => p.ImportedStatus == ImportedStatus.Unreconciled && p.RecommendedExpectedId is not null))
            {
                row.RecommendedExpectedId = null;
                UpdateImported(row);
            }

            var recommendations = _recommender.Recommend(imported, singles, transfers);
            var byId = imported.ToDictionary(p => p.Id);
            foreach (var recommendation in recommendations)
            {
                if (!byId.TryGetValue(recommendation.ImportedId, out var row))
                    continue;
                row.RecommendedExpectedId = recommendation.ExpectedId;
                UpdateImported(row);
            }

            return recommendations.Count;
        }

        public TransactionReconciliation AcceptMatch(Guid importedId, Guid? expectedId = null)
        {
            var imported = RequireUnreconciled(importedId);
            var expected = ResolveExpected(expectedId ?? imported.RecommendedExpectedId)
                ?? throw new InvalidOperationException("Select an expected transaction to match, or accept as new.");
            if (expected is FutureSingleTransaction realizedSingle && realizedSingle.IsRealized)
                throw new InvalidOperationException("That expected transaction is already reconciled.");
            if (expected is FutureTransferTransaction realizedTransfer && realizedTransfer.IsRealized)
                throw new InvalidOperationException("That expected transaction is already reconciled.");

            return CompleteMatch(imported, expected);
        }

        public TransactionReconciliation AcceptAsNew(Guid importedId)
        {
            var imported = RequireUnreconciled(importedId);
            var recommended = imported.RecommendedExpectedId;
            imported.ImportedStatus = ImportedStatus.AcceptedNew;
            imported.RecommendedExpectedId = null;
            UpdateImported(imported);

            var reconciliation = new TransactionReconciliation
            {
                ImportedTransactionId = imported.Id,
                RecommendedExpectedId = recommended,
                AcceptedOn = DateTime.UtcNow
            };
            _transactions.AddReconciliation(reconciliation);
            return reconciliation;
        }

        public int AcceptAsNewBefore(Guid accountId, DateTime before)
        {
            var cutoff = before.Date;
            var imported = UnreconciledForAccount(accountId)
                .Where(p => p.Date.Date < cutoff)
                .ToList();
            foreach (var row in imported)
                AcceptAsNew(row.Id);
            return imported.Count;
        }

        public void UndoMatch(Guid importedId)
        {
            var reconciliation = _transactions.GetReconciliationByImported(importedId)
                ?? throw new InvalidOperationException("That transaction is not reconciled.");
            var imported = FindImported(importedId)
                ?? throw new InvalidOperationException("Imported transaction was not found.");

            imported.ImportedStatus = ImportedStatus.Unreconciled;
            imported.RecommendedExpectedId = null;
            UpdateImported(imported);

            if (reconciliation.ExpectedTransactionId is Guid expectedId)
            {
                Unrealize(expectedId);
                RestoreRule(expectedId, reconciliation);
            }

            _transactions.DeleteReconciliation(reconciliation.Id);
        }

        public void MaterializePlannedFromRules()
        {
            foreach (var rule in _transactions.GetAllRecurringSingleRules().Where(r => r.IsActive))
                EnsurePlannedSingle(rule);
            foreach (var rule in _transactions.GetAllRecurringTransferRules().Where(r => r.IsActive))
                EnsurePlannedTransfer(rule);
        }

        public void MaterializeRule(RecurringSingleTransactionRule rule) => EnsurePlannedSingle(rule);

        public void MaterializeRule(RecurringTransferRule rule) => EnsurePlannedTransfer(rule);

        public void DropUnmatchedForRule(Guid ruleId)
        {
            foreach (var expected in _transactions.GetAllFutureSingleTransactions()
                         .Where(e => !e.IsRealized && e.OriginId == ruleId)
                         .ToList())
                _transactions.DeleteFutureSingleTransaction(expected.Id);
            foreach (var expected in _transactions.GetAllFutureTransferTransactions()
                         .Where(e => !e.IsRealized && e.OriginId == ruleId)
                         .ToList())
                _transactions.DeleteFutureTransferTransaction(expected.Id);
        }

        public List<ImportedTransactionView> GetUnreconciled(Guid accountId)
        {
            var expectedNames = ExpectedLookup();
            return UnreconciledForAccount(accountId)
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Description)
                .Select(p => ToImportedView(p, expectedNames))
                .ToList();
        }

        public List<ExpectedChoice> GetUnmatchedExpected(Guid accountId) =>
            UnmatchedExpected()
                .Where(e => e.AccountId == accountId)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Description)
                .ToList();

        public string DescribeExpected(Guid expectedId)
        {
            if (_transactions.GetFutureSingleTransaction(expectedId) is FutureSingleTransaction single)
                return FormatExpected(single.Date, single.Description, single.Amount, single.DisplayStatus());
            if (_transactions.GetFutureTransferTransaction(expectedId) is FutureTransferTransaction transfer)
                return FormatExpected(transfer.Date, transfer.Description, transfer.Amount, transfer.DisplayStatus());
            return "";
        }

        private TransactionReconciliation CompleteMatch(PostedTransaction imported, BaseTransaction expected)
        {
            DateTime? lastBefore = null;
            DateTime? nextBefore = null;
            Guid? originId = null;
            var origin = ExpectedOrigin.Manual;
            if (expected is FutureSingleTransaction single)
            {
                origin = single.Origin;
                originId = single.OriginId;
                CaptureRule(origin, originId, out lastBefore, out nextBefore);
                single.IsRealized = true;
                single.PostedTransactionId = imported.Id;
                _transactions.UpdateFutureSingleTransaction(single);
            }
            else if (expected is FutureTransferTransaction transfer)
            {
                origin = transfer.Origin;
                originId = transfer.OriginId;
                CaptureRule(origin, originId, out lastBefore, out nextBefore);
                transfer.IsRealized = true;
                if (imported.AccountId == transfer.FromAccountId)
                    transfer.PostedFromTransactionId = imported.Id;
                else
                    transfer.PostedToTransactionId = imported.Id;
                _transactions.UpdateFutureTransferTransaction(transfer);
            }

            ApplyExpectedDetails(imported, expected);

            var recommended = imported.RecommendedExpectedId;
            imported.ImportedStatus = ImportedStatus.Matched;
            imported.RecommendedExpectedId = null;
            UpdateImported(imported);

            var reconciliation = new TransactionReconciliation
            {
                ImportedTransactionId = imported.Id,
                ExpectedTransactionId = expected.Id,
                RecommendedExpectedId = recommended,
                AcceptedOn = DateTime.UtcNow,
                RuleLastOccurrenceBefore = lastBefore,
                RuleNextOccurrenceBefore = nextBefore
            };
            _transactions.AddReconciliation(reconciliation);

            if (origin is ExpectedOrigin.RecurringSingle or ExpectedOrigin.RecurringTransfer && originId is Guid ruleId)
                AdvanceRule(origin, ruleId, imported.Date);

            return reconciliation;
        }

        private void CaptureRule(
            ExpectedOrigin origin,
            Guid? originId,
            out DateTime? lastBefore,
            out DateTime? nextBefore)
        {
            lastBefore = null;
            nextBefore = null;
            if (originId is not Guid id)
                return;
            if (origin == ExpectedOrigin.RecurringSingle)
            {
                var rule = _transactions.GetRecurringSingleRule(id);
                lastBefore = rule?.LastOccurrence;
                nextBefore = rule?.NextOccurrence;
            }
            else if (origin == ExpectedOrigin.RecurringTransfer)
            {
                var rule = _transactions.GetRecurringTransferRule(id);
                lastBefore = rule?.LastOccurrence;
                nextBefore = rule?.NextOccurrence;
            }
        }

        private void AdvanceRule(ExpectedOrigin origin, Guid ruleId, DateTime postedDate)
        {
            if (origin == ExpectedOrigin.RecurringSingle)
            {
                var rule = _transactions.GetRecurringSingleRule(ruleId);
                if (rule is null || !rule.IsActive)
                    return;
                rule.LastOccurrence = postedDate.Date;
                rule.NextOccurrence = postedDate.Date.AddFrequency(rule.Frequency);
                _transactions.UpdateRecurringSingleRule(rule);
                EnsurePlannedSingle(rule);
                return;
            }

            var transfer = _transactions.GetRecurringTransferRule(ruleId);
            if (transfer is null || !transfer.IsActive)
                return;
            transfer.LastOccurrence = postedDate.Date;
            transfer.NextOccurrence = postedDate.Date.AddFrequency(transfer.Frequency);
            _transactions.UpdateRecurringTransferRule(transfer);
            EnsurePlannedTransfer(transfer);
        }

        private void RestoreRule(Guid expectedId, TransactionReconciliation reconciliation)
        {
            var single = _transactions.GetFutureSingleTransaction(expectedId);
            var transfer = single is null ? _transactions.GetFutureTransferTransaction(expectedId) : null;
            var origin = single?.Origin ?? transfer?.Origin ?? ExpectedOrigin.Manual;
            var originId = single?.OriginId ?? transfer?.OriginId;
            if (originId is not Guid ruleId)
                return;

            foreach (var extra in _transactions.GetAllFutureSingleTransactions()
                         .Where(e => !e.IsRealized && e.OriginId == ruleId && e.Id != expectedId)
                         .ToList())
                _transactions.DeleteFutureSingleTransaction(extra.Id);
            foreach (var extra in _transactions.GetAllFutureTransferTransactions()
                         .Where(e => !e.IsRealized && e.OriginId == ruleId && e.Id != expectedId)
                         .ToList())
                _transactions.DeleteFutureTransferTransaction(extra.Id);

            if (origin == ExpectedOrigin.RecurringSingle)
            {
                var rule = _transactions.GetRecurringSingleRule(ruleId);
                if (rule is null)
                    return;
                rule.LastOccurrence = reconciliation.RuleLastOccurrenceBefore;
                rule.NextOccurrence = reconciliation.RuleNextOccurrenceBefore ?? single?.Date ?? rule.NextOccurrence;
                _transactions.UpdateRecurringSingleRule(rule);
                EnsurePlannedSingle(rule);
            }
            else if (origin == ExpectedOrigin.RecurringTransfer)
            {
                var rule = _transactions.GetRecurringTransferRule(ruleId);
                if (rule is null)
                    return;
                rule.LastOccurrence = reconciliation.RuleLastOccurrenceBefore;
                rule.NextOccurrence = reconciliation.RuleNextOccurrenceBefore ?? transfer?.Date ?? rule.NextOccurrence;
                _transactions.UpdateRecurringTransferRule(rule);
                EnsurePlannedTransfer(rule);
            }
        }

        private void Unrealize(Guid expectedId)
        {
            var single = _transactions.GetFutureSingleTransaction(expectedId);
            if (single is not null)
            {
                single.IsRealized = false;
                single.PostedTransactionId = null;
                _transactions.UpdateFutureSingleTransaction(single);
                return;
            }

            var transfer = _transactions.GetFutureTransferTransaction(expectedId);
            if (transfer is null)
                return;
            transfer.IsRealized = false;
            transfer.PostedFromTransactionId = null;
            transfer.PostedToTransactionId = null;
            _transactions.UpdateFutureTransferTransaction(transfer);
        }

        private void EnsurePlannedSingle(RecurringSingleTransactionRule rule)
        {
            if (!rule.IsActive)
                return;
            if (HasUnmatchedExpected(rule.Id, rule.NextOccurrence.Date))
                return;
            var planned = new FutureSingleTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = rule.AccountId,
                Date = rule.NextOccurrence.Date,
                Amount = rule.Amount,
                Description = rule.Description,
                Category = rule.Category,
                CategoryId = rule.CategoryId,
                Origin = ExpectedOrigin.RecurringSingle,
                OriginId = rule.Id,
                Status = ExpectedStatus.Planned,
                IsUserCreated = true,
                Splits = CloneSplits(rule.Splits)
            };
            AssignSplitParents(planned.Id, planned.Splits);
            _transactions.AddFutureSingleTransaction(planned);
        }

        private void EnsurePlannedTransfer(RecurringTransferRule rule)
        {
            if (!rule.IsActive)
                return;
            if (HasUnmatchedExpected(rule.Id, rule.NextOccurrence.Date))
                return;
            var planned = new FutureTransferTransaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = rule.FromAccountId,
                ToAccountId = rule.ToAccountId,
                Date = rule.NextOccurrence.Date,
                Amount = Math.Abs(rule.Amount),
                Description = rule.Description,
                Category = rule.Category,
                CategoryId = rule.CategoryId,
                Origin = ExpectedOrigin.RecurringTransfer,
                OriginId = rule.Id,
                Status = ExpectedStatus.Planned,
                IsUserCreated = true,
                Splits = CloneSplits(rule.Splits)
            };
            AssignSplitParents(planned.Id, planned.Splits);
            _transactions.AddFutureTransferTransaction(planned);
        }

        private bool HasUnmatchedExpected(Guid originId, DateTime date) =>
            _transactions.GetAllFutureSingleTransactions().Any(e =>
                !e.IsRealized && e.OriginId == originId && e.Date.Date == date.Date)
            || _transactions.GetAllFutureTransferTransactions().Any(e =>
                !e.IsRealized && e.OriginId == originId && e.Date.Date == date.Date);

        private PostedTransaction RequireUnreconciled(Guid importedId)
        {
            var imported = FindImported(importedId)
                ?? throw new InvalidOperationException("Imported transaction was not found.");
            if (imported.ImportedStatus != ImportedStatus.Unreconciled)
                throw new InvalidOperationException("That transaction is already reconciled.");
            return imported;
        }

        private PostedTransaction? FindImported(Guid id) =>
            _transactions.GetPostedTransaction(id) ?? _transactions.GetPostedTransferTransaction(id);

        private BaseTransaction? ResolveExpected(Guid? id)
        {
            if (id is not Guid expectedId || expectedId == Guid.Empty)
                return null;
            return (BaseTransaction?)_transactions.GetFutureSingleTransaction(expectedId)
                ?? _transactions.GetFutureTransferTransaction(expectedId);
        }

        private void ApplyExpectedDetails(PostedTransaction imported, BaseTransaction expected)
        {
            if (!string.IsNullOrWhiteSpace(expected.Category))
                imported.Category = expected.Category;
            if (expected.CategoryId is Guid categoryId)
                imported.CategoryId = categoryId;
            if (!expected.HasSplits)
                return;

            var splits = CloneSplits(expected.Splits);
            AssignSplitParents(imported.Id, splits);
            imported.Splits = splits;
            _transactions.SaveSplits(imported.Id, splits);
        }

        private static List<SplitTransactionRow> CloneSplits(IEnumerable<SplitTransactionRow> splits) =>
            splits.Select(s =>
            {
                var copy = s.Clone();
                copy.Id = Guid.NewGuid();
                return copy;
            }).ToList();

        private static void AssignSplitParents(Guid parentId, IEnumerable<SplitTransactionRow> splits)
        {
            foreach (var split in splits)
                split.ParentTransactionId = parentId;
        }

        private void UpdateImported(PostedTransaction imported)
        {
            if (imported is PostedTransferTransaction transfer)
                _transactions.UpdatePostedTransferTransaction(transfer);
            else
                _transactions.UpdatePostedTransaction(imported);
        }

        private IEnumerable<PostedTransaction> UnreconciledForAccount(Guid accountId) =>
            _transactions.GetPostedTransactions(accountId)
                .Concat(_transactions.GetPostedTransferTransactions(accountId))
                .Where(p => p.ImportedStatus == ImportedStatus.Unreconciled);

        private IEnumerable<ExpectedChoice> UnmatchedExpected()
        {
            foreach (var single in _transactions.GetAllFutureSingleTransactions().Where(e => !e.IsRealized))
            {
                yield return new ExpectedChoice
                {
                    Id = single.Id,
                    AccountId = single.AccountId,
                    Date = single.Date,
                    Description = single.Description ?? "",
                    Amount = single.Amount,
                    Status = single.DisplayStatus()
                };
            }

            foreach (var transfer in _transactions.GetAllFutureTransferTransactions().Where(e => !e.IsRealized))
            {
                yield return new ExpectedChoice
                {
                    Id = transfer.Id,
                    AccountId = transfer.FromAccountId,
                    CounterpartyAccountId = transfer.ToAccountId,
                    Date = transfer.Date,
                    Description = transfer.Description ?? "",
                    Amount = transfer.Amount,
                    Status = transfer.DisplayStatus()
                };
                yield return new ExpectedChoice
                {
                    Id = transfer.Id,
                    AccountId = transfer.ToAccountId,
                    CounterpartyAccountId = transfer.FromAccountId,
                    Date = transfer.Date,
                    Description = transfer.Description ?? "",
                    Amount = transfer.Amount,
                    Status = transfer.DisplayStatus()
                };
            }
        }

        private Dictionary<Guid, string> ExpectedLookup()
        {
            var names = new Dictionary<Guid, string>();
            foreach (var single in _transactions.GetAllFutureSingleTransactions())
                names[single.Id] = FormatExpected(single.Date, single.Description, single.Amount, single.DisplayStatus());
            foreach (var transfer in _transactions.GetAllFutureTransferTransactions())
                names[transfer.Id] = FormatExpected(transfer.Date, transfer.Description, transfer.Amount, transfer.DisplayStatus());
            return names;
        }

        private static ImportedTransactionView ToImportedView(PostedTransaction posted, IReadOnlyDictionary<Guid, string> expectedNames)
        {
            expectedNames.TryGetValue(posted.RecommendedExpectedId ?? Guid.Empty, out var recommended);
            return new ImportedTransactionView
            {
                Id = posted.Id,
                AccountId = posted.AccountId,
                Date = posted.Date,
                Description = posted.Description ?? "",
                Amount = posted.Amount,
                Category = posted.Category,
                CategoryId = posted.CategoryId,
                Status = posted.DisplayStatus,
                RecommendedExpectedId = posted.RecommendedExpectedId,
                RecommendedMatch = recommended ?? "",
                Type = posted is PostedTransferTransaction
                    ? UnifiedTransactionView.PostedTransferType
                    : UnifiedTransactionView.PostedType
            };
        }

        private static string FormatExpected(DateTime date, string? description, decimal amount, string status) =>
            $"{date:d}  {description}  {amount:c2}  ({status})";
    }

    public sealed class ExpectedChoice
    {
        public Guid Id { get; init; }
        public Guid AccountId { get; init; }
        public Guid? CounterpartyAccountId { get; init; }
        public DateTime Date { get; init; }
        public string Description { get; init; } = "";
        public decimal Amount { get; init; }
        public string Status { get; init; } = "";

        public string TitleLine => $"{Date:d}  {Amount:c2}  ({Status})";

        public string DetailLine => Description;

        public override string ToString() =>
            $"{TitleLine}  {DetailLine}";
    }

    public sealed class ImportedTransactionView
    {
        public Guid Id { get; init; }
        public Guid AccountId { get; init; }
        public DateTime Date { get; init; }
        public string Description { get; init; } = "";
        public decimal Amount { get; init; }
        public string? Category { get; init; }
        public Guid? CategoryId { get; init; }
        public string Status { get; init; } = TransactionStatuses.Unreconciled;
        public Guid? RecommendedExpectedId { get; init; }
        public string RecommendedMatch { get; init; } = "";
        public string Type { get; init; } = "";
    }
}
