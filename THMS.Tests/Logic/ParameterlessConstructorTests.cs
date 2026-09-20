using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.Energy;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Energy;
using THMS.Logic.ViewModels.Finance;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class ParameterlessConstructorTests
    {
        [Test]
        public void ProductionConstructors_DoNotThrowAgainstInMemoryStores()
        {
            Assert.That(new EnergyAggregationService(), Is.Not.Null);
            Assert.That(new EnergyDashboardViewModel(), Is.Not.Null);
            Assert.That(new AccountOrchestrator(), Is.Not.Null);
            Assert.That(new TransactionOrchestrator(), Is.Not.Null);
            Assert.That(new ElectricContractOrchestrator(), Is.Not.Null);
            Assert.That(new SolarIntervalOrchestrator(), Is.Not.Null);
            Assert.That(new HomeCircuitReadingOrchestrator(), Is.Not.Null);
            Assert.That(new HomeCircuitAttributionOrchestrator(), Is.Not.Null);
            Assert.That(new EvChargeSessionOrchestrator(), Is.Not.Null);
            Assert.That(new TransactionUpdaterOrchestrator(), Is.Not.Null);
            Assert.That(new VehicleListViewModel(), Is.Not.Null);
            Assert.That(new MileageEntryViewModel(Guid.NewGuid()), Is.Not.Null);
            Assert.That(new ChargeCostEntryViewModel(Guid.NewGuid()), Is.Not.Null);
            Assert.That(new FinanceDashboardViewModel(), Is.Not.Null);
            Assert.That(new SolarDataSourceStatus(), Is.Not.Null);
            Assert.That(new ElectricContractDataSourceStatus(), Is.Not.Null);
            Assert.That(new EvChargeSessionDataSourceStatus(), Is.Not.Null);
            Assert.That(new HomeCircuitReadingDataSourceStatus(), Is.Not.Null);
            Assert.That(new HomeCircuitAttributionDataSourceStatus(), Is.Not.Null);
            Assert.That(new DataAvailabilityService(), Is.Not.Null);
            Assert.That(new VehicleDetailViewModel(Guid.NewGuid()), Is.Not.Null);
            Assert.That(new RegisterUpdateOrchestrator(), Is.Not.Null);
            Assert.That(new RecurringRuleOrchestrator(), Is.Not.Null);
            Assert.That(new BudgetOrchestrator(), Is.Not.Null);
            Assert.That(new CategoryOrchestrator(), Is.Not.Null);
            Assert.That(new AccountSyncOrchestrator(), Is.Not.Null);
            Assert.That(new TransactionImportOrchestrator(), Is.Not.Null);
            Assert.That(new EvChargeSessionImportOrchestrator(), Is.Not.Null);
            Assert.That(new AccountImportOrchestrator(), Is.Not.Null);
            Assert.That(new StatementImportOrchestrator(), Is.Not.Null);
            Assert.That(new AccountDiagnosticsOrchestrator(), Is.Not.Null);
            Assert.That(new TransactionDiagnosticsOrchestrator(), Is.Not.Null);
            Assert.That(new SplitDiagnosticsOrchestrator(), Is.Not.Null);
            Assert.That(new LoanDiagnosticsOrchestrator(), Is.Not.Null);
            Assert.That(new ForecastDiagnosticsOrchestrator(), Is.Not.Null);
            Assert.That(new PlanningOrchestrator(), Is.Not.Null);
            Assert.That(new BillsOrchestrator(), Is.Not.Null);
            Assert.That(new PlaidAccountOrchestrator(), Is.Not.Null);
            Assert.That(new PlaidTransactionOrchestrator(), Is.Not.Null);
            Assert.That(new ExternalTransactionAccess(), Is.Not.Null);
        }
    }
}
