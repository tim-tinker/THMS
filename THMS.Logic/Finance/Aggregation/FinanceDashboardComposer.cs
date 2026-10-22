using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Categories;
using THMS.Logic.Finance.Model;
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
            IReadOnlyList<ExpenseCategory> categories,
            IReadOnlyList<CashContribution>? activity = null)
        {
            var today = asOf.Date;
            var names = accounts.ToDictionary(a => a.Id, a => a.Name);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            activity ??= CategoryCashFlow.Read(
                posted,
                postedTransfers,
                categories,
                accounts.GroupBy(a => a.Id).ToDictionary(g => g.Key, g => g.First()));
            var balance = BudgetBalance.Compute(
                today,
                budgetRules,
                activePeriods,
                categories,
                activity);

            var totals = SumAccounts(accounts);
            var budgets = BuildBudgets(today, budgetRules, activePeriods, categories);
            var payments = BuildPayments(today, accounts, recurringSingles, names);
            var uncategorized = posted.Count(IsUncategorized);
            var mismatch = posted.Count(t => t.HasSplits && !SplitTransactionMath.AmountsMatch(t.Amount, t.Splits));
            var interestSpike = HasInterestSpike(today, posted);
            var alerts = BuildAlerts(accounts, budgets, payments, today, uncategorized, mismatch, interestSpike);
            var slices = BuildCategorySlices(activity, monthStart, categories);
            var trend = BuildMonthlyTrend(today, activity);
            var chartStart = monthStart.AddMonths(-11);
            var rows = activity
                .Where(line => line.Date.Date >= chartStart)
                .Select(line => new FinanceDashboardActivityRow
            {
                Month = new DateTime(line.Date.Year, line.Date.Month, 1),
                Date = line.Date,
                Description = line.Description,
                Account = line.AccountName,
                CategoryId = line.CategoryId,
                Category = line.CategoryName,
                Role = line.Role.ToString(),
                Amount = line.Amount,
                Effect = line.Effect,
                Spending = line.Spending,
                Income = line.Income,
                SetAside = line.SetAside
            }).ToList();
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
                BankAccounts = totals.Banks,
                CreditAccounts = totals.Credits,
                LoanAccounts = totals.LoansAccounts,
                InvestmentAccounts = totals.Investments,
                Budgets = budgets,
                UpcomingPayments = payments,
                Alerts = alerts,
                CategorySlices = slices,
                MonthlyTrend = trend,
                Activity = rows,
                Balance = balance,
                RecentPosted = recent,
                UpcomingForecast = upcoming,
                UncategorizedCount = uncategorized
            };
        }

        private static AccountTotals SumAccounts(IReadOnlyList<Account> accounts)
        {
            var totals = new AccountTotals();
            foreach (var account in accounts)
            {
                switch (account)
                {
                    case BankAccount bankAccount:
                        totals.Bank += bankAccount.PostedBalance;
                        totals.Banks.Add(Line(bankAccount, bankAccount.PostedBalance));
                        break;
                    case CreditAccount creditAccount:
                        var owed = CreditOwed(creditAccount);
                        totals.Credit += owed;
                        totals.Credits.Add(Line(creditAccount, owed));
                        break;
                    case LoanAccount loan:
                        var loanOwed = PostedBalanceCalculator.ToDisplayBalance(loan, loan.Principal);
                        totals.Loans += loanOwed;
                        totals.LoansAccounts.Add(Line(loan, loanOwed));
                        break;
                    case MortgageAccount mortgage:
                        var mortgageOwed = PostedBalanceCalculator.ToDisplayBalance(mortgage, mortgage.Principal);
                        totals.Loans += mortgageOwed;
                        totals.LoansAccounts.Add(Line(mortgage, mortgageOwed));
                        break;
                    case InvestmentAccount invested:
                        totals.Investment += invested.CashBalance;
                        totals.Investments.Add(Line(invested, invested.CashBalance));
                        break;
                }
            }

            Sort(totals.Banks);
            Sort(totals.Credits);
            Sort(totals.LoansAccounts);
            Sort(totals.Investments);
            return totals;
        }

        private static FinanceDashboardAccountLine Line(Account account, decimal balance) =>
            new()
            {
                AccountId = account.Id,
                Name = account.Name ?? "",
                Balance = balance
            };

        private static void Sort(List<FinanceDashboardAccountLine> lines) =>
            lines.Sort((left, right) =>
                string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase));

        private sealed class AccountTotals
        {
            public decimal Bank { get; set; }
            public decimal Credit { get; set; }
            public decimal Loans { get; set; }
            public decimal Investment { get; set; }
            public List<FinanceDashboardAccountLine> Banks { get; } = [];
            public List<FinanceDashboardAccountLine> Credits { get; } = [];
            public List<FinanceDashboardAccountLine> LoansAccounts { get; } = [];
            public List<FinanceDashboardAccountLine> Investments { get; } = [];
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
            IReadOnlyList<ExpenseCategory> categories)
        {
            var rows = new List<FinanceDashboardBudgetRow>();
            foreach (var rule in budgetRules.Where(r => r.IsActive).OrderBy(r => r.BudgetName))
            {
                if (!activePeriods.TryGetValue(rule.Id, out var period) || period is null)
                    continue;

                var role = BudgetBalance.UniformRole(rule, categories) ?? CategoryRole.Expense;
                rows.Add(new FinanceDashboardBudgetRow
                {
                    RuleId = rule.Id,
                    BudgetName = rule.BudgetName,
                    Remaining = period.Remaining,
                    PeriodStart = period.PeriodStart.Date,
                    PeriodEnd = period.PeriodEnd.Date,
                    Recommended = period.RecommendedAmount,
                    Status = BudgetStatus(period, today, rule.BudgetFrequency, role),
                    Role = role
                });
            }

            return rows;
        }

        public static string BudgetStatus(
            ExpenseBudgetHistory period,
            DateTime today,
            BudgetFrequency frequency,
            CategoryRole role = CategoryRole.Expense)
        {
            var parts = new List<string>();
            if (period.Remaining < 0 && role == CategoryRole.Expense)
                parts.Add("Overspent");
            var daysLeft = (period.PeriodEnd.Date - today.Date).TotalDays;
            if (daysLeft >= 0 && daysLeft <= frequency.SoonDays())
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
            int uncategorized,
            int mismatch,
            bool interestSpike)
        {
            var alerts = new List<string>();

            foreach (var budget in budgets.Where(b => b.Role == CategoryRole.Expense && b.Remaining < 0))
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
            if (mismatch > 0)
                alerts.Add("Split mismatch detected");
            if (interestSpike)
                alerts.Add("Loan interest spike detected");

            return alerts;
        }

        private static List<FinanceDashboardCategorySlice> BuildCategorySlices(
            IReadOnlyList<CashContribution> activity,
            DateTime monthStart,
            IReadOnlyList<ExpenseCategory> categories)
        {
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var names = categories.ToDictionary(c => c.Id, c => c.Name);
            var totals = new Dictionary<Guid, decimal>();
            foreach (var line in activity.Where(line => line.Date.Date >= monthStart && line.Date.Date <= monthEnd))
            {
                if (line.Spending == 0)
                    continue;
                totals[line.CategoryId] = totals.GetValueOrDefault(line.CategoryId) + line.Spending;
            }

            var slices = totals
                .Select(pair => new { pair.Key, Value = pair.Value })
                .Where(pair => pair.Value > 0)
                .Select(pair => new FinanceDashboardCategorySlice
                {
                    CategoryId = pair.Key,
                    Name = names.GetValueOrDefault(pair.Key, DefaultExpenseCategories.Uncategorized),
                    Amount = pair.Value
                })
                .OrderByDescending(s => s.Amount)
                .ToList();

            if (slices.Count <= PieSliceLimit)
                return slices;

            var top = slices.Take(PieSliceLimit - 1).ToList();
            var folded = slices.Skip(PieSliceLimit - 1).ToList();
            top.Add(new FinanceDashboardCategorySlice
            {
                Name = "Other",
                IsOther = true,
                Amount = folded.Sum(s => s.Amount),
                Folded = folded
            });
            return top;
        }

        private static List<FinanceDashboardMonthlyPoint> BuildMonthlyTrend(
            DateTime today,
            IReadOnlyList<CashContribution> activity)
        {
            var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            var points = new List<FinanceDashboardMonthlyPoint>();
            for (var i = 0; i < 12; i++)
            {
                var month = start.AddMonths(i);
                var monthEnd = month.AddMonths(1).AddDays(-1);
                var monthLines = activity.Where(line => line.Date.Date >= month && line.Date.Date <= monthEnd).ToList();
                points.Add(new FinanceDashboardMonthlyPoint
                {
                    Month = month,
                    Label = month.ToString("MMM yyyy"),
                    Spending = monthLines.Sum(line => line.Spending),
                    Income = monthLines.Sum(line => line.Income)
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

        private static bool HasInterestSpike(DateTime today, IReadOnlyList<PostedTransaction> posted)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var previousStart = monthStart.AddMonths(-1);
            decimal current = 0;
            decimal previous = 0;
            foreach (var transaction in posted.Where(t => t.HasSplits))
            {
                foreach (var split in transaction.Splits.Where(s => s.Type == SplitType.Interest))
                {
                    var date = transaction.Date.Date;
                    if (date >= monthStart && date < monthStart.AddMonths(1))
                        current += Math.Abs(split.Amount);
                    else if (date >= previousStart && date < monthStart)
                        previous += Math.Abs(split.Amount);
                }
            }

            return previous > 0 && current > previous * 1.5m;
        }

        public static bool IsUncategorized(PostedTransaction transaction)
        {
            if (transaction.HasSplits)
            {
                return transaction.Splits.Any(split =>
                    SplitTransactionMath.RequiresCategory(split.Type) &&
                    SplitTransactionMath.IsUncategorized(split.CategoryId, split.Category));
            }

            return SplitTransactionMath.IsUncategorized(transaction.CategoryId, transaction.Category);
        }
    }
}
