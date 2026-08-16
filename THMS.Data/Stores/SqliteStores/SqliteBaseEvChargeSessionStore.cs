using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteBaseEvChargeSessionStore
    {
        private readonly SqliteMileageRecordStore _mileageStore;
        private readonly BaseEvChargeSessionTable _table = new();

        public SqliteBaseEvChargeSessionStore(SqliteMileageRecordStore mileageStore)
        {
            _mileageStore = mileageStore;
        }

        public void InitializeSchema(SqliteConnection conn) =>
            _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, BaseEvChargeSession session)
        {
            _mileageStore.Upsert(conn, session, "Ev");
            _table.Upsert(conn, session);
        }

        public BaseEvChargeSession? Get(SqliteConnection conn, Guid sessionId)
        {
            var session = _table.GetById(conn, sessionId);
            if (session == null)
                return null;

            ApplyMileage(conn, session);
            return session;
        }

        public IEnumerable<BaseEvChargeSession> GetRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end) =>
            _table.GetByVehicleAndStartRange(conn, vehicleId, start, end);

        public BaseEvChargeSession? GetLatest(SqliteConnection conn)
        {
            var mileage = _mileageStore.GetLatestByType(conn, "Ev");
            return LoadFromMileage(conn, mileage);
        }

        public BaseEvChargeSession? GetLatest(SqliteConnection conn, Guid vehicleId)
        {
            var mileage = _mileageStore.GetLatestByTypeAndVehicle(conn, "Ev", vehicleId);
            return LoadFromMileage(conn, mileage);
        }

        private BaseEvChargeSession? LoadFromMileage(
            SqliteConnection conn,
            (Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles, string VehicleName)? mileage)
        {
            if (mileage is null)
                return null;

            var session = _table.GetById(conn, mileage.Value.Id);
            if (session is null)
                return null;

            session.VehicleId = mileage.Value.VehicleId;
            session.VehicleName = mileage.Value.VehicleName;
            session.OdometerMiles = mileage.Value.OdometerMiles;
            session.EndTime = mileage.Value.EndTime;
            return session;
        }

        public void Delete(SqliteConnection conn, Guid sessionId)
        {
            _table.Delete(conn, sessionId);
            _mileageStore.Delete(conn, sessionId);
        }

        private void ApplyMileage(SqliteConnection conn, BaseEvChargeSession session)
        {
            var mileageRecord = _mileageStore.GetById(conn, session.Id);
            if (mileageRecord == null)
                return;

            session.VehicleId = mileageRecord.Value.VehicleId;
            session.VehicleName = mileageRecord.Value.VehicleName;
            session.OdometerMiles = mileageRecord.Value.OdometerMiles;
            session.EndTime = mileageRecord.Value.EndTime;
        }
    }
}
