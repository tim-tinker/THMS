using System;
using System.Windows.Forms;
using THMS.Logic.ViewModels;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace THMS.UI.WinForms
{
    public partial class ImporterForm : Form
    {
        private readonly ImporterViewModel _vm;

        public ImporterForm(ImporterViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            // Bindings
            txtFilePath.DataBindings.Add("Text", _vm, nameof(_vm.SelectedFile));
            lblStatus.DataBindings.Add("Text", _vm, nameof(_vm.StatusMessage));
            progressBar.DataBindings.Add("Visible", _vm, nameof(_vm.IsBusy));

            // Enum binding for importer type
            comboImporterType.DataSource = Enum.GetValues(typeof(ImporterType));
            comboImporterType.DataBindings.Add("SelectedItem", _vm, nameof(_vm.SelectedImporter));
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Ingestion File"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _vm.SelectedFile = dialog.FileName;
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            _vm.Import();
        }
    }
}
