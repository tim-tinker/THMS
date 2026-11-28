namespace THMS.Domain.Finance.Planning
{
    /// <summary>
    /// Dollar limits for one person's 401(k) elective deferrals.
    /// Traditional and Roth deferrals share <see cref="ElectiveDeferral"/>. Catch-up is additional.
    /// </summary>
    public sealed record Retirement401kLimits
    {
        /// <summary>The calendar year these dollar amounts apply to. The amounts are entered for that year.</summary>
        public int TaxYear { get; init; }

        /// <summary>IRC 402(g) employee elective deferral limit. Traditional and Roth combined.</summary>
        public decimal ElectiveDeferral { get; init; }

        /// <summary>IRC 414(v) catch-up for a participant who is age 50 or older and not in the higher age band.</summary>
        public decimal CatchUp { get; init; }

        /// <summary>SECURE 2.0 higher catch-up for a participant who turns 60, 61, 62, or 63 during the year.</summary>
        public decimal HigherCatchUp { get; init; }

        /// <summary>
        /// IRC 414(v)(7). Catch-up must be Roth when prior-year wages from the plan sponsor exceed this amount.
        /// </summary>
        public decimal RothCatchUpWageThreshold { get; init; }

        /// <summary>IRC 415(c) annual additions limit. Catch-up contributions do not count toward it.</summary>
        public decimal AnnualAdditions { get; init; }

        /// <summary>IRC 401(a)(17). Match formulas ignore pay above this amount.</summary>
        public decimal CompensationCap { get; init; }

        /// <summary>
        /// 2026 amounts from IRS Notice 2025-67 and IR-2025-111.
        /// The Roth catch-up threshold is the 2025 wage figure used to test 2026 catch-up contributions.
        /// </summary>
        public static Retirement401kLimits For2026() => new()
        {
            TaxYear = 2026,
            ElectiveDeferral = 24_500m,
            CatchUp = 8_000m,
            HigherCatchUp = 11_250m,
            RothCatchUpWageThreshold = 150_000m,
            AnnualAdditions = 72_000m,
            CompensationCap = 360_000m
        };
    }
}
