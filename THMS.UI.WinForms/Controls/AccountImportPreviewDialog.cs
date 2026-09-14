using THMS.Domain.Finance.Accounts;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class AccountImportPreviewDialog : ImportPreviewDialog<AccountImportPreview>
    {
        private readonly AccountImportOrchestrator _orchestrator;

        public AccountImportPreviewDialog(IList<AccountImportPreview> rows)
            : this(rows, new AccountImportOrchestrator())
        {
        }

        public AccountImportPreviewDialog(IList<AccountImportPreview> rows, AccountImportOrchestrator orchestrator)
            : base(rows)
        {
            _orchestrator = orchestrator;
        }

        protected override string WindowTitle => "Import Accounts";
        protected override string Heading => "Accounts to Import (Preview)";
        protected override string Singular => "account";
        protected override string Plural => "accounts";

        protected override void ConfigureGrid()
        {
            PrepareGrid();
            Grid.Columns.AddRange(
                TextColumn(nameof(AccountImportPreview.Name), "Name"),
                TypeColumn(),
                TextColumn(nameof(AccountImportPreview.AccountNumber), "Number"),
                TextColumn(nameof(AccountImportPreview.WebsiteUrl), "URL"),
                TextColumn(nameof(AccountImportPreview.CreditLimit), "Credit Limit"),
                TextColumn(nameof(AccountImportPreview.Apr), "APR"),
                TextColumn(nameof(AccountImportPreview.Principal), "Principal"),
                TextColumn(nameof(AccountImportPreview.TermMonths), "Term"));
        }

        protected override ImportResult ImportRows(
            IReadOnlyList<AccountImportPreview> rows,
            IProgress<ImportProgress> progress) =>
            _orchestrator.ImportAccounts(rows, progress);

        private static DataGridViewComboBoxColumn TypeColumn() =>
            new()
            {
                DataPropertyName = nameof(AccountImportPreview.Type),
                HeaderText = "Type",
                Name = nameof(AccountImportPreview.Type),
                DataSource = new[]
                {
                    AccountKinds.Bank,
                    AccountKinds.Credit,
                    AccountKinds.Loan,
                    AccountKinds.Mortgage,
                    AccountKinds.Investment,
                    AccountKinds.Internal,
                    AccountKinds.Utility,
                    AccountKinds.Service,
                    AccountKinds.Insurance
                },
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
    }
}
