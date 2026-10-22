using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.Finance.Budget
{
    public sealed class CashContribution
    {
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = "";
        public CategoryRole Role { get; init; }
        public decimal Amount { get; init; }
        public decimal Spending { get; init; }
        public decimal Income { get; init; }
        public decimal SetAside { get; init; }
        public DateTime Date { get; init; }
        public string Description { get; init; } = "";
        public Guid AccountId { get; init; }
        public string AccountName { get; init; } = "";

        public string Effect =>
            Spending != 0 ? "Spending" :
            Income != 0 ? "Income" :
            SetAside != 0 ? "Set aside" :
            "None";
    }

    public static class CategoryCashFlow
    {
        public static bool IsBalanceSheetLiability(Account? account) =>
            account is LoanAccount or MortgageAccount;

        public static List<CashContribution> Read(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction>? transfers,
            IReadOnlyList<ExpenseCategory> categories,
            IReadOnlyDictionary<Guid, Account>? accounts)
        {
            var lines = new List<CashContribution>();
            var seen = new HashSet<Guid>();
            var roles = CategoryRoleResolver.Map(categories);
            var names = new Dictionary<Guid, string>(categories.Count);
            foreach (var category in categories)
                names[category.Id] = category.Name;
            foreach (var transaction in posted)
                ReadOne(transaction, lines, seen, roles, names, accounts);
            if (transfers is null)
                return lines;
            foreach (var transfer in transfers)
                ReadOne(transfer, lines, seen, roles, names, accounts);
            return lines;
        }

        private static void ReadOne(
            PostedTransaction transaction,
            List<CashContribution> lines,
            HashSet<Guid> seen,
            IReadOnlyDictionary<Guid, CategoryRole> roles,
            IReadOnlyDictionary<Guid, string> names,
            IReadOnlyDictionary<Guid, Account>? accounts)
        {
            if (transaction.Id != Guid.Empty && !seen.Add(transaction.Id))
                return;

            Account? account = null;
            accounts?.TryGetValue(transaction.AccountId, out account);
            var accountName = account?.Name ?? "";
            if (transaction.HasSplits)
            {
                foreach (var split in transaction.Splits)
                {
                    if (!Counts(split.Type))
                        continue;
                    lines.Add(Contribute(
                        split.Amount,
                        split.CategoryId,
                        split.Category,
                        transaction,
                        account,
                        accountName,
                        roles,
                        names));
                }

                return;
            }

            lines.Add(Contribute(
                transaction.Amount,
                transaction.CategoryId,
                transaction.Category,
                transaction,
                account,
                accountName,
                roles,
                names));
        }

        private static bool Counts(SplitType type) =>
            type is SplitType.Expense or SplitType.Income or SplitType.Interest or SplitType.Principal;

        private static CashContribution Contribute(
            decimal amount,
            Guid? categoryId,
            string? categoryName,
            PostedTransaction transaction,
            Account? account,
            string accountName,
            IReadOnlyDictionary<Guid, CategoryRole> roles,
            IReadOnlyDictionary<Guid, string> names)
        {
            var id = categoryId is Guid value && value != Guid.Empty
                ? value
                : DefaultExpenseCategories.UncategorizedId;
            var role = CategoryRoleResolver.Resolve(id, roles);
            var name = names.TryGetValue(id, out var known)
                ? known
                : string.IsNullOrWhiteSpace(categoryName) ? DefaultExpenseCategories.Uncategorized : categoryName;
            decimal spending = 0;
            decimal income = 0;
            decimal setAside = 0;
            if (amount != 0 && !IsBalanceSheetLiability(account))
            {
                switch (role)
                {
                    case CategoryRole.Transfer:
                        break;
                    case CategoryRole.Investment when account is BankAccount:
                        setAside = -amount;
                        break;
                    case CategoryRole.Income:
                        income = amount;
                        break;
                    case CategoryRole.Expense:
                        spending = -amount;
                        break;
                }
            }

            return new CashContribution
            {
                CategoryId = id,
                CategoryName = name,
                Role = role,
                Amount = amount,
                Spending = spending,
                Income = income,
                SetAside = setAside,
                Date = transaction.Date,
                Description = transaction.Description ?? "",
                AccountId = transaction.AccountId,
                AccountName = accountName
            };
        }
    }
}
