namespace THMS.Domain.Finance.Transactions
{
    public static class DefaultExpenseCategories
    {
        public static readonly Guid UtilityId = Guid.Parse("11111111-1111-1111-1111-111111111001");
        public static readonly Guid ElectricId = Guid.Parse("11111111-1111-1111-1111-111111111002");
        public static readonly Guid WaterId = Guid.Parse("11111111-1111-1111-1111-111111111003");
        public static readonly Guid GasId = Guid.Parse("11111111-1111-1111-1111-111111111004");
        public static readonly Guid GroceriesId = Guid.Parse("11111111-1111-1111-1111-111111111005");
        public static readonly Guid RestaurantsId = Guid.Parse("11111111-1111-1111-1111-111111111006");
        public static readonly Guid PaymentId = Guid.Parse("11111111-1111-1111-1111-111111111007");
        public static readonly Guid UncategorizedId = Guid.Parse("11111111-1111-1111-1111-111111111008");

        public const string Utility = "Utility";
        public const string Electricity = "Electricity";
        public const string Water = "Water";
        public const string Gas = "Natural Gas";
        public const string Groceries = "Groceries";
        public const string Restaurants = "Restaurants";
        public const string Payment = "Payment";
        public const string Uncategorized = "Uncategorized";

        public static readonly Guid[] UtilityMemberIds = [UtilityId, ElectricId, WaterId, GasId];

        public static IReadOnlyList<ExpenseCategory> All { get; } =
        [
            new() { Id = UtilityId, Name = Utility, DisplayOrder = 10 },
            new() { Id = ElectricId, Name = Electricity, ParentCategoryId = UtilityId, DisplayOrder = 11 },
            new() { Id = WaterId, Name = Water, ParentCategoryId = UtilityId, DisplayOrder = 12 },
            new() { Id = GasId, Name = Gas, ParentCategoryId = UtilityId, DisplayOrder = 13 },
            new() { Id = GroceriesId, Name = Groceries, DisplayOrder = 20 },
            new() { Id = RestaurantsId, Name = Restaurants, DisplayOrder = 30 },
            new() { Id = PaymentId, Name = Payment, DisplayOrder = 40 },
            new() { Id = UncategorizedId, Name = Uncategorized, DisplayOrder = 90 }
        ];

        public static string CanonicalName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Uncategorized;

            return name.Trim() switch
            {
                _ when Matches(name, "Utilities") => Utility,
                _ when Matches(name, "Electricity") => Electricity,
                _ when Matches(name, "Electric") => Electricity,
                _ when Matches(name, "Water") => Water,
                _ when Matches(name, "Gas") => Gas,
                _ when Matches(name, "Groceries") => Groceries,
                _ when Matches(name, "Shopping") => Restaurants,
                _ when Matches(name, "Payment") => Payment,
                _ when Matches(name, "Uncategorized") => Uncategorized,
                _ => name.Trim()
            };
        }

        private static bool Matches(string value, string expected) =>
            string.Equals(value.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
