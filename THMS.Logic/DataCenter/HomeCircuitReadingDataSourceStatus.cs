using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using THMS.Data.Stores;

namespace THMS.Logic.DataCenter
{
    public class HomeCircuitReadingDataSourceStatus : IDataSourceStatus
    {
        private readonly IEnergyDataStore _energyStore;

        public string DataSourceName => "EV Circuit Readings";

        public DateTime? LastRetrieval { get; private set; }

        public HomeCircuitReadingDataSourceStatus(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void QueryStatus()
        {
            var lastReading = _energyStore.GetLatestHomeCircuitReading();

            LastRetrieval = lastReading?.Timestamp;
        }
    }
}
