using THMS.Domain.Finance.Income;
using THMS.Domain.Household;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class FormW2Dialog : Form
    {
        private readonly IncomeOrchestrator _income;
        private readonly FormW2 _form;
        private readonly PersonField _person;
        private readonly NumericUpDown _taxYear = Year("TaxYear");
        private readonly TextBox _employer = new() { Name = "Employer", MaxLength = 120 };
        private readonly NumericUpDown _wages = Money("Wages");
        private readonly NumericUpDown _socialSecurity = Money("SocialSecurityWages");
        private readonly NumericUpDown _traditional = Money("Traditional401k");
        private readonly NumericUpDown _roth = Money("Roth401k");
        private readonly NumericUpDown _hsa = Money("Hsa");

        public FormW2Dialog(
            IncomeOrchestrator income,
            FormW2? existing,
            int? taxYear = null,
            string? employer = null,
            IReadOnlyList<HouseholdPerson>? people = null,
            Guid? personId = null,
            HouseholdPersonOrchestrator? peopleStore = null)
        {
            _income = income;
            _person = new PersonField(peopleStore, people, Guid.Empty);
            var person = existing is not null && existing.PersonId != Guid.Empty
                ? existing.PersonId
                : personId ?? Guid.Empty;
            _form = existing is null
                ? new FormW2
                {
                    TaxYear = taxYear ?? DateTime.Today.Year - 1,
                    Employer = employer?.Trim() ?? "",
                    PersonId = person
                }
                : new FormW2
                {
                    Id = existing.Id,
                    CreatedOn = existing.CreatedOn,
                    TaxYear = existing.TaxYear,
                    Employer = existing.Employer,
                    PersonId = person,
                    Wages = existing.Wages,
                    SocialSecurityWages = existing.SocialSecurityWages,
                    Traditional401k = existing.Traditional401k,
                    Roth401k = existing.Roth401k,
                    Hsa = existing.Hsa
                };

            Text = existing is null ? "Add W-2" : "Edit W-2";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            var captions = new[]
            {
                "Person",
                "Tax year",
                "Box c  Employer name",
                "Box 1  Wages, tips, other compensation",
                "Box 3  Social Security wages",
                "Box 12, code D  Traditional 401(k)",
                "Box 12, code AA  Roth 401(k)",
                "Box 12, code W  HSA"
            };
            const int valueWidth = 360;
            var labelWidth = captions.Max(text => TextRenderer.MeasureText(text, Font).Width) + 20;
            var fitWidth = labelWidth + valueWidth + 40;
            ClientSize = new Size(fitWidth, 480);
            var frameWidth = Width - ClientSize.Width;
            var frameHeight = Height - ClientSize.Height;
            MinimumSize = new Size(fitWidth + frameWidth, 480 + frameHeight);
            ClientSize = new Size(fitWidth + 80, 480);

            _person.SelectPerson(_form.PersonId);
            _taxYear.Value = Math.Clamp(_form.TaxYear, (int)_taxYear.Minimum, (int)_taxYear.Maximum);
            _employer.Text = _form.Employer;
            Set(_wages, _form.Wages);
            Set(_socialSecurity, _form.SocialSecurityWages);
            Set(_traditional, _form.Traditional401k);
            Set(_roth, _form.Roth401k);
            Set(_hsa, _form.Hsa);

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                ColumnCount = 2,
                Padding = new Padding(20, 16, 20, 8)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, valueWidth));

            AddRow(stack, "Person", _person);
            FitRow(stack, _person);
            AddRow(stack, "Tax year", _taxYear);
            AddRow(stack, "Box c  Employer name", _employer);
            AddRow(stack, "Box 1  Wages, tips, other compensation", _wages);
            AddRow(stack, "Box 3  Social Security wages", _socialSecurity);
            AddRow(stack, "Box 12, code D  Traditional 401(k)", _traditional);
            AddRow(stack, "Box 12, code AA  Roth 401(k)", _roth);
            AddRow(stack, "Box 12, code W  HSA", _hsa);
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stack.RowCount = stack.RowStyles.Count;
            while (stack.RowStyles.Count > stack.RowCount)
                stack.RowStyles.RemoveAt(stack.RowStyles.Count - 1);
            Shown += (_, _) =>
            {
                FitRow(stack, _person);
                _employer.SelectionStart = 0;
                _employer.SelectionLength = 0;
            };

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
        }

        public Guid FormId => _form.Id;

        private void OnSave()
        {
            try
            {
                _form.PersonId = _person.Resolve();
                _form.TaxYear = (int)_taxYear.Value;
                _form.Employer = _employer.Text;
                _form.Wages = _wages.Value;
                _form.SocialSecurityWages = _socialSecurity.Value;
                _form.Traditional401k = _traditional.Value;
                _form.Roth401k = _roth.Value;
                _form.Hsa = _hsa.Value;
                _income.Save(_form);
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
            stack.RowCount = stack.RowStyles.Count;
            while (stack.RowStyles.Count > stack.RowCount)
                stack.RowStyles.RemoveAt(stack.RowStyles.Count - 1);
            stack.RowStyles[row] = new RowStyle(SizeType.Absolute, 40);

            stack.Controls.Add(new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 12, 0)
            }, 0, row);
            value.Dock = DockStyle.None;
            value.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            value.Margin = new Padding(0, 4, 0, 4);
            stack.Controls.Add(value, 1, row);
        }

        private static void FitRow(TableLayoutPanel stack, Control value)
        {
            var row = stack.GetRow(value);
            var needed = value.Height + value.Margin.Vertical;
            if (needed > stack.RowStyles[row].Height)
                stack.RowStyles[row].Height = needed;
            stack.PerformLayout();
        }

        private static void Set(NumericUpDown input, decimal amount) =>
            input.Value = Math.Clamp(amount, input.Minimum, input.Maximum);

        private static NumericUpDown Year(string name) => new()
        {
            Name = name,
            DecimalPlaces = 0,
            Minimum = 1950,
            Maximum = 2100,
            Increment = 1,
            TextAlign = HorizontalAlignment.Right,
            MinimumSize = new Size(120, 28)
        };

        private static NumericUpDown Money(string name) => new()
        {
            Name = name,
            DecimalPlaces = 2,
            Minimum = 0,
            Maximum = 10_000_000m,
            Increment = 100m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right,
            MinimumSize = new Size(120, 28)
        };
    }
}
