using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class CalendarColumn : DataGridViewColumn
    {
        public CalendarColumn()
            : base(new CalendarCell())
        {
            ValueType = typeof(DateTime);
            SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        public override DataGridViewCell? CellTemplate
        {
            get => base.CellTemplate;
            set
            {
                if (value is not null and not CalendarCell)
                    throw new InvalidCastException("CalendarColumn cells must be CalendarCell.");
                base.CellTemplate = value;
            }
        }
    }

    internal sealed class CalendarCell : DataGridViewTextBoxCell
    {
        public CalendarCell()
        {
            Style.Format = "d";
            ValueType = typeof(DateTime);
        }

        public override Type EditType => typeof(CalendarEditingControl);
        public override Type FormattedValueType => typeof(string);
        public override object DefaultNewRowValue => DateTime.Today;

        public override void InitializeEditingControl(
            int rowIndex,
            object? initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            if (DataGridView?.EditingControl is not CalendarEditingControl control)
                return;

            var date = Value is DateTime value && value.Year > 1 ? value.Date : DateTime.Today;
            control.SetDate(date);
        }

        public override object ParseFormattedValue(
            object? formattedValue,
            DataGridViewCellStyle cellStyle,
            TypeConverter? formattedValueTypeConverter,
            TypeConverter? valueTypeConverter)
        {
            if (formattedValue is DateTime date)
                return date.Date;
            if (formattedValue is string text && DateTime.TryParse(text, out var parsed))
                return parsed.Date;
            return DateTime.Today;
        }
    }

    internal sealed class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl
    {
        private DataGridView? _grid;
        private bool _valueChanged;
        private int _rowIndex;
        private bool _initializing;

        public CalendarEditingControl()
        {
            Format = DateTimePickerFormat.Short;
            ShowUpDown = false;
        }

        public void SetDate(DateTime date)
        {
            _initializing = true;
            try
            {
                Value = date.Year > 1 ? date.Date : DateTime.Today;
                _valueChanged = false;
            }
            finally
            {
                _initializing = false;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [AllowNull]
        public object EditingControlFormattedValue
        {
            get => Value.ToString("d");
            set
            {
                if (_initializing)
                    return;
                if (value is DateTime date)
                    Value = date.Year > 1 ? date.Date : DateTime.Today;
                else if (value is string text && DateTime.TryParse(text, out var parsed))
                    Value = parsed.Date;
            }
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) =>
            Value.ToString("d");

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
        {
            Font = style.Font;
            CalendarForeColor = style.ForeColor;
            CalendarMonthBackground = style.BackColor;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int EditingControlRowIndex
        {
            get => _rowIndex;
            set => _rowIndex = value;
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Tab or Keys.Enter)
                return false;

            return key is Keys.Left or Keys.Up or Keys.Down or Keys.Right
                or Keys.Home or Keys.End or Keys.PageDown or Keys.PageUp
                || !dataGridViewWantsInputKey;
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
        }

        public bool RepositionEditingControlOnValueChange => false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataGridView? EditingControlDataGridView
        {
            get => _grid;
            set => _grid = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EditingControlValueChanged
        {
            get => _valueChanged;
            set => _valueChanged = value;
        }

        public Cursor EditingPanelCursor => Cursor;

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) is Keys.Tab or Keys.Enter)
                return false;
            return base.ProcessDialogKey(keyData);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) is Keys.Tab or Keys.Enter)
                return false;
            return base.IsInputKey(keyData);
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            if (!_initializing)
            {
                _valueChanged = true;
                _grid?.NotifyCurrentCellDirty(true);
            }

            base.OnValueChanged(eventargs);
        }
    }
}
