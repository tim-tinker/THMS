using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Aggregation
{
    public class FinanceDashboardComposer
    {
        public const decimal HighUtilizationThreshold = 0.30m;
        public const int SoonDays = 7;
        public const int RecentCount = 10;
        public const int ForecastCount = 10;
        public const int PieSliceLimit = 6;

        public FinanceDashboardSnapshot Compose(
            DateTime asOf,
            IReadOnlyList<Account> accounts,
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<PostedTransferTransaction> postedTransfers,
            IReadOnlyList<ExpenseBudgetRule> budgetRules,
            IReadOnlyDictionary<Guid, ExpenseBudgetHistory?> activePeriods,
            IReadOnlyList<RecurringSingleTransactionRule> recurringSingles,
            IReadOnlyList<UnifiedTransactionView> forecast,
            IReadOnlyList<ExpenseCategory> categories)
        {
            var today = asOf.Date;
            var names = accounts.ToDictionary(a => a.Id, a => a.Name);
            var budgetedIds = BudgetedCategoryIds(budgetRules.Where(r => r.IsActive), categories);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthPosted = posted.Where(t => t.Date.Date >= monthStart && t.Date.Date <= monthEnd).ToList();

            var totals = SumAccounts(accounts);
            var budgets = BuildBudgets(today, budgetRules, activePeriods, names);
            var payments = BuildPayments(today, accounts, recurringSingles, names);
            var uncategorized = posted.Count(IsUncategorized);
            var alerts = BuildAlerts(accounts, budgets, payments, today, uncategorized);
            var slices = BuildCategorySlices(monthPosted, budgetedIds, categories);
            var trend = BuildMonthlyTrend(today, posted, budgetedIds);
            var recent = BuildRecent(posted, postedTransfers);
            var upcoming = forecast
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Description)
                .Take(ForecastCount)
                .ToList();

            return new FinanceDashboardSnapshot
            {
                BankBalance = totals.Bank,
                CreditOwed = totals.Credit,
                LoanPrincipal = totals.Loans,
                InvestmentCash = totals.Investment,
                NetLiquid = totals.Bank + totals.Investment - totals.Credit,
                NetPosition = totals.Bank + totals.Investment - totals.Credit - totals.Loans,
                Budgets = budgets,
                UpcomingPayments = payments,
                Alerts = alerts,
                CategorySlices = slices,
                MonthlyTrend = trend,
                RecentPosted = recent,
                UpcomingForecast = upcoming,
                UncategorizedCount = uncategorized
            };
        }

        private static (decimal Bank, decimal Credit, decimal Loans, decimal Investment) SumAccounts(
            IReadOnlyList<Account> accounts)
        {
            decimal bank = 0, credit = 0, loans = 0, investment = 0;
            foreach (var account in accounts)
            {
                switch (account)
                {
                    case BankAccount bankAccount:
                        bank += bankAccount.PostedBalance;
                        break;
                    case CreditAccount creditAccount:
                        credit += CreditOwed(creditAccount);
                        break;
                    case LoanAccount loan:
                        loans += loan.Principal;
                        break;
                    case MortgageAccount mortgage:
                        loans += mortgage.Principal;
                        break;
                    case InvestmentAccount invested:
                        investment += invested.CashBalance;
                        break;
                }
            }

            return (bank, credit, loans, investment);
        }

        public static decimal CreditOwed(CreditAccount credit) =>
            Math.Max(0m, -credit.PostedBalance);

        public static decimal Utilization(CreditAccount credit)
        {
            if (credit.CreditLimit <= 0)
                return 0;
            return CreditOwed(credit) / credit.CreditLimit;
        }

        private static List<FinanceDashboardBudgetRow> BuildBudgets(
            DateTime today,
            IReadOnlyList<ExpenseBudgetRule> budgetRules,
            IReadOnlyDictionary<Guid, ExpenseBudgetHistory?> activePeriods,
            IReadOnlyDictionary<Guid, string> names)
        {
            var rows = new List<FinanceDashboardBudgetRow>();
            foreach (var rule in budgetRules.Where(r => r.IsActive).OrderBy(r => r.BudgetName))
            {
                if (!activePeriods.TryGetValue(rule.Id, out var period) || period is null)
                    continue;

                rows.Add(new FinanceDashboardBudgetRow
                {
                    RuleId = rule.Id,
                    AccountName = names.GetValueOrDefault(rule.AccountId, ""),
                    BudgetName = rule.BudgetName,
                    Frequency = rule.BudgetFrequency.ToString(),
                    Remaining = period.Remaining,
                    Ending = period.EndingBalance,
                    Recommended = period.RecommendedAmount,
                    Status = BudgetStatus(period, today)
                });
            }

            return rows;
        }

        public static string BudgetStatus(ExpenseBudgetHistory period, DateTime today)
        {
            var parts = new List<string>();
            if (period.Remaining < 0)
                parts.Add("Overspent");
            var daysLeft = (period.PeriodEnd.Date - today.Date).TotalDays;
            if (daysLeft >= 0 && daysLeft <= SoonDays)
                parts.Add("Ends soon");
            return parts.Count == 0 ? "OK" : string.Join(", ", parts);
        }

        private static List<FinanceDashboardPaymentRow> BuildPayments(
            DateTime today,
            IReadOnlyList<Account> accounts,
            IReadOnlyList<RecurringSingleTransactionRule> recurringSingles,
            IReadOnlyDictionary<Guid, string> names)
        {
            var rows = new List<FinanceDashboardPaymentRow>();
            var covered = new HashSet<Guid>();

            foreach (var rule in recurringSingles.Where(r => r.IsActive))
            {
                rows.Add(new FinanceDashboardPaymentRow
                {
                    Date = rule.NextOccurrence.Date,
                    AccountName = names.GetValueOrDefault(rule.AccountId, ""),
                    Description = string.IsNullOrWhiteSpace(rule.Description) ? "Recurring" : rule.Description!,
                    Amount = rule.Amount
                });
                covered.Add(rule.AccountId);
            }

            foreach (var account in accounts)
            {
                if (covered.Contains(account.Id))
                    continue;

                DateTime? due = account switch
                {
                    CreditAccount credit when credit.DueDate != default => credit.DueDate.Date,
                    MortgageAccount mortgage when mortgage.NextPaymentDate != default => mortgage.NextPaymentDate.Date,
                    _ => null
                };
                if (due is null)
                    continue;

                rows.Add(new FinanceDashboardPaymentRow
                {
                    Date = due.Value,
                    AccountName = account.Name,
                    Description = account is CreditAccount ? "Statement due" : "Mortgage payment",
                    Amount = null
                });
            }

            return rows
                .OrderBy(r => r.Date)
                .ThenBy(r => r.AccountName)
                .Take(12)
                .ToList();
        }

        private static List<string> BuildAlerts(
            IReadOnlyList<Account> accounts,
            IReadOnlyList<FinanceDashboardBudgetRow> budgets,
            IReadOnlyList<FinanceDashboardPaymentRow> payments,
            DateTime today,
            int uncategorized)
        {
            var alerts = new List<string>();

            foreach (var budget in budgets.Where(b => b.Remaining < 0))
                alerts.Add($"Overspent: {budget.BudgetName} ({budget.Remaining:c2})");

            foreach (var credit in accounts.OfType<CreditAccount>())
            {
                var utilization = Utilization(credit);
                if (utilization > HighUtilizationThreshold)
                    alerts.Add($"High utilization: {credit.Name} ({utilization:p0})");
            }

            foreach (var payment in payments.Where(p => p.Date >= today && p.Date <= today.AddDays(SoonDays)))
                alerts.Add($"Payment due {payment.Date:d}: {payment.Description}");

            foreach (var bank in accounts.OfType<BankAccount>())
            {
                if (bank.PostedBalance < 0)
                    alerts.Add($"Negative balance: {bank.Name} ({bank.PostedBalance:c2})");
            }

            if (uncategorized > 0)
                alerts.Add($"{uncategorized} uncategorized transaction{(uncategorized == 1 ? "" : "s")}");

            return alerts;
        }

        private static List<FinanceDashboardCategorySlice> BuildCategorySlices(
            IReadOnlyList<PostedTransaction> monthPosted,
            HashSet<Guid> budgetedIds,
            IReadOnlyList<ExpenseCategory> categories)
        {
            var names = categories.ToDictionary(c => c.Id, c => c.Name);
            var totals = new Dictionary<Guid, decimal>();
            foreach (var transaction in monthPosted)
                ApplyNet(totals, transaction, budgetedIds);

            var slices = totals
                .Select(pair => new { pair.Key, Value = Math.Max(0, pair.Value) })
                .Where(pair => pair.Value > 0)
                .Select(pair => new FinanceDashboardCategorySlice
                {
                    Name = names.GetValueOrDefault(pair.Key, DefaultExpenseCategories.Uncategorized),
                    Amount = pair.Value
                })
                .OrderByDescending(s => s.Amount)
                .ToList();

            if (slices.Count <= PieSliceLimit)
                return slices;

            var top = slices.Take(PieSliceLimit - 1).ToList();
            top.Add(new FinanceDashboardCategorySlice
            {
                Name = "Other",
                Amount = slices.Skip(PieSliceLimit - 1).Sum(s => s.Amount)
            });
            return top;
        }

        private static List<FinanceDashboardMonthlyPoint> BuildMonthlyTrend(
            DateTime today,
            IReadOnlyList<PostedTransaction> posted,
            HashSet<Guid> budgetedIds)
        {
            var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            var points = new List<FinanceDashboardMonthlyPoint>();
            for (var i = 0; i < 12; i++)
            {
                var month = start.AddMonths(i);
                var monthEnd = month.AddMonths(1).AddDays(-1);
                decimal spending = 0;
                decimal income = 0;
                foreach (var transaction in posted.Where(t => t.Date.Date >= month && t.Date.Date <= monthEnd))
                {
                    var categoryId = ResolveCategoryId(transaction);
                    if (transaction.Amount < 0)
                        spending += -transaction.Amount;
                    else if (transaction.Amount > 0 && budgetedIds.Contains(categoryId))
                        spending -= transaction.Amount;
                    else if (transaction.Amount > 0)
                        income += transaction.Amount;
                }

                points.Add(new FinanceDashboardMonthlyPoint
                {
                    Month = month,
                    Label = month.ToString("MMM yyyy"),
                    Spending = Math.Max(0, spending),
                    Income = income
                });
            }

            return points;
        }

        private static List<UnifiedTransactionView> BuildRecent(
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<PostedTransferTransaction> postedTransfers)
        {
            var views = UnifiedTransactionViewBuilder.Build(posted, postedTransfers.Where(t => t.Amount < 0));
            return UnifiedTransactionView.OrderForDisplay(views).Take(RecentCount).ToList();
        }

        private static void ApplyNet(
            Dictionary<Guid, decimal> totals,
            PostedTransaction transaction,
            HashSet<Guid> budgetedIds)
        {
            var categoryId = ResolveCategoryId(transaction);
            if (transaction.Amount < 0)
                totals[categoryId] = totals.GetValueOrDefault(categoryId) + -transaction.Amount;
            else if (transaction.Amount > 0 && budgetedIds.Contains(categoryId))
                totals[categoryId] = totals.GetValueOrDefault(categoryId) - transaction.Amount;
        }

        private static HashSet<Guid> BudgetedCategoryIds(
            IEnumerable<ExpenseBudgetRule> rules,
            IReadOnlyList<ExpenseCategory> categories)
        {
            var selected = rules.SelectMany(r => r.IncludedCategoryIds).Where(id => id != Guid.Empty);
            return ExpenseCategoryTree.ExpandWithDescendants(selected, categories);
        }

        public static bool IsUncategorized(PostedTransaction transaction)
        {
            if (transaction.CategoryId is not Guid id || id == Guid.Empty)
                return true;
            if (id == DefaultExpenseCategories.UncategorizedId)
                return true;
            return string.Equals(transaction.Category, DefaultExpenseCategories.Uncategorized, StringComparison.OrdinalIgnoreCase);
        }

        private static Guid ResolveCategoryId(PostedTransaction transaction) =>
            transaction.CategoryId is Guid id && id != Guid.Empty
                ? id
                : DefaultExpenseCategories.UncategorizedId;
    }
}
