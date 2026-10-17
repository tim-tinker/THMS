using System.Text.RegularExpressions;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Diagnostics
{
    public sealed class EntityKeyDescriber
    {
        private static readonly Regex GuidPattern = new(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;
        private readonly IAccountStatementDataStore _statements;

        public EntityKeyDescriber()
            : this(
                new DataStoreFactory().GetAccountStore(),
                new DataStoreFactory().GetTransactionStore(),
                new DataStoreFactory().GetAccountStatementStore())
        {
        }

        public EntityKeyDescriber(
            IAccountDataStore accounts,
            ITransactionDataStore transactions,
            IAccountStatementDataStore statements)
        {
            _accounts = accounts;
            _transactions = transactions;
            _statements = statements;
        }

        public IReadOnlyList<string> DescribeIdsIn(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            var ids = GuidPattern.Matches(text)
                .Select(match => Guid.Parse(match.Value))
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return [];

            var names = _accounts.GetAllAccounts()
                .GroupBy(account => account.Id)
                .ToDictionary(group => group.Key, group => group.First().Name ?? "");
            var mentioned = ids.ToHashSet();
            var lines = new List<string>();
            foreach (var id in ids)
            {
                var hits = Find(id, names);
                if (hits.Count == 0)
                {
                    lines.Add($"{id:D} was not found as an account, transaction, transfer, category, budget, rule, or statement.");
                    continue;
                }

                foreach (var hit in hits)
                    lines.Add($"{hit.Kind} {id:D}: {hit.Detail}");

                foreach (var relatedId in hits.SelectMany(hit => hit.RelatedIds).Distinct())
                {
                    if (relatedId == Guid.Empty || mentioned.Contains(relatedId))
                        continue;

                    var related = Find(relatedId, names);
                    if (related.Count == 0)
                    {
                        lines.Add($"  Related object {relatedId:D} was not found.");
                        continue;
                    }

                    foreach (var hit in related)
                        lines.Add($"  Related {hit.Kind.ToLowerInvariant()} {relatedId:D}: {hit.Detail}");
                }
            }

            return lines;
        }

        private List<Hit> Find(Guid id, IReadOnlyDictionary<Guid, string> names)
        {
            var hits = new List<Hit>();

            if (names.ContainsKey(id))
                hits.Add(new Hit("Account", Name(names, id), []));

            var posted = _transactions.GetPostedTransaction(id);
            if (posted is not null)
                hits.Add(new Hit("Posted transaction", TransactionDetail(posted, names), []));

            var transfer = _transactions.GetPostedTransferTransaction(id);
            if (transfer is not null)
            {
                hits.Add(new Hit(
                    "Posted transfer",
                    TransactionDetail(transfer, names),
                    transfer.RelatedPostedTransactionId == Guid.Empty ? [] : [transfer.RelatedPostedTransactionId]));
            }

            var expected = _transactions.GetFutureSingleTransaction(id);
            if (expected is not null)
                hits.Add(new Hit("Expected transaction", TransactionDetail(expected, expected.AccountId, names), []));

            var expectedTransfer = _transactions.GetFutureTransferTransaction(id);
            if (expectedTransfer is not null)
            {
                hits.Add(new Hit(
                    "Expected transfer",
                    $"{When(expectedTransfer)} between {Name(names, expectedTransfer.FromAccountId)} and {Name(names, expectedTransfer.ToAccountId)}",
                    Related(expectedTransfer.PostedFromTransactionId, expectedTransfer.PostedToTransactionId)));
            }

            var singleRule = _transactions.GetRecurringSingleRule(id);
            if (singleRule is not null)
                hits.Add(new Hit("Recurring transaction rule", TransactionDetail(singleRule, names), []));

            var transferRule = _transactions.GetRecurringTransferRule(id);
            if (transferRule is not null)
            {
                hits.Add(new Hit(
                    "Recurring transfer rule",
                    $"{When(transferRule)} between {Name(names, transferRule.FromAccountId)} and {Name(names, transferRule.ToAccountId)}",
                    []));
            }

            var category = _transactions.GetCategory(id);
            if (category is not null)
                hits.Add(new Hit("Category", category.Name, []));

            var budget = _transactions.GetExpenseBudgetRule(id);
            if (budget is not null)
                hits.Add(new Hit("Budget", budget.BudgetName, budget.AccountId == Guid.Empty ? [] : [budget.AccountId]));

            var period = _transactions.GetExpenseBudgetHistoryById(id);
            if (period is not null)
            {
                hits.Add(new Hit(
                    "Budget period",
                    $"{period.PeriodStart:d} to {period.PeriodEnd:d}",
                    [period.BudgetRuleId]));
            }

            var statement = _statements.Get(id);
            if (statement is not null)
                hits.Add(new Hit("Statement", StatementDetail(statement, names), [statement.AccountId]));

            var reconciliation = _transactions.GetReconciliation(id);
            if (reconciliation is not null)
            {
                hits.Add(new Hit(
                    "Reconciliation",
                    $"accepted {reconciliation.AcceptedOn:g}",
                    Related(reconciliation.ImportedTransactionId, reconciliation.ExpectedTransactionId)));
            }

            return hits;
        }

        private static string TransactionDetail(BaseSingleAccountTransaction transaction, IReadOnlyDictionary<Guid, string> names) =>
            TransactionDetail(transaction, transaction.AccountId, names);

        private static string TransactionDetail(BaseTransaction transaction, Guid accountId, IReadOnlyDictionary<Guid, string> names)
        {
            var description = string.IsNullOrWhiteSpace(transaction.Description)
                ? "(no description)"
                : transaction.Description.Trim();
            return $"{transaction.Date:d}, {description}, {transaction.Amount:c2}, {Name(names, accountId)}";
        }

        private static string When(BaseTransaction transaction)
        {
            var description = string.IsNullOrWhiteSpace(transaction.Description)
                ? "(no description)"
                : transaction.Description.Trim();
            return $"{transaction.Date:d}, {description}, {transaction.Amount:c2}";
        }

        private static string StatementDetail(AccountStatement statement, IReadOnlyDictionary<Guid, string> names) =>
            $"{statement.StatementDate:d}, due {statement.DueDate:d}, {statement.AmountDue:c2}, {Name(names, statement.AccountId)}";

        private static string Name(IReadOnlyDictionary<Guid, string> names, Guid id) =>
            id != Guid.Empty && names.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "unknown account";

        private static Guid[] Related(params Guid?[] ids) =>
            ids.OfType<Guid>().Where(id => id != Guid.Empty).Distinct().ToArray();

        private readonly record struct Hit(string Kind, string Detail, Guid[] RelatedIds);
    }
}
