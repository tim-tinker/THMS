using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    // TODO: Not used. Other forms or updaters appear to have replaced this class. Remove later once that is confirmed.
    public class HomeCircuitAttributionUpdater : IDataSourceUpdater
    {
        private readonly HomeCircuitAttributionOrchestrator _orchestrator = new();

        public IDataSourceStatus Status { get; } = new HomeCircuitAttributionDataSourceStatus();

        public void UpdateDataSource()
        {
            _orchestrator.Update();
        }
    }
}
