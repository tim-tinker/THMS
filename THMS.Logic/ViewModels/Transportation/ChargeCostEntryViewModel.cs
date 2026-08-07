using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class ChargeCostEntryViewModel
    {
        private readonly IVehicleDataStore _store;

        public ChargeCostEntryViewModel(IVehicleDataStore store, Guid vehicleId)
        {
            _store = store;
            VehicleId = vehicleId;
        }

        public Guid VehicleId { get; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public decimal Cost { get; set; }

        public void Save()
        {
            var record = new ChargeCostRecord
            {
                VehicleId = VehicleId,
                Timestamp = Timestamp,
                Cost = Cost
            };

            //_store.AddChargeCostRecord(record);
        }
    }
}
