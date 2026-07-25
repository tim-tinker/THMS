using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.UI.ViewModels.Transportation
{
    public class ChargingCostEntryViewModel
    {
        private readonly TransportationDataStore _store;

        public ChargingCostEntryViewModel(TransportationDataStore store, Guid vehicleId)
        {
            _store = store;
            VehicleId = vehicleId;
        }

        public Guid VehicleId { get; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public decimal Cost { get; set; }

        public void Save()
        {
            var record = new ChargingCostRecord
            {
                VehicleId = VehicleId,
                Timestamp = Timestamp,
                Cost = Cost
            };

            _store.AddChargingCostRecord(record);
        }
    }
}
