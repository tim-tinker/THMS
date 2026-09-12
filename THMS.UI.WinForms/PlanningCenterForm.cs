using THMS.Domain.Finance.Planning;
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
            upcomingObligations.AddStatementClicked += (_, _) => AddStatement();
            upcomingObligations.EditStatementClicked += (_, _) => EditStatement();
            upcomingObligations.DeleteStatementClicked += (_, _) => DeleteStatement();
        }

        private void AddStatement()
        {
            try
            {
                using var dlg = new StatementEditorDialog(_orchestrator, null);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                RefreshUpcomingObligations();
                RefreshAll();
            }
            catch (Exception ex)
            {
                ShowPlanningError(ex.Message);
            }
        }

        private void EditStatement()
        {
            try
            {
                var selected = GetSelectedStatement() ?? ChooseStatement();
                if (selected is null)
                {
                    MessageBox.Show(this,
                        "Select a statement to edit, or add a statement first.",
                        "Planning Center",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using var dlg = new StatementEditorDialog(_orchestrator, selected);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                RefreshUpcomingObligations();
                RefreshAll();
            }
            catch (Exception ex)
            {
                ShowPlanningError(ex.Message);
            }
        }

        private AccountStatement? ChooseStatement()
        {
            var statements = _orchestrator.GetAllStatements();
            if (statements.Count == 0)
                return null;
            if (statements.Count == 1)
                return statements[0];

            var names = _orchestrator.GetAccounts().ToDictionary(a => a.Id, a => a.Name);
            var items = statements
                .Select(s => new StatementPick(
                    s,
                    $"{names.GetValueOrDefault(s.AccountId, "Account")} — {s.Type} — {s.StatementDate:d}"))
                .ToList();

            using var picker = new Form
            {
                Text = "Select Statement",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                Width = 480,
                Height = 360
            };
            var list = new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = nameof(StatementPick.Label),
                DataSource = items
            };
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            picker.AcceptButton = ok;
            picker.CancelButton = cancel;
            picker.Controls.Add(buttons);
            picker.Controls.Add(list);
            if (picker.ShowDialog(this) != DialogResult.OK)
                return null;
            return list.SelectedItem is StatementPick pick ? pick.Statement : statements[0];
        }

        private sealed record StatementPick(AccountStatement Statement, string Label);

        private void DeleteStatement()
        {
            try
            {
                var selected = GetSelectedStatement() ?? ChooseStatement();
                if (selected is null)
                {
                    MessageBox.Show(this,
                        "Select a statement to delete, or add a statement first.",
                        "Planning Center",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show(this,
                        "Delete this statement?",
                        "Planning Center",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                _orchestrator.DeleteStatement(selected.Id);
                RefreshUpcomingObligations();
                RefreshAll();
            }
            catch (Exception ex)
            {
                ShowPlanningError(ex.Message);
            }
        }

        private void ShowPlanningError(string message) =>
            MessageBox.Show(this, message, "Planning Center", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private AccountStatement? GetSelectedStatement() =>
            upcomingObligations.GetSelectedStatement();

        private void RefreshUpcomingObligations()
        {
            upcomingObligations.RefreshObligations();
        }

        private void RefreshAll()
        {
            upcomingObligations.RefreshObligations();
            plannedPayments.RefreshPayments();
            cashFlowForecast.RefreshForecast();
        }
    }
}
