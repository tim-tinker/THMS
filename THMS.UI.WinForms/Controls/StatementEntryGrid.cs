namespace THMS.UI.WinForms.Controls
{
    internal sealed class StatementEntryGrid : DataGridView
    {
        public StatementEntryGrid()
        {
            Dock = DockStyle.Fill;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AutoGenerateColumns = false;
            RowHeadersVisible = false;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            CurrentCellDirtyStateChanged += OnCurrentCellDirtyStateChanged;
            CellClick += OnCellClickBeginEdit;
            DataError += (_, e) => e.ThrowException = false;
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Tab or Keys.Enter)
                CommitCurrentEdit();
            return base.ProcessDataGridViewKey(e);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) is Keys.Tab or Keys.Enter)
                CommitCurrentEdit();
            return base.ProcessDialogKey(keyData);
        }

        private void OnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (!IsCurrentCellDirty || CurrentCell?.OwningColumn is null)
                return;
            if (CurrentCell.OwningColumn is CalendarColumn or DataGridViewComboBoxColumn)
                CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void OnCellClickBeginEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (Columns[e.ColumnIndex] is CalendarColumn or DataGridViewComboBoxColumn)
                BeginEdit(true);
        }

        private void CommitCurrentEdit()
        {
            if (IsCurrentCellDirty)
                CommitEdit(DataGridViewDataErrorContexts.Commit);
            if (IsCurrentCellInEditMode)
                EndEdit();
            if (DataSource is not null && BindingContext is not null)
            {
                try
                {
                    BindingContext[DataSource].EndCurrentEdit();
                }
                catch (ArgumentException)
                {
                }
            }
        }
    }
}
