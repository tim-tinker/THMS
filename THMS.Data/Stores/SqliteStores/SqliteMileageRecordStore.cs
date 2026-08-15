using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteMileageRecordStore
    {
        private readonly MileageRecordTable _table = new();

        public void InitializeSchema(SqliteConnection conn) => _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, MileageRecordBase record, string type) =>
            _table.Upsert(conn, record, type);

        public void Update(SqliteConnection conn, MileageRecordBase record) =>
            _table.Update(conn, record);

        public void Delete(SqliteConnection conn, Guid id) =>
            _table.Delete(conn, id);

        public (Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string Type, string VehicleName)? GetById(
            SqliteConnection conn,
            Guid id) =>
            _table.GetById(conn, id);

        public IEnumerable<(Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string Type, string VehicleName)>
            GetRange(SqliteConnection conn, Guid vehicleId, DateTime start, DateTime end) =>
            _table.GetRange(conn, vehicleId, start, end);

        public (Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string VehicleName)? GetLatestByType(
            SqliteConnection conn,
            string type) =>
            _table.GetLatestByType(conn, type);

        public (Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string VehicleName)? GetLatestByTypeAndVehicle(
            SqliteConnection conn,
            string type,
            Guid vehicleId) =>
            _table.GetLatestByTypeAndVehicle(conn, type, vehicleId);

        public decimal GetMilesDrivenInPeriod(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            var records = _table
                .GetRange(conn, vehicleId, start, end)
                .OrderBy(r => r.EndTime)
                .ToList();

            if (records.Count < 2)
                return 0m;

            return records.Last().OdometerMiles - records.First().OdometerMiles;
        }
    }
}
