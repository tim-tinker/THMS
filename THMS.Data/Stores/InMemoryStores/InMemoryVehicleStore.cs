using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryVehicleStore
    {
        private readonly List<VehicleBase> _items = new();

        public void Upsert(VehicleBase vehicle)
        {
            var index = _items.FindIndex(v => v.Id == vehicle.Id);
            if (index < 0)
            {
                _items.Add(vehicle);
                return;
            }

            _items[index] = vehicle;
        }

        public VehicleBase? Get(Guid id)
        {
            return _items.FirstOrDefault(v => v.Id == id);
        }

        public IEnumerable<VehicleBase> GetAll()
        {
            return _items;
        }
    }
}
