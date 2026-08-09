using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteVehicleStore
    {
        private readonly VehicleTable _vehicleTable = new();
        private readonly VehicleIceTable _vehicleIceTable = new();
        private readonly VehicleEvTable _vehicleEvTable = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            _vehicleTable.InitializeSchema(conn);
            _vehicleIceTable.InitializeSchema(conn);
            _vehicleEvTable.InitializeSchema(conn);
        }

        public void Upsert(SqliteConnection conn, VehicleBase vehicle)
        {
            _vehicleTable.Upsert(conn, vehicle);

            if (vehicle is VehicleIce ice)
                _vehicleIceTable.Upsert(conn, ice);
            else if (vehicle is VehicleEv ev)
                _vehicleEvTable.Upsert(conn, ev);
        }

        public VehicleBase? Get(SqliteConnection conn, Guid id)
        {
            var baseInfo = _vehicleTable.GetBase(conn, id);
            if (baseInfo == null)
                return null;

            var (name, make, model, year, vin, type) = baseInfo.Value;

            if (type == nameof(VehicleIce))
            {
                var iceInfo = _vehicleIceTable.Get(conn, id);
                if (iceInfo == null) return null;

                var (fuelTankCapacityGallons, fuelType) = iceInfo.Value;

                return new VehicleIce
                {
                    Id = id,
                    Name = name,
                    Make = make,
                    Model = model,
                    Year = year,
                    Vin = vin,
                    FuelTankCapacityGallons = fuelTankCapacityGallons,
                    FuelType = fuelType
                };
            }

            if (type == nameof(VehicleEv))
            {
                var evInfo = _vehicleEvTable.Get(conn, id);
                if (evInfo == null) return null;

                var (batteryCapacityKwh, chargingPortType) = evInfo.Value;

                return new VehicleEv
                {
                    Id = id,
                    Name = name,
                    Make = make,
                    Model = model,
                    Year = year,
                    Vin = vin,
                    BatteryCapacityKwh = batteryCapacityKwh,
                    ChargePortType = chargingPortType
                };
            }

            return null;
        }

        public IEnumerable<VehicleBase> GetAll(SqliteConnection conn)
        {
            var ids = _vehicleTable.GetAllIds(conn).ToList();

            foreach (var id in ids)
            {
                var vehicle = Get(conn, id);
                if (vehicle != null)
                    yield return vehicle;
            }
        }
    }
}
