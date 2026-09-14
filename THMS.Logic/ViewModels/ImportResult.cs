namespace THMS.Logic.ViewModels
{
    public sealed class ImportResult
    {
        public static ImportResult Empty { get; } = new();

        public int Count { get; init; }
        public DateTime? Start { get; init; }
        public DateTime? End { get; init; }

        public static ImportResult CountOnly(int count) => new() { Count = count };

        public static ImportResult FromDates(int count, IEnumerable<DateTime> dates)
        {
            var values = dates.ToList();
            if (count <= 0 || values.Count == 0)
                return CountOnly(count);

            return new ImportResult
            {
                Count = count,
                Start = values.Min(),
                End = values.Max()
            };
        }
    }
}
