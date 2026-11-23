using THMS.Domain.Transportation;

namespace THMS.Logic.Transportation
{
    public static class BatterySocEnergy
    {
        public const decimal LowSocPercent = 20m;
        public const decimal HighSocPercent = 90m;

        /// <summary>
        /// Quadratic and cubic terms stay zero until this many measured sessions
        /// start below <see cref="LowSocPercent"/> or end above <see cref="HighSocPercent"/>.
        /// </summary>
        public const int MinimumExtremeSessions = 5;

        public static decimal? EstimateKwh(decimal startSoc, decimal endSoc, SocEnergyCoefficients? model)
        {
            if (model is not SocEnergyCoefficients energy || endSoc <= startSoc)
                return null;

            var delta = endSoc - startSoc;
            var deltaSquared = endSoc * endSoc - startSoc * startSoc;
            var deltaCubed = endSoc * endSoc * endSoc - startSoc * startSoc * startSoc;
            var kwh = energy.A1 * delta + energy.A2 * deltaSquared + energy.A3 * deltaCubed;
            return decimal.Round(kwh, 3, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Least squares for E(end) − E(start), using measured battery kWh only.
        /// Calculated values and zero kWh are left out. A2 and A3 are zero until
        /// enough sessions reach the low or high end of the SOC display.
        /// </summary>
        public static SocEnergyFit? Fit(IEnumerable<BaseEvChargeSession> sessions)
        {
            var measured = new List<BaseEvChargeSession>();
            foreach (var session in sessions)
            {
                if (session.KwhAddedIsCalculated || session.KwhAdded <= 0)
                    continue;
                if (session.EndSoc == session.StartSoc)
                    continue;

                measured.Add(session);
            }

            if (measured.Count == 0)
                return null;

            var extreme = measured.Count(session =>
                session.StartSoc < LowSocPercent || session.EndSoc > HighSocPercent);
            if (extreme < MinimumExtremeSessions)
            {
                var linear = FitLinear(measured);
                return linear is null
                    ? null
                    : new SocEnergyFit(linear.Value, measured.Count, extreme, false);
            }

            if (!TryFitPolynomial(measured, 3, out var cubic))
            {
                var linear = FitLinear(measured);
                return linear is null
                    ? null
                    : new SocEnergyFit(linear.Value, measured.Count, extreme, false);
            }

            return new SocEnergyFit(cubic, measured.Count, extreme, true);
        }

        private static SocEnergyCoefficients? FitLinear(IReadOnlyList<BaseEvChargeSession> sessions)
        {
            decimal numerator = 0;
            decimal denominator = 0;
            foreach (var session in sessions)
            {
                var delta = session.EndSoc - session.StartSoc;
                numerator += session.KwhAdded * delta;
                denominator += delta * delta;
            }

            if (denominator == 0)
                return null;

            return new SocEnergyCoefficients(numerator / denominator, 0, 0);
        }

        /// <summary>
        /// Fits c1, c2, c3 on SOC fraction u = s/100, then converts to the percent basis
        /// A1·s + A2·s² + A3·s³. The fraction basis keeps the normal equations well scaled.
        /// </summary>
        private static bool TryFitPolynomial(
            IReadOnlyList<BaseEvChargeSession> sessions,
            int terms,
            out SocEnergyCoefficients coefficients)
        {
            var normal = new double[terms, terms];
            var rhs = new double[terms];
            foreach (var session in sessions)
            {
                var start = (double)session.StartSoc / 100d;
                var end = (double)session.EndSoc / 100d;
                var features = new[]
                {
                    end - start,
                    end * end - start * start,
                    end * end * end - start * start * start
                };
                var kwh = (double)session.KwhAdded;
                for (var row = 0; row < terms; row++)
                {
                    rhs[row] += features[row] * kwh;
                    for (var column = 0; column < terms; column++)
                        normal[row, column] += features[row] * features[column];
                }
            }

            var solution = new double[terms];
            if (!Solve(normal, rhs, terms, solution))
            {
                coefficients = default;
                return false;
            }

            if (!TryCoefficient(solution[0] / 100d, out var a1)
                || !TryCoefficient(terms > 1 ? solution[1] / 10000d : 0d, out var a2)
                || !TryCoefficient(terms > 2 ? solution[2] / 1000000d : 0d, out var a3))
            {
                coefficients = default;
                return false;
            }

            coefficients = new SocEnergyCoefficients(a1, a2, a3);
            return true;
        }

        private static bool TryCoefficient(double value, out decimal coefficient)
        {
            coefficient = 0;
            if (double.IsNaN(value) || double.IsInfinity(value))
                return false;
            if (Math.Abs(value) < 1e-12)
                return true;

            try
            {
                coefficient = (decimal)value;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static bool Solve(double[,] matrix, double[] rhs, int n, double[] solution)
        {
            var a = new double[n, n];
            var b = new double[n];
            for (var row = 0; row < n; row++)
            {
                b[row] = rhs[row];
                for (var column = 0; column < n; column++)
                    a[row, column] = matrix[row, column];
            }

            for (var column = 0; column < n; column++)
            {
                var pivot = column;
                var pivotMagnitude = Math.Abs(a[column, column]);
                for (var row = column + 1; row < n; row++)
                {
                    var magnitude = Math.Abs(a[row, column]);
                    if (magnitude > pivotMagnitude)
                    {
                        pivotMagnitude = magnitude;
                        pivot = row;
                    }
                }

                if (pivotMagnitude < 1e-12)
                    return false;

                if (pivot != column)
                {
                    (b[column], b[pivot]) = (b[pivot], b[column]);
                    for (var index = column; index < n; index++)
                        (a[column, index], a[pivot, index]) = (a[pivot, index], a[column, index]);
                }

                var diagonal = a[column, column];
                for (var row = column + 1; row < n; row++)
                {
                    var factor = a[row, column] / diagonal;
                    b[row] -= factor * b[column];
                    for (var index = column; index < n; index++)
                        a[row, index] -= factor * a[column, index];
                }
            }

            for (var row = n - 1; row >= 0; row--)
            {
                var sum = b[row];
                for (var column = row + 1; column < n; column++)
                    sum -= a[row, column] * solution[column];
                if (Math.Abs(a[row, row]) < 1e-12)
                    return false;
                solution[row] = sum / a[row, row];
            }

            return true;
        }
    }

    public readonly record struct SocEnergyFit(
        SocEnergyCoefficients Coefficients,
        int MeasuredSessions,
        int ExtremeSessions,
        bool HigherTermsUsed,
        int RefreshedCalculatedSessions = 0,
        int UnchangedCalculatedSessions = 0);
}
