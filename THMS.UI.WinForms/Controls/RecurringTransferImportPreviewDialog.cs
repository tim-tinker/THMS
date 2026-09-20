using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class RecurringTransferImportPreviewDialog : ImportPreviewDialog<RecurringTransferImportPreview>
    {
        private readonly RecurringTransferImportOrchestrator _orchestrator;

        public RecurringTransferImportPreviewDialog(IList<RecurringTransferImportPreview> rows)
            : this(rows, new RecurringTransferImportOrchestrator())
        {
        }

        public RecurringTransferImportPreviewDialog(
            IList<RecurringTransferImportPreview> rows,
            RecurringTransferImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Transfer Rules";
        protected override string Heading => "Transfer Rules to Import (Preview)";
        protected override string Singular => "transfer rule";
        protected override string Plural => "transfer rules";
        protected override int WindowWidth => 1200;

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                TextColumn(nameof(RecurringTransferImportPreview.FromAccount), "From Account"),
                TextColumn(nameof(RecurringTransferImportPreview.ToAccount), "To Account"),
                TextColumn(nameof(RecurringTransferImportPreview.Description), "Description"),
                TextColumn(nameof(RecurringTransferImportPreview.Frequency), "Frequency"),
                DateColumn(nameof(RecurringTransferImportPreview.LastOccurrence), "Last Occurrence"),
                DateColumn(nameof(RecurringTransferImportPreview.NextOccurrence), "Date"),
                NumberColumn(nameof(RecurringTransferImportPreview.Amount), "Amount", "c2"),
                TextColumn(nameof(RecurringTransferImportPreview.Category), "Category"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<RecurringTransferImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportRules(rows, progress);
    }
}
