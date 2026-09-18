using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class StatementImportPreviewDialog : ImportPreviewDialog<StatementImportPreview>
    {
        private readonly StatementImportOrchestrator _orchestrator;

        public StatementImportPreviewDialog(IList<StatementImportPreview> rows)
            : this(rows, new StatementImportOrchestrator())
        {
        }

        public StatementImportPreviewDialog(
            IList<StatementImportPreview> rows,
            StatementImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Statements";
        protected override string Heading => "Statements to Import (Preview)";
        protected override string Singular => "statement";
        protected override string Plural => "statements";
        protected override int WindowWidth => 1100;

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                TextColumn(nameof(StatementImportPreview.Account), "Account", readOnly: true),
                TextColumn(nameof(StatementImportPreview.Type), "Type", readOnly: true),
                DateColumn(nameof(StatementImportPreview.StatementDate), "Statement Date"),
                DateColumn(nameof(StatementImportPreview.DueDate), "Due Date"),
                NumberColumn(nameof(StatementImportPreview.AmountDue), "Amount Due", "c2"),
                NumberColumn(nameof(StatementImportPreview.StatementBalance), "Statement Balance", "c2"),
                NumberColumn(nameof(StatementImportPreview.EscrowBalance), "Escrow", "c2"),
                TextColumn(nameof(StatementImportPreview.PayFrom), "Pay From"),
                TextColumn(nameof(StatementImportPreview.Notes), "Notes"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<StatementImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportStatements(rows, progress);
    }
}
