namespace THMS.Domain.Finance.Transactions
{
    public readonly record struct PostedCategoryAssignment(Guid TransactionId, Guid? SplitRowId = null);

    public readonly record struct PostedCategoryWrite(bool Updated, string? Description);
}