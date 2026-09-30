using THMS.Domain.Finance.Accounts;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal static class TransactionFileImportLauncher
    {
        public static bool IsSpreadsheet(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xls", StringComparison.OrdinalIgnoreCase);
        }

        public static void ImportFile(IWin32Window? owner, Account account, string filePath, Action? afterImport = null)
        {
            try
            {
                var import = new AccountTransactionFileImportOrchestrator();
                var sheet = import.Read(filePath);
                if (sheet.Headers.Count == 0 || sheet.Rows.Count == 0)
                {
                    MessageBox.Show(owner, "The selected file did not contain any transactions.", "Import Transactions",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var map = import.GetMap(account.Id)
                    ?? AccountTransactionFileImportOrchestrator.SuggestMap(account.Id, sheet.Headers);
                using var mapping = new TransactionColumnMapDialog(account.Name, sheet, map);
                if (mapping.ShowDialog(owner) != DialogResult.OK)
                    return;

                import.SaveMap(mapping.Map);
                var preview = import.BuildPreview(account.Id, sheet, mapping.Map);
                if (preview.Count == 0)
                {
                    MessageBox.Show(owner, "The selected columns did not contain any transactions.", "Import Transactions",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var review = new TransactionFileImportPreviewDialog(account.Name, preview);
                if (review.ShowDialog(owner) != DialogResult.OK)
                    return;

                AppStatus.Set("Importing transactions...", busy: true);
                var result = import.Import(account.Id, review.Selected, AppStatus.ForImport());
                afterImport?.Invoke();
                AppStatus.Set(ImportStatusText.Imported(result, "transaction", "transactions"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                MessageBox.Show(owner, $"Could not import transactions.\n{ex.Message}", "Import Transactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
