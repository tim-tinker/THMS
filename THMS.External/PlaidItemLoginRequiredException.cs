namespace THMS.External
{
    public sealed class PlaidItemLoginRequiredException : InvalidOperationException
    {
        public PlaidItemLoginRequiredException(string? message)
            : base(string.IsNullOrWhiteSpace(message)
                ? "This Plaid institution needs to be signed in again."
                : message)
        {
        }
    }
}
