using THMS.Domain.Household;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class HouseholdPersonDialog : Form
    {
        private readonly HouseholdPersonOrchestrator _people;
        private readonly HouseholdPerson _person;
        private readonly TextBox _name = new() { Name = "Name", MaxLength = 80 };
        private readonly NumericUpDown _birthYear = new()
        {
            Name = "BirthYear",
            DecimalPlaces = 0,
            Minimum = 0,
            Maximum = 2100,
            Increment = 1,
            TextAlign = HorizontalAlignment.Right
        };

        public HouseholdPersonDialog(HouseholdPersonOrchestrator people, HouseholdPerson? existing)
        {
            _people = people;
            _person = existing is null
                ? new HouseholdPerson()
                : new HouseholdPerson
                {
                    Id = existing.Id,
                    CreatedOn = existing.CreatedOn,
                    Name = existing.Name,
                    BirthYear = existing.BirthYear
                };

            Text = existing is null ? "Add person" : "Edit person";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(420, 180);

            _name.Text = _person.Name;
            _birthYear.Value = Math.Clamp(_person.BirthYear, (int)_birthYear.Minimum, (int)_birthYear.Maximum);

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(16)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddRow(stack, "Name", _name);
            AddRow(stack, "Birth year", _birthYear);

            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
            var save = new ThmsButton { Text = "Save", Name = "Save" };
            save.Click += (_, _) => OnSave();
            AcceptButton = save;
            CancelButton = cancel;
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(16, 8, 16, 16)
            };
            buttons.Height = Math.Max(save.PreferredSize.Height, cancel.PreferredSize.Height) + buttons.Padding.Vertical;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            Controls.Add(stack);
            Controls.Add(buttons);
            Shown += (_, _) =>
            {
                _name.SelectionStart = 0;
                _name.SelectionLength = 0;
            };
        }

        public Guid PersonId => _person.Id;

        private void OnSave()
        {
            try
            {
                _person.Name = _name.Text;
                _person.BirthYear = (int)_birthYear.Value;
                _people.Save(_person);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void AddRow(TableLayoutPanel stack, string caption, Control value)
        {
            var row = stack.RowCount;
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            stack.RowCount = row + 1;
            stack.Controls.Add(new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                UseMnemonic = false
            }, 0, row);
            value.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            value.Margin = new Padding(0, 4, 0, 4);
            stack.Controls.Add(value, 1, row);
        }
    }
}
