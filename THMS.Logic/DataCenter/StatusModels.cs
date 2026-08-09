public class SolarDataStatus
{
    public bool HasData { get; set; }
    public DateTime? LastInterval { get; set; }
    public bool IsPartialMonth { get; set; }
    public bool IsMissingMonth { get; set; }
    public DateTime ExpectedAvailableDate { get; set; }
}

public class BillDataStatus
{
    public bool HasData { get; set; }
    public DateTime? LastBillDate { get; set; }
    public bool IsMissingMonth { get; set; }
    public DateTime ExpectedAvailableDate { get; set; }
}

public class EvCircuitStatus
{
    public bool HasData { get; set; }
    public DateTime? LastReading { get; set; }
    public bool HasGaps { get; set; }
}

public class EvCommercialStatus
{
    public bool HasData { get; set; }
    public DateTime? LastSession { get; set; }
    public bool HasMissingSessions { get; set; }
}

public class AttributionStatus
{
    public bool HasData { get; set; }
    public DateTime? LastAttribution { get; set; }
    public bool NeedsRecalculation { get; set; }
}
