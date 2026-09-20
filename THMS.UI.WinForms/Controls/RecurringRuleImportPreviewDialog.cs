using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class RecurringRuleImportPreviewDialog : ImportPreviewDialog<RecurringRuleImportPreview>
    {
        private readonly RecurringRuleImportOrchestrator _orchestrator;

        public RecurringRuleImportPreviewDialog(IList<RecurringRuleImportPreview> rows)
            : this(rows, new RecurringRuleImportOrchestrator())
        {
        }

        public RecurringRuleImportPreviewDialog(
            IList<RecurringRuleImportPreview> rows,
            RecurringRuleImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Transaction Rules";
        protected override string Heading => "Transaction Rules to Import (Preview)";
        protected override string Singular => "transaction rule";
        protected override string Plural => "transaction rules";
        protected override int WindowWidth => 1100;

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                TextColumn(nameof(RecurringRuleImportPreview.Account), "Account"),
                TextColumn(nameof(RecurringRuleImportPreview.Description), "Description"),
                TextColumn(nameof(RecurringRuleImportPreview.Frequency), "Frequency"),
                DateColumn(nameof(RecurringRuleImportPreview.LastOccurrence), "Last Occurrence"),
                DateColumn(nameof(RecurringRuleImportPreview.NextOccurrence), "Date"),
                NumberColumn(nameof(RecurringRuleImportPreview.Amount), "Amount", "c2"),
                TextColumn(nameof(RecurringRuleImportPreview.Category), "Category"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<RecurringRuleImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportRules(rows, progress);
    }
}
