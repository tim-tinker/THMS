using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class MileageEntryViewModel
    {
        private readonly IVehicleDataStore _store;

        public MileageEntryViewModel(Guid vehicleId)
        {
            _store = new DataStoreFactory().GetVehicleStore();
            VehicleId = vehicleId;
        }

        public Guid VehicleId { get; }

        public DateTime Date { get; set; } = DateTime.Now;
        public decimal OdometerMiles { get; set; }
        public decimal GallonsAdded { get; set; }
        public decimal FuelCost { get; set; }
        public bool IsFullFillUp { get; set; }

        public void Save()
        {
            var record = new IceMileageRecord
            {
                VehicleId = VehicleId,
                EndTime = Date,
                OdometerMiles = OdometerMiles,
                GallonsAdded = GallonsAdded,
                FuelCost = FuelCost,
                IsFullFillUp = IsFullFillUp
            };

            _store.UpsertIceMileageRecord(record);
        }
    }
}
