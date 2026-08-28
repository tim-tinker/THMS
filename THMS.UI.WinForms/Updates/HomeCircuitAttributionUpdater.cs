using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
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
