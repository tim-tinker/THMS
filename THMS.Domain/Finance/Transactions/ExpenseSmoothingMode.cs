namespace THMS.Domain.Finance.Transactions
{
    public enum ExpenseSmoothingMode
    {
        SimpleAverage,
        WeightedAverage,
        Exponential,
        Seasonal,
        Hybrid
    }
}
