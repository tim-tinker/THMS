using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class AccountImportPreviewDialog : Form
    {
        private readonly AccountImportOrchestrator _orchestrator;
        private readonly BindingList<AccountImportPreview> _rows;
        private readonly DataGridView _grid = new();
        private readonly Label _status = new();

        public int ImportedCount { get; private set; }

        public AccountImportPreviewDialog(IList<AccountImportPreview> rows)
            : this(rows, new AccountImportOrchestrator())
        {
        }

        public AccountImportPreviewDialog(IList<AccountImportPreview> rows, AccountImportOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            _rows = new BindingList<AccountImportPreview>(rows.ToList());

            Text = "Import Accounts";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 520);
            MinimumSize = new Size(640, 360);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = "Accounts to Import (Preview)",
                TextAlign = ContentAlignment.MiddleLeft
            };

            ConfigureGrid();

            var btnDelete = new Button { Text = "Delete Row", AutoSize = true };
            btnDelete.Click += OnDeleteRow;
            var btnOk = new Button { Text = "OK", AutoSize = true };
            btnOk.Click += OnImport;
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 48,
                Padding = new Padding(8, 8, 8, 8),
                WrapContents = false
            };
            buttons.Controls.Add(btnOk);
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnDelete);

            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = $"{_rows.Count} account{(_rows.Count == 1 ? "" : "s")} loaded. Edit cells or delete rows, then click OK to import.";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(buttons);
            Controls.Add(heading);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void ConfigureGrid()
        {
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = true;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Dock = DockStyle.Fill;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.DataError += (_, e) => e.ThrowException = false;
            _grid.Columns.AddRange(
                TextColumn(nameof(AccountImportPreview.Name), "Name"),
                TypeColumn(),
                TextColumn(nameof(AccountImportPreview.AccountNumber), "Number"),
                TextColumn(nameof(AccountImportPreview.WebsiteUrl), "URL"),
                TextColumn(nameof(AccountImportPreview.CreditLimit), "Credit Limit"),
                TextColumn(nameof(AccountImportPreview.Apr), "APR"),
                TextColumn(nameof(AccountImportPreview.Principal), "Principal"),
                TextColumn(nameof(AccountImportPreview.TermMonths), "Term"));
            _grid.DataSource = _rows;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property
            };

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

        private void OnDeleteRow(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not AccountImportPreview row)
            {
                MessageBox.Show(this, "Select a row to delete.", "Import Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _rows.Remove(row);
            _status.Text = $"{_rows.Count} account{(_rows.Count == 1 ? "" : "s")} remaining.";
        }

        private void OnImport(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "There are no accounts to import.", "Import Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ImportedCount = _orchestrator.ImportAccounts(_rows);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Import failed.\n{ex.Message}", "Import Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
