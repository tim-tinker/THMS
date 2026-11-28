using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Finance.Planning
{
    public sealed record Retirement401kRequest
    {
        public int AgeAtYearEnd { get; init; }
        public decimal AnnualPay { get; init; }

        /// <summary>Prior-year Social Security wages from the employer that sponsors the plan.</summary>
        public decimal PriorYearWages { get; init; }

        public PayFrequency PayFrequency { get; init; } = PayFrequency.Biweekly;
        public bool PlanOffersRoth { get; init; } = true;

        /// <summary>Fraction, from 0 to 1, of the deferral the employee may choose that is pre-tax.</summary>
        public decimal TraditionalShare { get; init; } = 1m;

        /// <summary>Fraction saved on each pre-tax dollar. 0.22 is a 22% marginal rate.</summary>
        public decimal FederalMarginalRate { get; init; }

        /// <summary>Fraction of state income tax saved on each pre-tax dollar. Enter 0 when the state does not exclude the deferral.</summary>
        public decimal StateMarginalRate { get; init; }

        /// <summary>Employer match rate on the first band. 1 is a 100% match.</summary>
        public decimal FirstMatchRate { get; init; }

        /// <summary>Width of the first matched band, as a fraction of pay. 0.03 is the first 3% of pay.</summary>
        public decimal FirstMatchOfPay { get; init; }

        /// <summary>Employer match rate on the next band.</summary>
        public decimal NextMatchRate { get; init; }

        /// <summary>Width of the next matched band, as a fraction of pay.</summary>
        public decimal NextMatchOfPay { get; init; }

        public Retirement401kLimits? Limits { get; init; }
    }

    public sealed class Retirement401kElection
    {
        public decimal TraditionalAnnual { get; init; }
        public decimal RothAnnual { get; init; }
        public decimal EmployeeAnnual => TraditionalAnnual + RothAnnual;
        public decimal EmployerMatch { get; init; }
        public decimal TraditionalPercent { get; init; }
        public decimal RothPercent { get; init; }
        public decimal PerPayTraditional { get; init; }
        public decimal PerPayRoth { get; init; }
        public decimal PerPayFederalTaxSaved { get; init; }
        public decimal PerPayStateTaxSaved { get; init; }
        public decimal PerPayTakeHomeReduction =>
            PerPayTraditional + PerPayRoth - PerPayFederalTaxSaved - PerPayStateTaxSaved;
        public bool PercentRoundedUp { get; init; }
    }

    public sealed record Retirement401kSelectionQuote
    {
        public decimal RegularMaxPercent { get; init; }
        public decimal CatchUpMaxPercent { get; init; }
        public decimal PreTaxWithheld { get; init; }
        public decimal RothWithheld { get; init; }
        public decimal FederalTaxSaved { get; init; }
        public decimal StateTaxSaved { get; init; }
        public decimal TakeHomeLowerThanCurrent { get; init; }
        public decimal CurrentEmployerMatch { get; init; }
        public decimal SelectedEmployerMatch { get; init; }
        public decimal EmployeeContributionIncrease { get; init; }
        public decimal EmployerContributionIncrease { get; init; }
        public bool RegularExceedsMaximum { get; init; }
        public bool CatchUpExceedsMaximum { get; init; }
        public bool CatchUpMustBeRoth { get; init; }
        public bool RothSelectedWithoutARothAccount { get; init; }
    }

    public sealed class Retirement401kResult
    {
        public decimal AnnualPay { get; init; }
        public string PayPeriodName { get; init; } = "";
        public decimal MaximumTraditional { get; init; }
        public decimal MaximumRoth { get; init; }
        public decimal CatchUpLimit { get; init; }

        /// <summary>Employee deferral room before catch-up, after the annual-additions limit.</summary>
        public decimal RegularRoom { get; init; }

        /// <summary>Catch-up room that can actually be contributed.</summary>
        public decimal CatchUpRoom { get; init; }

        public bool CatchUpMustBeRoth { get; init; }
        public bool CatchUpBlockedWithoutRoth { get; init; }
        public bool AnnualAdditionsReducedDeferral { get; init; }
        public bool IncomeTaxSavingsCapped { get; init; }
        public bool HasEmployerMatch { get; init; }
        public decimal FullFormulaMatch { get; init; }
        public decimal UnreachableMatch { get; init; }
        public required Retirement401kElection Maximum { get; init; }
        public required Retirement401kElection FullMatch { get; init; }
        public string RuleSummary { get; init; } = "";
    }

    /// <summary>
    /// Turns pay, age, and a match formula into 401(k) payroll percentages.
    /// Traditional and Roth elective deferrals share one IRC 402(g) limit.
    /// Catch-up is extra. From 2026, IRC 414(v)(7) requires that catch-up to be Roth
    /// only when prior-year wages from the plan sponsor exceed the indexed threshold.
    /// Age makes catch-up available. Age does not, by itself, force the Roth treatment.
    /// </summary>
    public static class Retirement401kCalculator
    {
        public const int CatchUpAge = 50;
        public const int HigherCatchUpFromAge = 60;
        public const int HigherCatchUpThroughAge = 63;

        public static Retirement401kResult Calculate(Retirement401kRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var limits = request.Limits ?? Retirement401kLimits.For2026();
            var pay = NonNegative(request.AnnualPay);
            var priorWages = NonNegative(request.PriorYearWages);
            var elective = NonNegative(limits.ElectiveDeferral);
            var standardCatchUp = NonNegative(limits.CatchUp);
            var higherCatchUp = NonNegative(limits.HigherCatchUp);
            var threshold = NonNegative(limits.RothCatchUpWageThreshold);
            var additionsLimit = NonNegative(limits.AnnualAdditions);
            var compensationCap = NonNegative(limits.CompensationCap);
            var share = Fraction(request.TraditionalShare);
            var federalRate = Fraction(request.FederalMarginalRate);
            var stateRate = Fraction(request.StateMarginalRate);
            var ratesExceedADollar = federalRate + stateRate > 1m;
            var offersRoth = request.PlanOffersRoth;
            var periods = PeriodsPerYear(request.PayFrequency);

            var statutoryCatchUp = CatchUpDollars(request.AgeAtYearEnd, standardCatchUp, higherCatchUp);
            var mustRoth = statutoryCatchUp > 0 && priorWages > threshold;
            var blocked = mustRoth && !offersRoth;
            var catchUpAvailable = blocked ? 0m : statutoryCatchUp;

            var regularBeforeAdditions = Math.Min(elective, pay);
            var catchUpBeforeAdditions = Math.Min(catchUpAvailable, Math.Max(0m, pay - regularBeforeAdditions));
            var matchPay = compensationCap > 0 ? Math.Min(pay, compensationCap) : 0m;
            var additionsCap = Math.Min(additionsLimit, matchPay > 0 ? matchPay : pay);

            var regularRoom = MaximumRegularDeferral(
                regularBeforeAdditions,
                catchUpBeforeAdditions,
                additionsCap,
                total => Match(total, matchPay, request));
            var additionsReduced = regularRoom + 0.001m < regularBeforeAdditions;
            var catchUpRoom = Math.Min(catchUpBeforeAdditions, Math.Max(0m, pay - regularRoom));

            var maximumDollars = regularRoom + catchUpRoom;
            var band1 = Money(matchPay * Fraction(request.FirstMatchOfPay));
            var band2 = Money(matchPay * Fraction(request.NextMatchOfPay));
            var neededForFullMatch = band1 + band2;
            var hasMatchFormula = PositiveBand(request.FirstMatchRate, request.FirstMatchOfPay)
                || PositiveBand(request.NextMatchRate, request.NextMatchOfPay);
            var fullMatchDollars = Math.Min(neededForFullMatch, maximumDollars);
            var fullFormulaMatch = Match(neededForFullMatch, matchPay, request);
            var matchAtMaximum = Match(maximumDollars, matchPay, request);
            if (regularRoom + matchAtMaximum > additionsCap)
                matchAtMaximum = Math.Max(0m, additionsCap - regularRoom);

            var maximum = Election(
                maximumDollars,
                regularRoom,
                catchUpRoom,
                mustRoth && !blocked,
                offersRoth,
                share,
                pay,
                periods,
                federalRate,
                stateRate,
                matchAtMaximum);
            var fullMatch = Election(
                fullMatchDollars,
                regularRoom,
                catchUpRoom,
                mustRoth && !blocked,
                offersRoth,
                share,
                pay,
                periods,
                federalRate,
                stateRate,
                Match(fullMatchDollars, matchPay, request));

            var maximumTraditional = regularRoom + (mustRoth ? 0m : catchUpRoom);
            var maximumRoth = offersRoth ? regularRoom + catchUpRoom : 0m;

            var unreachable = Money(Math.Max(0m, fullFormulaMatch - maximum.EmployerMatch));
            var summary = Summarize(
                request,
                limits,
                pay,
                elective,
                statutoryCatchUp,
                mustRoth,
                blocked,
                maximumTraditional,
                maximumRoth,
                additionsReduced,
                ratesExceedADollar,
                maximum.PercentRoundedUp || fullMatch.PercentRoundedUp,
                compensationCap,
                maximum.TraditionalPercent + maximum.RothPercent);

            return new Retirement401kResult
            {
                AnnualPay = pay,
                PayPeriodName = PeriodName(request.PayFrequency),
                MaximumTraditional = maximumTraditional,
                MaximumRoth = maximumRoth,
                CatchUpLimit = statutoryCatchUp,
                RegularRoom = regularRoom,
                CatchUpRoom = catchUpRoom,
                CatchUpMustBeRoth = mustRoth,
                CatchUpBlockedWithoutRoth = blocked,
                AnnualAdditionsReducedDeferral = additionsReduced,
                IncomeTaxSavingsCapped = ratesExceedADollar,
                HasEmployerMatch = hasMatchFormula,
                FullFormulaMatch = fullFormulaMatch,
                UnreachableMatch = unreachable,
                Maximum = maximum,
                FullMatch = fullMatch,
                RuleSummary = summary
            };
        }

        /// <summary>
        /// Prices a paycheck from percentages of that period's gross pay.
        /// Percents are points: 10 is 10 percent. Take-home is the change from the current percents.
        /// </summary>
        public static Retirement401kSelectionQuote QuoteSelection(
            Retirement401kRequest request,
            decimal periodGross,
            decimal currentPreTaxPercent,
            decimal currentRothPercent,
            decimal selectedPreTaxPercent,
            decimal selectedRothPercent,
            decimal selectedCatchUpPercent)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = Calculate(request);
            var regularMax = PayrollPercent(result.RegularRoom, result.AnnualPay);
            var catchUpMax = PayrollPercent(result.CatchUpRoom, result.AnnualPay);
            var gross = NonNegative(periodGross);
            var federal = Fraction(request.FederalMarginalRate);
            var state = Fraction(request.StateMarginalRate);

            var preTax = Withheld(selectedPreTaxPercent, gross);
            var roth = Withheld(selectedRothPercent + selectedCatchUpPercent, gross);
            var (federalSaved, stateSaved) = TaxSaved(preTax, federal, state);
            var currentPreTax = Withheld(currentPreTaxPercent, gross);
            var currentRothDollars = Withheld(currentRothPercent, gross);
            var (currentFederal, currentState) = TaxSaved(currentPreTax, federal, state);
            var selectedReduction = preTax + roth - federalSaved - stateSaved;
            var currentReduction = currentPreTax + currentRothDollars - currentFederal - currentState;
            var limits = request.Limits ?? Retirement401kLimits.For2026();
            var matchPay = PeriodMatchPay(gross, result.AnnualPay, NonNegative(limits.CompensationCap));
            var currentMatch = Match(currentPreTax + currentRothDollars, matchPay, request);
            var selectedMatch = Match(preTax + roth, matchPay, request);

            return new Retirement401kSelectionQuote
            {
                RegularMaxPercent = regularMax,
                CatchUpMaxPercent = catchUpMax,
                PreTaxWithheld = preTax,
                RothWithheld = roth,
                FederalTaxSaved = federalSaved,
                StateTaxSaved = stateSaved,
                TakeHomeLowerThanCurrent = Money(selectedReduction - currentReduction),
                CurrentEmployerMatch = currentMatch,
                SelectedEmployerMatch = selectedMatch,
                EmployeeContributionIncrease = Money(preTax + roth - currentPreTax - currentRothDollars),
                EmployerContributionIncrease = Money(selectedMatch - currentMatch),
                RegularExceedsMaximum = PercentPoints(selectedPreTaxPercent) + PercentPoints(selectedRothPercent) > regularMax + 0.001m,
                CatchUpExceedsMaximum = PercentPoints(selectedCatchUpPercent) > catchUpMax + 0.001m,
                CatchUpMustBeRoth = result.CatchUpMustBeRoth,
                RothSelectedWithoutARothAccount = !request.PlanOffersRoth
                    && (PercentPoints(selectedRothPercent) > 0m || PercentPoints(selectedCatchUpPercent) > 0m)
            };
        }

        public static int PeriodsPerYear(PayFrequency frequency) => frequency switch
        {
            PayFrequency.Weekly => 52,
            PayFrequency.Biweekly => 26,
            PayFrequency.Semimonthly => 24,
            PayFrequency.Monthly => 12,
            _ => 26
        };

        public static string PeriodName(PayFrequency frequency) => frequency switch
        {
            PayFrequency.Weekly => "weekly",
            PayFrequency.Biweekly => "biweekly",
            PayFrequency.Semimonthly => "semimonthly",
            PayFrequency.Monthly => "monthly",
            _ => "biweekly"
        };

        private static Retirement401kElection Election(
            decimal deferral,
            decimal regularRoom,
            decimal catchUpRoom,
            bool catchUpIsRoth,
            bool offersRoth,
            decimal traditionalShare,
            decimal pay,
            int periods,
            decimal federalRate,
            decimal stateRate,
            decimal employerMatch)
        {
            var regular = Math.Min(Math.Max(0m, deferral), regularRoom);
            var catchUpPart = Math.Min(Math.Max(0m, deferral - regular), catchUpRoom);

            var forcedRoth = catchUpIsRoth ? catchUpPart : 0m;
            var choosable = regular + catchUpPart - forcedRoth;
            decimal traditional;
            decimal roth;
            if (!offersRoth)
            {
                traditional = choosable;
                roth = 0m;
            }
            else
            {
                traditional = Money(choosable * traditionalShare);
                if (traditional > choosable)
                    traditional = choosable;
                roth = choosable - traditional + forcedRoth;
            }

            var traditionalPercent = PayrollPercent(traditional, pay);
            var rothPercent = PayrollPercent(roth, pay);
            var overflow = traditionalPercent + rothPercent - 100m;
            if (overflow > 0m)
            {
                var fromRoth = Math.Min(rothPercent, overflow);
                rothPercent -= fromRoth;
                traditionalPercent = Math.Max(0m, traditionalPercent - (overflow - fromRoth));
            }

            var perTraditional = periods > 0 ? Money(traditional / periods) : 0m;
            var perRoth = periods > 0 ? Money(roth / periods) : 0m;
            var federalSaved = Money(perTraditional * federalRate);
            var stateSaved = Money(perTraditional * stateRate);
            if (federalSaved + stateSaved > perTraditional)
            {
                if (federalSaved > perTraditional)
                    federalSaved = perTraditional;
                stateSaved = Math.Min(stateSaved, perTraditional - federalSaved);
            }

            return new Retirement401kElection
            {
                TraditionalAnnual = traditional,
                RothAnnual = roth,
                EmployerMatch = Money(employerMatch),
                TraditionalPercent = traditionalPercent,
                RothPercent = rothPercent,
                PerPayTraditional = perTraditional,
                PerPayRoth = perRoth,
                PerPayFederalTaxSaved = federalSaved,
                PerPayStateTaxSaved = stateSaved,
                PercentRoundedUp = RoundedUp(traditional, pay, traditionalPercent)
                    || RoundedUp(roth, pay, rothPercent)
            };
        }

        private static decimal CatchUpDollars(int age, decimal standardCatchUp, decimal higherCatchUp)
        {
            if (age < CatchUpAge)
                return 0m;
            if (age >= HigherCatchUpFromAge && age <= HigherCatchUpThroughAge)
                return higherCatchUp;
            return standardCatchUp;
        }

        private static decimal MaximumRegularDeferral(
            decimal ceiling,
            decimal catchUp,
            decimal additionsCap,
            Func<decimal, decimal> matchOnTotal)
        {
            ceiling = Money(ceiling);
            if (ceiling <= 0m)
                return 0m;
            if (Fits(ceiling))
                return ceiling;

            var low = 0m;
            var high = ceiling;
            for (var i = 0; i < 48; i++)
            {
                var mid = Money((low + high) / 2m);
                if (Fits(mid))
                    low = mid;
                else
                    high = mid;
            }

            var candidate = low;
            while (candidate < ceiling && Fits(candidate + 0.01m))
                candidate = Money(candidate + 0.01m);
            while (candidate > 0m && !Fits(candidate))
                candidate = Money(candidate - 0.01m);
            return Math.Max(0m, candidate);

            bool Fits(decimal regular)
            {
                var match = matchOnTotal(regular + catchUp);
                return Money(regular + match) <= additionsCap;
            }
        }

        private static decimal PeriodMatchPay(decimal periodGross, decimal annualPay, decimal compensationCap)
        {
            if (periodGross <= 0m || compensationCap <= 0m)
                return 0m;
            if (annualPay <= 0m || annualPay <= compensationCap)
                return periodGross;
            return Money(periodGross * compensationCap / annualPay);
        }

        private static decimal Match(decimal employeeDeferral, decimal matchPay, Retirement401kRequest request)
        {
            if (employeeDeferral <= 0m || matchPay <= 0m)
                return 0m;

            var band1 = Money(matchPay * Fraction(request.FirstMatchOfPay));
            var band2 = Money(matchPay * Fraction(request.NextMatchOfPay));
            var inFirst = Math.Min(employeeDeferral, band1);
            var inNext = Math.Min(Math.Max(0m, employeeDeferral - band1), band2);
            var rate1 = NonNegative(request.FirstMatchRate);
            var rate2 = NonNegative(request.NextMatchRate);
            return Money(inFirst * rate1 + inNext * rate2);
        }

        private static string Summarize(
            Retirement401kRequest request,
            Retirement401kLimits limits,
            decimal pay,
            decimal elective,
            decimal statutoryCatchUp,
            bool mustRoth,
            bool blocked,
            decimal maximumTraditional,
            decimal maximumRoth,
            bool additionsReduced,
            bool ratesCapped,
            bool percentRoundedUp,
            decimal compensationCap,
            decimal electedPercent)
        {
            var lines = new List<string>
            {
                $"Traditional and Roth 401(k) contributions share one employee limit of {Dollars(elective)}."
            };

            if (request.AgeAtYearEnd < CatchUpAge)
            {
                lines.Add("Catch-up contributions start at age 50. Age does not decide whether a contribution is Roth.");
            }
            else if (request.AgeAtYearEnd >= HigherCatchUpFromAge && request.AgeAtYearEnd <= HigherCatchUpThroughAge)
            {
                lines.Add($"Catch-up of {Dollars(statutoryCatchUp)} is available because this age falls in the 60 through 63 range.");
            }
            else
            {
                lines.Add($"Catch-up of {Dollars(statutoryCatchUp)} is available at age 50 or older.");
            }

            if (blocked)
            {
                lines.Add(
                    $"That catch-up has to be Roth because prior-year wages from this employer exceed {Dollars(limits.RothCatchUpWageThreshold)}. " +
                    "This plan has no Roth account, so the catch-up cannot be made.");
            }
            else if (statutoryCatchUp > 0m && mustRoth)
            {
                lines.Add(
                    $"That catch-up has to be Roth because prior-year wages from this employer exceed {Dollars(limits.RothCatchUpWageThreshold)}. " +
                    "The shared employee limit can still be pre-tax.");
            }
            else if (statutoryCatchUp > 0m)
            {
                lines.Add(
                    $"That catch-up can be pre-tax or Roth. It has to be Roth only when prior-year wages from this employer exceed {Dollars(limits.RothCatchUpWageThreshold)}.");
            }

            if (!request.PlanOffersRoth && !blocked)
                lines.Add("This plan has no Roth account, so the contribution is pre-tax.");

            if (pay <= 0m)
            {
                lines.Add("Enter annual pay to turn these limits into a payroll percentage.");
            }
            else if (!request.PlanOffersRoth)
            {
                lines.Add($"The most that can go in pre-tax is {Dollars(maximumTraditional)}.");
            }
            else if (mustRoth)
            {
                lines.Add(
                    $"Putting as much as possible in pre-tax uses {Dollars(maximumTraditional)} of the shared limit, and the catch-up is still Roth. " +
                    $"Putting as much as possible in Roth uses {Dollars(maximumRoth)}. " +
                    "The shared limit can be split either way. The catch-up cannot.");
            }
            else
            {
                lines.Add(
                    $"Putting as much as possible in pre-tax uses {Dollars(maximumTraditional)}. " +
                    $"Putting as much as possible in Roth uses {Dollars(maximumRoth)}. " +
                    "Choosing more of one leaves less of the other.");
            }

            if (pay > compensationCap && compensationCap > 0m)
                lines.Add($"The match formula counts only the first {Dollars(compensationCap)} of pay.");

            if (additionsReduced)
            {
                lines.Add(
                    $"The deferral is limited so employee contributions other than catch-up, plus the employer match, stay within the {Dollars(limits.AnnualAdditions)} annual additions limit.");
            }

            if (percentRoundedUp)
            {
                lines.Add(
                    "The percentages are rounded up to the next 0.01% so the election reaches the dollar amount. The plan should stop at the legal dollar limit.");
            }

            if (pay > 0m && electedPercent >= 90m)
            {
                lines.Add(
                    "Payroll taxes and other deductions come out of the same paycheck, so the plan may refuse a deferral this high. Contribute the highest percent it allows.");
            }

            if (ratesCapped)
            {
                lines.Add("The federal and state rates add up to more than 100%, so tax saved is limited to the pre-tax amount withheld.");
            }

            lines.Add("These amounts assume this is the only 401(k) deferred into this year.");
            return string.Join(Environment.NewLine + Environment.NewLine, lines);
        }

        private static bool PositiveBand(decimal rate, decimal portionOfPay) =>
            rate > 0m && portionOfPay > 0m;

        private static decimal Withheld(decimal percent, decimal gross) =>
            Money(gross * PercentPoints(percent) / 100m);

        private static (decimal Federal, decimal State) TaxSaved(decimal preTax, decimal federalRate, decimal stateRate)
        {
            var federal = Money(preTax * federalRate);
            var state = Money(preTax * stateRate);
            if (federal + state > preTax)
            {
                if (federal > preTax)
                    federal = preTax;
                state = Math.Min(state, preTax - federal);
            }

            return (federal, state);
        }

        private static decimal PercentPoints(decimal value) => value < 0m ? 0m : value;

        private static decimal PayrollPercent(decimal dollars, decimal annualPay)
        {
            if (annualPay <= 0m || dollars <= 0m)
                return 0m;

            var percent = Math.Ceiling(dollars / annualPay * 100m * 100m) / 100m;
            return percent > 100m ? 100m : percent;
        }

        private static bool RoundedUp(decimal dollars, decimal annualPay, decimal percent)
        {
            if (annualPay <= 0m || dollars <= 0m)
                return false;
            var exact = dollars / annualPay * 100m;
            return percent > exact + 0.0000001m;
        }

        private static decimal Fraction(decimal value)
        {
            if (value < 0m)
                return 0m;
            return value > 1m ? 1m : value;
        }

        private static decimal NonNegative(decimal value) => value < 0m ? 0m : value;

        private static decimal Money(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string Dollars(decimal value)
        {
            var rounded = Money(value);
            var format = rounded == decimal.Truncate(rounded) ? "c0" : "c2";
            return rounded.ToString(format);
        }
    }
}
