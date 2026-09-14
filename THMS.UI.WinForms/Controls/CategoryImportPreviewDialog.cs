using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class CategoryImportPreviewDialog : ImportPreviewDialog<CategoryImportPreview>
    {
        private readonly CategoryImportOrchestrator _orchestrator;

        public CategoryImportPreviewDialog(IList<CategoryImportPreview> rows)
            : this(rows, new CategoryImportOrchestrator())
        {
        }

        public CategoryImportPreviewDialog(IList<CategoryImportPreview> rows, CategoryImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Categories";
        protected override string Heading => "Categories to Import (Preview)";
        protected override string Singular => "category";
        protected override string Plural => "categories";
        protected override int WindowWidth => 640;

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                TextColumn(nameof(CategoryImportPreview.Name), "Name"),
                TextColumn(nameof(CategoryImportPreview.Parent), "Parent"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<CategoryImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportCategories(rows, progress);
    }
}
