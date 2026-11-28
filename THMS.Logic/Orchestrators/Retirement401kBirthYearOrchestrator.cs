using THMS.Data.Stores;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Orchestrators
{
    public class Retirement401kBirthYearOrchestrator
    {
        private readonly IRetirement401kBirthYearStore _birthYear;

        public Retirement401kBirthYearOrchestrator()
            : this(new DataStoreFactory().GetRetirement401kBirthYearStore())
        {
        }

        public Retirement401kBirthYearOrchestrator(IRetirement401kBirthYearStore birthYear)
        {
            _birthYear = birthYear;
        }

        public Retirement401kBirthYear? Get() => _birthYear.Get();

        public void Save(Retirement401kBirthYear birthYear)
        {
            ArgumentNullException.ThrowIfNull(birthYear);
            if (birthYear.Year < 1900 || birthYear.Year > 2100)
                throw new InvalidOperationException("Birth year must be from 1900 through 2100.");
            _birthYear.Save(birthYear);
        }

        /// <summary>Age on December 31 of the tax year. A missing birth year returns 0.</summary>
        public static int AgeAtYearEnd(int? birthYear, int taxYear)
        {
            if (birthYear is not int year || year < 1900)
                return 0;
            return Math.Max(0, taxYear - year);
        }
    }
}
