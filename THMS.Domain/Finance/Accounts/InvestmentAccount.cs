using THMS.Domain.Finance.Investments;

namespace THMS.Domain.Finance.Accounts
{
    public class InvestmentAccount : Account
    {
        public decimal CashBalance { get; set; }

        /// <summary>Market value of share lots. Cash stays in <see cref="CashBalance"/>.</summary>
        public decimal PositionValue { get; set; }

        /// <summary>True after a positions file has opened the share ledger. Plaid no longer replaces cash.</summary>
        public bool TracksPositions { get; set; }

        /// <summary>History on this date is already inside the opening share quantities.</summary>
        public DateTime? BookOpenedOn { get; set; }

        /// <summary>Every security in the portfolio uses this price source.</summary>
        public QuoteAccess QuoteAccess { get; set; } = QuoteAccess.Service;

        public string PriceSource => QuoteAccess == QuoteAccess.Service ? "Queryable" : "Import only";

        public decimal AccountValue => CashBalance + PositionValue;
    }
}
