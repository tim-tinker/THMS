using THMS.Domain.Household;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    /// <summary>
    /// Person entry for a payslip or W-2. An existing name can be picked, a new name can be typed,
    /// and Add opens the person dialog so the birth year can be saved at the same time.
    /// </summary>
    internal sealed class PersonField : UserControl
    {
        private readonly HouseholdPersonOrchestrator? _store;
        private readonly ComboBox _person = new()
        {
            Name = "Person",
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
        private readonly ThmsButton _add = new() { Name = "AddPerson", Text = "Add person", AutoSize = false };
        private bool _placing;
        private List<HouseholdPerson> _loaded = [];

        public PersonField(HouseholdPersonOrchestrator? store, IReadOnlyList<HouseholdPerson>? people, Guid selectedId)
        {
            _store = store;
            _add.Enabled = store is not null;
            _add.Click += (_, _) => AddPerson();
            Controls.Add(_person);
            Controls.Add(_add);
            Resize += (_, _) => Place();
            LoadPeople(people);
            SelectPerson(selectedId);
            Place();
        }

        public void SelectPerson(Guid id) => Select(id);

        public Guid Resolve()
        {
            var name = _person.Text.Trim();
            if (name.Length == 0)
                throw new InvalidOperationException("Person is required.");
            if (_person.SelectedItem is HouseholdPerson selected
                && string.Equals(selected.Name, name, StringComparison.OrdinalIgnoreCase))
                return selected.Id;

            var match = _loaded.FirstOrDefault(person =>
                string.Equals(person.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match.Id;
            if (_store is null)
                throw new InvalidOperationException("Person is required.");

            var created = new HouseholdPerson { Name = name };
            _store.Save(created);
            LoadPeople(null);
            Select(created.Id);
            return created.Id;
        }

        private void AddPerson()
        {
            if (_store is null)
                return;
            using var dialog = new HouseholdPersonDialog(_store, null);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            LoadPeople(null);
            Select(dialog.PersonId);
        }

        private void LoadPeople(IReadOnlyList<HouseholdPerson>? seed)
        {
            _loaded = (_store?.GetAll() ?? seed ?? []).ToList();
            var text = _person.Text;
            _person.BeginUpdate();
            try
            {
                _person.Items.Clear();
                foreach (var person in _loaded)
                    _person.Items.Add(person);
            }
            finally
            {
                _person.EndUpdate();
            }

            if (text.Length > 0)
                _person.Text = text;
        }

        private void Select(Guid id)
        {
            for (var i = 0; i < _person.Items.Count; i++)
            {
                if (_person.Items[i] is HouseholdPerson person && person.Id == id)
                {
                    _person.SelectedIndex = i;
                    return;
                }
            }

            if (_person.Items.Count == 1)
                _person.SelectedIndex = 0;
        }

        private void Place()
        {
            if (_placing)
                return;
            _placing = true;
            try
            {
                PlaceCore();
            }
            finally
            {
                _placing = false;
            }
        }

        private void PlaceCore()
        {
            var buttonSize = _add.PreferredSize;
            var comboHeight = Math.Max(_person.PreferredSize.Height, 23);
            var height = Math.Max(buttonSize.Height, comboHeight);
            MinimumSize = new Size(Math.Max(160, buttonSize.Width + 80), height);
            if (Height != height)
                Height = height;

            var buttonTop = Math.Max(0, (Height - buttonSize.Height) / 2);
            _add.SetBounds(
                Math.Max(0, Width - buttonSize.Width),
                buttonTop,
                Math.Min(buttonSize.Width, Width),
                buttonSize.Height);
            _person.SetBounds(
                0,
                Math.Max(0, (Height - comboHeight) / 2),
                Math.Max(0, Width - buttonSize.Width - 8),
                comboHeight);
        }
    }
}
