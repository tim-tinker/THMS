using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Domain.Transportation.Analytics;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class TransportationDashboardViewModel : BaseDashboardViewModel
    {
        // Lazy-initialized dependencies
        private TransportationDataStore? _store;
        private TransportationAnalyticsEngine? _engine;

        protected TransportationDataStore Store =>
            _store ??= new TransportationDataStore();

        protected TransportationAnalyticsEngine Engine =>
            _engine ??= new TransportationAnalyticsEngine(Store);

        public TransportationDashboardViewModel()
        {
            Vehicles = Store.GetAllVehicles().ToList();

            if (Vehicles.Any())
                SelectedVehicle = Vehicles.First();
        }

        // ---------------------------------------------------------
        // Vehicle selection
        // ---------------------------------------------------------

        public List<Vehicle> Vehicles { get; }
        public Vehicle? SelectedVehicle { get; set; }

        // ---------------------------------------------------------
        // Analytics results
        // ---------------------------------------------------------

        public MonthlyTransportationSummary? MonthlySummary { get; private set; }
        public decimal LifetimeCostPerMile { get; private set; }

        // ---------------------------------------------------------
        // Dashboard refresh logic
        // ---------------------------------------------------------

        public void Refresh(int year, int month)
        {
            if (SelectedVehicle == null)
            {
                MonthlySummary = null;
                LifetimeCostPerMile = 0;
                return;
            }

            MonthlySummary = Engine.ComputeMonthlySummary(SelectedVehicle.Id, year, month);
            LifetimeCostPerMile = Engine.ComputeLifetimeCostPerMile(SelectedVehicle.Id);
        }

        // ---------------------------------------------------------
        // Test injection support
        // ---------------------------------------------------------

        public void SetStoreForTesting(TransportationDataStore store)
        {
            _store = store;
        }

        public void SetEngineForTesting(TransportationAnalyticsEngine engine)
        {
            _engine = engine;
        }
    }
}
