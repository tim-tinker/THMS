using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;

namespace THMS.Logic.Orchestrators.Finance
{
    public class LoanDiagnosticsOrchestrator
    {
        private readonly IAccountDataStore _accounts;
        private readonly ITransactionDataStore _transactions;

        public LoanDiagnosticsOrchestrator()
            : this(new DataStoreFactory().GetAccountStore(), new DataStoreFactory().GetTransactionStore())
        {
        }

        public LoanDiagnosticsOrchestrator(IAccountDataStore accounts, ITransactionDataStore transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
        }

        public List<string> Run()
        {
            var findings = new List<string>();
            var accounts = _accounts.GetAllAccounts().ToList();
            var loans = accounts.OfType<LoanAccount>().ToList();
            var mortgages = accounts.OfType<MortgageAccount>().ToList();
            var posted = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var recurring = _transactions.GetAllRecurringSingleRules().ToList();

            FindMissingLoanMetadata(loans, findings);
            FindMissingMortgageMetadata(mortgages, findings);
            FindMissingPayments(loans, mortgages, posted, recurring, findings);
            FindUnsplitPayments(loans, mortgages, posted, findings);
            FindNegativeRemainingPrincipal(loans, mortgages, posted, findings);
            FindAmortizationIssues(loans, mortgages, findings);
            FindInterestSpike(posted, findings);

            if (findings.Count == 0)
                findings.Add("No issues found.");

            return findings;
        }

        private static void FindMissingLoanMetadata(IReadOnlyList<LoanAccount> loans, List<string> findings)
        {
            foreach (var loan in loans)
            {
                var missing = new List<string>();
                if (loan.Principal <= 0)
                    missing.Add("principal");
                if (loan.InterestRate <= 0)
                    missing.Add("interest rate");
                if (loan.TermMonths <= 0)
                    missing.Add("term");
                if (loan.StartDate == default)
                    missing.Add("start date");
                if (missing.Count > 0)
                    findings.Add($"Missing loan metadata ({string.Join(", ", missing)}): {loan.Name}");
            }
        }

        private static void FindMissingMortgageMetadata(IReadOnlyList<MortgageAccount> mortgages, List<string> findings)
        {
            foreach (var mortgage in mortgages)
            {
                var missing = new List<string>();
                if (mortgage.Principal <= 0)
                    missing.Add("principal");
                if (mortgage.InterestRate <= 0)
                    missing.Add("interest rate");
                if (mortgage.TermMonths <= 0)
                    missing.Add("term");
                if (mortgage.NextPaymentDate == default)
                    missing.Add("next payment date");
                if (missing.Count > 0)
                    findings.Add($"Missing mortgage metadata ({string.Join(", ", missing)}): {mortgage.Name}");
            }
        }

        private static void FindMissingPayments(
            IReadOnlyList<LoanAccount> loans,
            IReadOnlyList<MortgageAccount> mortgages,
            IReadOnlyList<PostedTransaction> posted,
            IReadOnlyList<RecurringSingleTransactionRule> recurring,
            List<string> findings)
        {
            foreach (var loan in loans)
            {
                if (!posted.Any(t => t.AccountId == loan.Id) &&
                    !recurring.Any(r => r.IsActive && r.AccountId == loan.Id))
                    findings.Add($"Loan with no payments: {loan.Name}");
            }

            foreach (var mortgage in mortgages)
            {
                if (!posted.Any(t => t.AccountId == mortgage.Id) &&
                    !recurring.Any(r => r.IsActive && r.AccountId == mortgage.Id))
                    findings.Add($"Mortgage with no payments: {mortgage.Name}");
            }
        }

        private static void FindUnsplitPayments(
            IReadOnlyList<LoanAccount> loans,
            IReadOnlyList<MortgageAccount> mortgages,
            IReadOnlyList<PostedTransaction> posted,
            List<string> findings)
        {
            var loanIds = loans.Select(l => l.Id).Concat(mortgages.Select(m => m.Id)).ToHashSet();
            foreach (var transaction in posted.Where(t => loanIds.Contains(t.AccountId) && t.Amount != 0))
            {
                var hasLoanSplit = transaction.HasSplits &&
                                  transaction.Splits.Any(s => s.Type is SplitType.Principal or SplitType.Interest);
                if (!hasLoanSplit)
                    findings.Add($"Loan payment is not split into principal/interest: {transaction.Description} ({transaction.Date:d})");
            }
        }

        private static void FindNegativeRemainingPrincipal(
            IReadOnlyList<LoanAccount> loans,
            IReadOnlyList<MortgageAccount> mortgages,
            IReadOnlyList<PostedTransaction> posted,
            List<string> findings)
        {
            foreach (var loan in loans.Where(l => l.Principal > 0))
            {
                var remaining = RemainingPrincipal(loan.Principal, posted.Where(t => t.AccountId == loan.Id));
                if (remaining < 0)
                    findings.Add($"Remaining principal is negative: {loan.Name} ({remaining:c2})");
            }

            foreach (var mortgage in mortgages.Where(m => m.Principal > 0))
            {
                var remaining = RemainingPrincipal(mortgage.Principal, posted.Where(t => t.AccountId == mortgage.Id));
                if (remaining < 0)
                    findings.Add($"Remaining principal is negative: {mortgage.Name} ({remaining:c2})");
            }
        }

        private static void FindAmortizationIssues(
            IReadOnlyList<LoanAccount> loans,
            IReadOnlyList<MortgageAccount> mortgages,
            List<string> findings)
        {
            var engine = new LoanAmortizationEngine();
            foreach (var loan in loans)
                InspectSchedule(loan.Name, loan.Principal, loan.InterestRate, loan.TermMonths, engine, findings);
            foreach (var mortgage in mortgages)
                InspectSchedule(mortgage.Name, mortgage.Principal, mortgage.InterestRate, mortgage.TermMonths, engine, findings);
        }

        private static void InspectSchedule(
            string name,
            decimal principal,
            decimal interestRate,
            int termMonths,
            LoanAmortizationEngine engine,
            List<string> findings)
        {
            if (principal <= 0 || interestRate <= 0 || termMonths <= 0)
                return;

            var rate = NormalizeRate(interestRate);
            var monthly = MonthlyPayment(principal, rate, termMonths);
            var monthlyInterest = principal * rate / 12m;
            if (monthly <= monthlyInterest)
                findings.Add($"Payment does not cover interest: {name}");

            var schedule = engine.GenerateSchedule(principal, rate, termMonths, monthly);
            if (schedule.Count > 0 && schedule.Last().RemainingPrincipal > 0.01m)
                findings.Add($"Amortization does not reach zero: {name}");
        }

        private static void FindInterestSpike(IReadOnlyList<PostedTransaction> posted, List<string> findings)
        {
            var today = DateTime.Today;
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

            if (previous > 0 && current > previous * 1.5m)
                findings.Add($"Loan interest spike detected ({previous:c2} last month vs {current:c2} this month)");
        }

        private static decimal RemainingPrincipal(decimal originalPrincipal, IEnumerable<PostedTransaction> posted)
        {
            var splits = posted.Where(t => t.HasSplits).SelectMany(t => t.Splits);
            return SplitTransactionMath.SummarizeLoanPayments(originalPrincipal, splits).RemainingPrincipal;
        }

        private static decimal NormalizeRate(decimal annualRate) =>
            annualRate > 1m ? annualRate / 100m : annualRate;

        private static decimal MonthlyPayment(decimal principal, decimal annualRate, int termMonths)
        {
            if (annualRate <= 0)
                return principal / termMonths;

            var monthlyRate = annualRate / 12m;
            var factor = (decimal)Math.Pow((double)(1 + monthlyRate), termMonths);
            return principal * monthlyRate * factor / (factor - 1);
        }
    }
}
