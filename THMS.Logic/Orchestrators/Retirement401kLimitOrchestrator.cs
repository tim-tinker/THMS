using THMS.Data.Stores;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Orchestrators
{
    public class Retirement401kLimitOrchestrator
    {
        private readonly IRetirement401kLimitStore _limits;

        public Retirement401kLimitOrchestrator()
            : this(new DataStoreFactory().GetRetirement401kLimitStore())
        {
        }

        public Retirement401kLimitOrchestrator(IRetirement401kLimitStore limits)
        {
            _limits = limits;
        }

        public Retirement401kLimits? Get(int taxYear) => _limits.Get(taxYear);

        public Retirement401kLimits? GetLatest() => _limits.GetLatest();

        public void Save(Retirement401kLimits limits)
        {
            ArgumentNullException.ThrowIfNull(limits);
            if (limits.TaxYear < 1950 || limits.TaxYear > 2100)
                throw new InvalidOperationException("Tax year must be from 1950 through 2100.");
            RequireNonNegative(limits.ElectiveDeferral, "Employee limit");
            RequireNonNegative(limits.CatchUp, "Catch-up");
            RequireNonNegative(limits.HigherCatchUp, "Higher catch-up");
            RequireNonNegative(limits.RothCatchUpWageThreshold, "Roth catch-up wage threshold");
            RequireNonNegative(limits.AnnualAdditions, "Annual additions limit");
            RequireNonNegative(limits.CompensationCap, "Pay counted by the match");
            _limits.Save(limits);
        }

        private static void RequireNonNegative(decimal amount, string name)
        {
            if (amount < 0m)
                throw new InvalidOperationException($"{name} cannot be negative.");
        }
    }
}
