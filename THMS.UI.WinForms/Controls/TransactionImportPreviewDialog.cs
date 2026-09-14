using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class TransactionImportPreviewDialog : ImportPreviewDialog<TransactionImportPreview>
    {
        private readonly TransactionImportOrchestrator _orchestrator;

        public TransactionImportPreviewDialog(IList<TransactionImportPreview> rows)
            : this(rows, new TransactionImportOrchestrator())
        {
        }

        public TransactionImportPreviewDialog(
            IList<TransactionImportPreview> rows,
            TransactionImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Transactions";
        protected override string Heading => "Transactions to Import (Preview)";
        protected override string Singular => "transaction";
        protected override string Plural => "transactions";

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                DateColumn(nameof(TransactionImportPreview.Date), "Date"),
                TextColumn(nameof(TransactionImportPreview.Description), "Description"),
                NumberColumn(nameof(TransactionImportPreview.Amount), "Amount", "c2"),
                TextColumn(nameof(TransactionImportPreview.Account), "Account", readOnly: true),
                TextColumn(nameof(TransactionImportPreview.Category), "Category"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<TransactionImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportTransactions(rows, progress);
    }
}
