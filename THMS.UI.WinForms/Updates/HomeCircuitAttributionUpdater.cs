using System;
using System.Collections.Generic;
using System.Text;
using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class HomeCircuitAttributionUpdater : IDataSourceUpdater
    {
        private IEnergyDataStore _energyStore;
        private HomeCircuitAttributionOrchestrator _orchestrator;

        public IDataSourceStatus Status { get; }

        public HomeCircuitAttributionUpdater(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            Status = new HomeCircuitAttributionDataSourceStatus(energyStore);
            _orchestrator = new HomeCircuitAttributionOrchestrator(energyStore);
        }

        public void UpdateDataSource()
        {
            _orchestrator.Update();
        }
    }
}
