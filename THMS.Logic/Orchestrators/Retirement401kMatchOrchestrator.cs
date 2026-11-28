using THMS.Data.Stores;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Orchestrators
{
    public class Retirement401kMatchOrchestrator
    {
        private readonly IRetirement401kMatchStore _match;

        public Retirement401kMatchOrchestrator()
            : this(new DataStoreFactory().GetRetirement401kMatchStore())
        {
        }

        public Retirement401kMatchOrchestrator(IRetirement401kMatchStore match)
        {
            _match = match;
        }

        public Retirement401kMatchFormula? Get() => _match.Get();

        public Retirement401kMatchFormula? Get(Guid personId, string employer)
        {
            var scoped = _match.GetFor(personId, employer.Trim());
            return scoped ?? Get();
        }

        public void Save(Retirement401kMatchFormula formula)
        {
            ArgumentNullException.ThrowIfNull(formula);
            RequireFormula(formula);
            _match.Save(formula);
        }

        public void Save(Guid personId, string employer, Retirement401kMatchFormula formula)
        {
            ArgumentNullException.ThrowIfNull(formula);
            if (personId == Guid.Empty)
                throw new InvalidOperationException("Person is required.");
            employer = employer.Trim();
            if (employer.Length == 0)
                throw new InvalidOperationException("Employer is required.");
            RequireFormula(formula);
            _match.SaveFor(personId, employer, formula);
        }

        private static void RequireFormula(Retirement401kMatchFormula formula)
        {
            RequireBand(formula.FirstMatchRate, formula.FirstMatchOfPay, "First");
            RequireBand(formula.NextMatchRate, formula.NextMatchOfPay, "Next");
        }

        private static void RequireBand(decimal rate, decimal portionOfPay, string name)
        {
            if (rate < 0m)
                throw new InvalidOperationException($"{name} match rate cannot be negative.");
            if (portionOfPay < 0m || portionOfPay > 1m)
                throw new InvalidOperationException($"{name} band must be from 0% through 100% of pay.");
        }
    }
}
