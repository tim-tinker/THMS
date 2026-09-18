using THMS.Logic.Orchestrators.Finance;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class PlanningCenterForm : BaseEmbeddedForm
    {
        private readonly PlanningOrchestrator _orchestrator;

        public PlanningCenterForm()
            : this(new PlanningOrchestrator())
        {
        }

        public PlanningCenterForm(PlanningOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            InitializeComponent();
            upcomingObligations.Bind(_orchestrator);
            plannedPayments.Bind(_orchestrator);
            cashFlowForecast.Bind(_orchestrator, () => plannedPayments.SelectedPayment());
            upcomingObligations.DataChanged += (_, _) => RefreshAll();
            plannedPayments.DataChanged += (_, _) => RefreshAll();
            cashFlowForecast.DataChanged += (_, _) => RefreshAll();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !Disposing && IsHandleCreated)
                RefreshAll();
        }

        private void RefreshAll()
        {
            upcomingObligations.RefreshObligations();
            plannedPayments.RefreshPayments();
            cashFlowForecast.RefreshForecast();
        }
    }
}
