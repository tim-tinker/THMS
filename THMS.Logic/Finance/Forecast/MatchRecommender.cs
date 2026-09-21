using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Forecast
{
    public sealed class MatchRecommendation
    {
        public Guid ImportedId { get; init; }
        public Guid ExpectedId { get; init; }
    }

    public class MatchRecommender
    {
        public const int DayTolerance = 4;

        public List<MatchRecommendation> Recommend(
            IEnumerable<PostedTransaction> imported,
            IEnumerable<FutureSingleTransaction> expectedSingles,
            IEnumerable<FutureTransferTransaction> expectedTransfers)
        {
            var unmatchedImported = imported
                .Where(p => p.ImportedStatus == ImportedStatus.Unreconciled)
                .OrderBy(p => p.Date)
                .ToList();
            var unmatchedSingles = expectedSingles.Where(e => !e.IsRealized).ToList();
            var unmatchedTransfers = expectedTransfers.Where(e => !e.IsRealized).ToList();
            var usedExpected = new HashSet<Guid>();
            var results = new List<MatchRecommendation>();

            foreach (var posted in unmatchedImported)
            {
                var expectedId = FindExpected(posted, unmatchedSingles, unmatchedTransfers, usedExpected);
                if (expectedId is not Guid id)
                    continue;
                usedExpected.Add(id);
                results.Add(new MatchRecommendation { ImportedId = posted.Id, ExpectedId = id });
            }

            return results;
        }

        private static Guid? FindExpected(
            PostedTransaction posted,
            List<FutureSingleTransaction> singles,
            List<FutureTransferTransaction> transfers,
            HashSet<Guid> used)
        {
            foreach (var expected in singles)
            {
                if (used.Contains(expected.Id))
                    continue;
                if (expected.AccountId != posted.AccountId)
                    continue;
                if (!IsAmountMatch(posted.Amount, expected.Amount))
                    continue;
                if (!IsDateMatch(posted.Date, expected.Date) &&
                    !IsLateDescriptionMatch(posted.Description, expected.Description, expected.Date, posted.Date))
                    continue;
                return expected.Id;
            }

            foreach (var expected in transfers)
            {
                if (used.Contains(expected.Id))
                    continue;
                if (expected.FromAccountId != posted.AccountId && expected.ToAccountId != posted.AccountId)
                    continue;
                if (!IsAmountMatch(posted.Amount, expected.Amount))
                    continue;
                if (!IsDateMatch(posted.Date, expected.Date) &&
                    !IsLateDescriptionMatch(posted.Description, expected.Description, expected.Date, posted.Date))
                    continue;
                return expected.Id;
            }

            return null;
        }

        private static bool IsAmountMatch(decimal postedAmount, decimal expectedAmount) =>
            Math.Abs(Math.Abs(postedAmount) - Math.Abs(expectedAmount)) < RecurringRulePattern.AmountTolerance;

        private static bool IsDateMatch(DateTime postedDate, DateTime expectedDate) =>
            Math.Abs((postedDate.Date - expectedDate.Date).TotalDays) <= DayTolerance;

        private static bool IsLateDescriptionMatch(
            string? postedDescription,
            string? expectedDescription,
            DateTime expectedDate,
            DateTime postedDate)
        {
            if (!string.Equals(postedDescription ?? "", expectedDescription ?? "", StringComparison.OrdinalIgnoreCase))
                return false;
            return postedDate.Date >= expectedDate.Date.AddDays(-DayTolerance);
        }
    }
}
