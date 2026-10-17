namespace THMS.UI.WinForms.Controls
{
    internal sealed class StatementEntryGrid : DataGridView
    {
        private bool _tabAppendHandled;

        public event EventHandler? AppendRow;

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
            DataGridViewUtil.EnableDoubleBuffering(this);
        }

        public void EditFirstCellOfLastRow()
        {
            if (IsDisposed)
                return;
            if (!IsHandleCreated)
            {
                StartEditOnFirstCell();
                return;
            }

            BeginInvoke(StartEditOnFirstCell);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Tab or Keys.Enter)
                CommitCurrentEdit();
            if (TryAppendRowOnTab(e.KeyData))
                return true;
            return base.ProcessDataGridViewKey(e);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) is Keys.Tab or Keys.Enter)
                CommitCurrentEdit();
            if (TryAppendRowOnTab(keyData))
                return true;
            return base.ProcessDialogKey(keyData);
        }

        private bool TryAppendRowOnTab(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) != Keys.Tab || (keyData & Keys.Shift) != 0)
                return false;
            if (_tabAppendHandled)
                return true;
            if (AppendRow is null || !IsOnLastCell())
                return false;

            _tabAppendHandled = true;
            try
            {
                CommitCurrentEdit();
                AppendRow(this, EventArgs.Empty);
                EditFirstCellOfLastRow();
            }
            finally
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke(() => _tabAppendHandled = false);
                else
                    _tabAppendHandled = false;
            }

            return true;
        }

        private bool IsOnLastCell()
        {
            if (CurrentCell is null || Rows.Count == 0)
                return false;

            var lastColumn = -1;
            for (var i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].Visible)
                    lastColumn = i;
            }

            return lastColumn >= 0
                && CurrentCell.ColumnIndex == lastColumn
                && CurrentCell.RowIndex == Rows.Count - 1;
        }

        private void StartEditOnFirstCell()
        {
            if (IsDisposed || Rows.Count == 0 || Columns.Count == 0)
                return;

            var column = -1;
            for (var i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].Visible)
                {
                    column = i;
                    break;
                }
            }

            if (column < 0)
                return;

            Focus();
            CurrentCell = this[column, Rows.Count - 1];
            BeginEdit(true);
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
