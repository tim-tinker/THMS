namespace THMS.Domain.Assets
{
    public static class AssetKinds
    {
        public const string Property = "Property";
        public const string Vehicle = "Vehicle";
        public const string Personal = "Personal";

        public static string Of(Asset asset) => asset switch
        {
            PropertyAsset => Property,
            VehicleAsset => Vehicle,
            PersonalAsset => Personal,
            _ => asset.GetType().Name
        };
    }
}
