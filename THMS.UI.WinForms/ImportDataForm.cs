using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms
{
    public partial class ImportDataForm : Form
    {
        public ImportDataForm()
        {
            InitializeComponent();
        }

        private void OnClickAccountFile(object sender, EventArgs e)
        {
            if (DialogResult.OK == _openAccountFile.ShowDialog())
            {
                _textAccountFile.Text = _openAccountFile.FileName;
            }
        }

        private void OnClickOpenTransactionFiles(object sender, EventArgs e)
        {
            if (DialogResult.OK == _openTransactionFiles.ShowDialog())
            {
                _listTransactionFiles.Items.AddRange(_openTransactionFiles.FileNames);
            }
        }

        private void OnClickImport(object sender, EventArgs e)
        {
            // 1. Import accounts
            var accountImporter = new SpreadsheetAccountImporter();
            accountImporter.Import(_textAccountFile.Text);

            // 2. Import transactions
            var txImporter = new SpreadsheetTransactionImporter();

            foreach (var file in _listTransactionFiles.Items)
                txImporter.Import(file.ToString());

            // 3. Run orchestrator
            var orchestrator = new TransactionUpdaterOrchestrator();
            orchestrator.RunLedgerUpdate();

            // 4. Close dialog
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnClickCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
