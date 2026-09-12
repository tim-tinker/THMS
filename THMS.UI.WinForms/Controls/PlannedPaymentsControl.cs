using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class PlannedPaymentsControl : UserControl
    {
        private PlanningOrchestrator _orchestrator = new();
        private List<PlannedPaymentView> _rows = [];

        public event EventHandler? DataChanged;

        public PlannedPaymentsControl()
        {
            InitializeComponent();
            ConfigureGrid();
            dtUntil.Value = DateTime.Today.AddDays(45);
        }

        public void Bind(PlanningOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            dtUntil.Value = _orchestrator.SuggestPlanUntil();
            RefreshPayments();
        }

        public void RefreshPayments()
        {
            _rows = _orchestrator.GetPlannedPaymentViews();
            gridPayments.DataSource = _rows;
            SetStatus($"{_rows.Count} planned payment{(_rows.Count == 1 ? "" : "s")}.");
        }

        public PlannedPaymentView? SelectedPayment()
        {
            if (gridPayments.CurrentRow?.DataBoundItem is PlannedPaymentView row)
                return row;
            return _rows.FirstOrDefault();
        }

        private void ConfigureGrid()
        {
            gridPayments.AutoGenerateColumns = false;
            gridPayments.Columns.Clear();
            gridPayments.Columns.AddRange(
                TextColumn(nameof(PlannedPaymentView.AccountName), "Account"),
                AmountColumn(nameof(PlannedPaymentView.PlannedAmount), "Planned Amount"),
                DateColumn(nameof(PlannedPaymentView.PlannedDate), "Planned Date"),
                TextColumn(nameof(PlannedPaymentView.Notes), "Notes"));
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

        private void OnAdd(object sender, EventArgs e)
        {
            using var dialog = new PlannedPaymentEditForm(_orchestrator.GetAccounts());
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _orchestrator.AddPlannedPayment(dialog.AccountId, dialog.Amount, dialog.Date, dialog.Note);
                AfterChange($"Added planned payment.");
            }
            catch (Exception ex)
            {
                ShowError($"Could not add the planned payment.\n{ex.Message}");
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            var selected = SelectedPayment();
            if (selected is null)
            {
                ShowError("Select a planned payment to delete.");
                return;
            }

            try
            {
                _orchestrator.DeletePlannedPayment(selected.Id);
                AfterChange("Deleted planned payment.");
            }
            catch (Exception ex)
            {
                ShowError($"Could not delete the planned payment.\n{ex.Message}");
            }
        }

        private void OnGenerateMinimums(object sender, EventArgs e) =>
            RunGenerate("Minimum payments", () => _orchestrator.GenerateMinimumPayments(dtUntil.Value.Date));

        private void OnGeneratePromotions(object sender, EventArgs e) =>
            RunGenerate("Promotion payments", () => _orchestrator.GeneratePromotionPayments(dtUntil.Value.Date));

        private void OnGeneratePayAll(object sender, EventArgs e) =>
            RunGenerate("Pay-all-due payments", () => _orchestrator.GeneratePayAllDue(_orchestrator.SuggestPlanUntil()));

        private void OnGenerateExtra(object sender, EventArgs e)
        {
            using var dialog = new ExtraPrincipalDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            RunGenerate("Extra principal", () => _orchestrator.GenerateExtraPrincipalPayments(dialog.Amount));
        }

        private void RunGenerate(string label, Func<List<FutureSingleTransaction>> generate)
        {
            try
            {
                var created = generate();
                AfterChange($"{label}: created {created.Count}.");
            }
            catch (Exception ex)
            {
                ShowError($"{label} failed.\n{ex.Message}");
            }
        }

        private void AfterChange(string status)
        {
            RefreshPayments();
            SetStatus(status);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowError(string message) =>
            MessageBox.Show(FindForm(), message, "Planning Center", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void SetStatus(string message) => lblStatus.Text = message;
    }
}
