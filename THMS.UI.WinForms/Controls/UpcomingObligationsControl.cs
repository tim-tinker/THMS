using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class UpcomingObligationsControl : UserControl
    {
        private PlanningOrchestrator _orchestrator = new();

        public event EventHandler? DataChanged;
        public event EventHandler? AddStatementClicked;
        public event EventHandler? EditStatementClicked;
        public event EventHandler? DeleteStatementClicked;

        public UpcomingObligationsControl()
        {
            InitializeComponent();
            ConfigureGrid();
        }

        public void Bind(PlanningOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            RefreshObligations();
        }

        public void RefreshObligations()
        {
            gridObligations.DataSource = _orchestrator.GetUpcomingObligations(DateTime.Today);
            SetStatus($"Loaded {gridObligations.Rows.Count} obligation{(gridObligations.Rows.Count == 1 ? "" : "s")}.");
        }

        public AccountStatement? GetSelectedStatement()
        {
            if (gridObligations.CurrentRow?.DataBoundItem is not UpcomingObligation row)
                return null;
            if (row.StatementId is not Guid statementId)
                return null;
            return _orchestrator.GetStatement(statementId);
        }

        private void ConfigureGrid()
        {
            gridObligations.AutoGenerateColumns = false;
            gridObligations.Columns.Clear();
            gridObligations.Columns.AddRange(
                TextColumn(nameof(UpcomingObligation.AccountName), "Account"),
                DateColumn(nameof(UpcomingObligation.DueDate), "Due Date"),
                AmountColumn(nameof(UpcomingObligation.MinimumPayment), "Minimum"),
                AmountColumn(nameof(UpcomingObligation.AmountDue), "Amount Due"),
                AmountColumn(nameof(UpcomingObligation.PromotionalDue), "Promotional Due"),
                TextColumn(nameof(UpcomingObligation.Notes), "Notes"));
            gridObligations.CellDoubleClick += (_, _) => EditStatementClicked?.Invoke(this, EventArgs.Empty);
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "d";
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            return column;
        }

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            return column;
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            RefreshObligations();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddStatement(object sender, EventArgs e) =>
            AddStatementClicked?.Invoke(this, EventArgs.Empty);

        private void OnEditStatement(object sender, EventArgs e) =>
            EditStatementClicked?.Invoke(this, EventArgs.Empty);

        private void OnDeleteStatement(object sender, EventArgs e) =>
            DeleteStatementClicked?.Invoke(this, EventArgs.Empty);

        private void SetStatus(string message) => lblStatus.Text = message;
    }
}
