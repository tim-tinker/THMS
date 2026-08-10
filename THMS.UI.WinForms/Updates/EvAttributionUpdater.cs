using System;
using System.Collections.Generic;
using System.Text;
using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class EvAttributionUpdater : IDataSourceUpdater
    {
        private IEnergyDataStore _energyStore;
        private EvAttributionOrchestrator _orchestrator;

        public IDataSourceStatus Status { get; }

        public EvAttributionUpdater(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            Status = new EvAttributionDataSourceStatus(energyStore);
            _orchestrator = new EvAttributionOrchestrator(energyStore);
        }

        public void UpdateDataSource()
        {
            _orchestrator.Update();
        }
    }
}
