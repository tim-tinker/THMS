using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteEvChargeSessionStore
    {
        private readonly SqliteMileageRecordStore _mileageStore;
        private readonly EvChargeSessionTable _evSessionTable = new();

        public SqliteEvChargeSessionStore(SqliteMileageRecordStore mileageStore)
        {
            _mileageStore = mileageStore;
        }

        public void InitializeSchema(SqliteConnection conn) =>
            _evSessionTable.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, EvChargeSession session)
        {
            _mileageStore.Upsert(conn, session, "Ev");
            _evSessionTable.Upsert(conn, session);
        }

        public EvChargeSession? Get(SqliteConnection conn, Guid sessionId)
        {
            var session = _evSessionTable.GetById(conn, sessionId);
            if (session == null)
                return null;

            var mileageRecord = _mileageStore.GetById(conn, sessionId);
            if (mileageRecord != null)
            {
                session.VehicleId = mileageRecord.Value.VehicleId;
                session.OdometerMiles = mileageRecord.Value.OdometerMiles;
                session.EndTime = mileageRecord.Value.EndTime;
            }

            return session;
        }

        public IEnumerable<EvChargeSession> GetRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            var mileageRecords = _mileageStore.GetRange(conn, vehicleId, start, end);

            foreach (var record in mileageRecords)
            {
                var ev = _evSessionTable.GetById(conn, record.Id);
                if (ev == null)
                    continue;

                ev.VehicleId = record.VehicleId;
                ev.OdometerMiles = record.OdometerMiles;
                ev.EndTime = record.EndTime;
                yield return ev;
            }
        }

        public EvChargeSession? GetLatest(SqliteConnection conn)
        {
            var mileage = _mileageStore.GetLatestByType(conn, "Ev");
            return LoadFromMileage(conn, mileage);
        }

        public EvChargeSession? GetLatest(SqliteConnection conn, Guid vehicleId)
        {
            var mileage = _mileageStore.GetLatestByTypeAndVehicle(conn, "Ev", vehicleId);
            return LoadFromMileage(conn, mileage);
        }

        private EvChargeSession? LoadFromMileage(
            SqliteConnection conn,
            (Guid Id, Guid VehicleId, DateTime EndTime, decimal OdometerMiles)? mileage)
        {
            if (mileage is null)
                return null;

            var session = _evSessionTable.GetById(conn, mileage.Value.Id);
            if (session is null)
                return null;

            session.VehicleId = mileage.Value.VehicleId;
            session.OdometerMiles = mileage.Value.OdometerMiles;
            session.EndTime = mileage.Value.EndTime;
            return session;
        }

        public void Delete(SqliteConnection conn, Guid sessionId)
        {
            _evSessionTable.Delete(conn, sessionId);
            _mileageStore.Delete(conn, sessionId);
        }
    }
}
