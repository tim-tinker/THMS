namespace THMS.Logic.ViewModels
{
    public readonly record struct ImportProgress(int Completed, int Total, string? Phase = null)
    {
        public const string LedgerPhase = "Updating ledger...";
        public const string AttributionPhase = "Updating circuit attribution...";

        public bool UpdatingLedger =>
            string.Equals(Phase, LedgerPhase, StringComparison.Ordinal);
    }
}
