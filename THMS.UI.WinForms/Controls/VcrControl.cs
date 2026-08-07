using System.ComponentModel;

namespace THMS.UI.WinForms.Controls
{
    public partial class VcrControl : UserControl
    {
        public event EventHandler? MoveBackward;
        public event EventHandler? MoveForward;
        public event EventHandler<EventArgs<DateTime>>? DateSelected;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SelectedDate
        {
            get => _datePicker.Value;
            set => _datePicker.Value = value;
        }
        public VcrControl()
        {
            InitializeComponent();
        }

        private void OnClickBack(object sender, EventArgs e)
        {
            _datePicker.Value = _datePicker.Value.AddDays(-1);
            MoveBackward?.Invoke(this, EventArgs.Empty);
        }

        private void OnClickForward(object sender, EventArgs e)
        {
            _datePicker.Value = _datePicker.Value.AddDays(+1);
            MoveForward?.Invoke(this, EventArgs.Empty);
        }

        private void OnValueChangedDate(object sender, EventArgs e)
        {
            DateSelected?.Invoke(this, new EventArgs<DateTime>(_datePicker.Value));
        }
    }
}
