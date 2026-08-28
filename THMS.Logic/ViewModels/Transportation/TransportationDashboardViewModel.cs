using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Domain.Transportation.Analytics;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class TransportationDashboardViewModel : BaseDashboardViewModel
    {
        private readonly IVehicleDataStore _store;
        private readonly TransportationAnalyticsEngine _engine;

        public TransportationDashboardViewModel()
        {
            _store = new DataStoreFactory().GetVehicleStore();
            _engine = new TransportationAnalyticsEngine(_store);

            Vehicles = _store.GetAllVehicles().ToList();

            if (Vehicles.Any())
                SelectedVehicle = Vehicles.First();
        }

        public List<VehicleBase> Vehicles { get; }
        public VehicleBase? SelectedVehicle { get; set; }

        public MonthlyTransportationSummary? MonthlySummary { get; private set; }
        public decimal LifetimeCostPerMile { get; private set; }

        public void Refresh(int year, int month)
        {
            if (SelectedVehicle == null)
            {
                MonthlySummary = null;
                LifetimeCostPerMile = 0;
                return;
            }

            MonthlySummary = _engine.ComputeMonthlySummary(SelectedVehicle.Id, year, month);
            LifetimeCostPerMile = _engine.ComputeLifetimeCostPerMile(SelectedVehicle.Id);
        }
    }
}
