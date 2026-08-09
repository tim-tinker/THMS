using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteIceMileageStore
    {
        private readonly SqliteMileageRecordStore _mileageStore;
        private readonly IceMileageTable _iceMileageTable = new();

        public SqliteIceMileageStore(SqliteMileageRecordStore mileageStore)
        {
            _mileageStore = mileageStore;
        }

        public void InitializeSchema(SqliteConnection conn) =>
            _iceMileageTable.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, IceMileageRecord record)
        {
            _mileageStore.Upsert(conn, record, "Ice");
            _iceMileageTable.Upsert(conn, record);
        }

        public IceMileageRecord? GetEarliest(SqliteConnection conn, Guid vehicleId)
        {
            return GetRange(conn, vehicleId, DateTime.MinValue, DateTime.MaxValue)
                .FirstOrDefault();
        }

        public IEnumerable<IceMileageRecord> GetRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            var baseRows = _mileageStore
                .GetRange(conn, vehicleId, start, end)
                .Where(r => r.Type == "Ice")
                .ToList();

            var results = new List<IceMileageRecord>();

            foreach (var (id, vId, date, odo, _) in baseRows)
            {
                var derived = _iceMileageTable.GetById(conn, id);
                if (derived == null) continue;

                var (gallonsAdded, isFullFillUp, fuelCost) = derived.Value;

                results.Add(new IceMileageRecord
                {
                    Id = id,
                    VehicleId = vId,
                    EndTime = date,
                    OdometerMiles = odo,
                    GallonsAdded = gallonsAdded,
                    IsFullFillUp = isFullFillUp,
                    FuelCost = fuelCost
                });
            }

            return results.OrderBy(r => r.EndTime);
        }
    }
}
