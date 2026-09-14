using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public enum EvChargeEnergyColumn
    {
        Drawn,
        Solar,
        Battery,
        Grid,
        Cost
    }

    public enum EvChargeEnergyCellKind
    {
        Normal,
        Estimated,
        Unavailable,
        NotApplicable
    }

    public readonly record struct EvChargeEnergyCellHint(
        EvChargeEnergyCellKind Kind,
        string? ToolTip);

    public static class EvChargeEnergyCellHints
    {
        public const string NoCircuitToolTip = "No home circuit data for this session.";
        public const string NoSolarToolTip =
            "Estimated: no solar data for this session. Circuit energy is charged to the grid.";
        public const string NoContractToolTip = "Unknown: no electric contract for this session.";
        public const string CommercialToolTip = "Not applicable for commercial charging.";

        public static EvChargeEnergyCellHint For(BaseEvChargeSession session, EvChargeEnergyColumn column)
        {
            if (!session.IsHomeCharge)
            {
                return column is EvChargeEnergyColumn.Solar
                    or EvChargeEnergyColumn.Battery
                    or EvChargeEnergyColumn.Grid
                    ? new(EvChargeEnergyCellKind.NotApplicable, CommercialToolTip)
                    : Default;
            }

            if (!session.HasCircuitData)
                return new(EvChargeEnergyCellKind.Unavailable, NoCircuitToolTip);

            if (column == EvChargeEnergyColumn.Drawn)
                return Default;

            if (column == EvChargeEnergyColumn.Cost)
            {
                if (!session.HasElectricContract)
                    return new(EvChargeEnergyCellKind.Unavailable, NoContractToolTip);
                if (!session.HasSolarData)
                    return new(EvChargeEnergyCellKind.Estimated, NoSolarToolTip);
                return Default;
            }

            if (!session.HasSolarData)
                return new(EvChargeEnergyCellKind.Estimated, NoSolarToolTip);

            return Default;
        }

        private static EvChargeEnergyCellHint Default { get; } =
            new(EvChargeEnergyCellKind.Normal, null);
    }
}
