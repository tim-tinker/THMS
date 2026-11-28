using THMS.Domain.Finance.Income;
using THMS.Domain.Household;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class PayslipDialog : Form
    {
        private readonly IncomeOrchestrator _income;
        private readonly Payslip _payslip;
        private readonly ToolTip _tips = new() { ShowAlways = true };
        private readonly PersonField _person;
        private readonly TextBox _employer = new() { Name = "Employer", MaxLength = 120 };
        private readonly DateTimePicker _payDate = new() { Format = DateTimePickerFormat.Short, Name = "PayDate" };
        private readonly NumericUpDown _gross = Money("Gross");
        private readonly NumericUpDown _grossYearToDate = Money("GrossYearToDate");
        private readonly NumericUpDown _traditional = Money("Traditional401k");
        private readonly NumericUpDown _traditionalYearToDate = Money("Traditional401kYearToDate");
        private readonly NumericUpDown _roth = Money("Roth401k");
        private readonly NumericUpDown _rothYearToDate = Money("Roth401kYearToDate");
        private readonly NumericUpDown _socialSecurityTax = Money("SocialSecurityTax");
        private readonly NumericUpDown _socialSecurityTaxYearToDate = Money("SocialSecurityTaxYearToDate");
        private readonly NumericUpDown _medicareTax = Money("MedicareTax");
        private readonly NumericUpDown _medicareTaxYearToDate = Money("MedicareTaxYearToDate");
        private readonly NumericUpDown _federalIncomeTax = Money("FederalIncomeTax");
        private readonly NumericUpDown _federalIncomeTaxYearToDate = Money("FederalIncomeTaxYearToDate");
        private readonly NumericUpDown _groupTermLife = Money("GroupTermLifeTaxable");
        private readonly NumericUpDown _groupTermLifeYearToDate = Money("GroupTermLifeTaxableYearToDate");
        private readonly NumericUpDown _hsa = Money("Hsa");
        private readonly NumericUpDown _hsaYearToDate = Money("HsaYearToDate");

        public PayslipDialog(
            IncomeOrchestrator income,
            Payslip? existing,
            string? employer = null,
            IReadOnlyList<HouseholdPerson>? people = null,
            Guid? personId = null,
            HouseholdPersonOrchestrator? peopleStore = null)
        {
            _income = income;
            _person = new PersonField(peopleStore, people, Guid.Empty);
            var prefill = existing is not null && !string.IsNullOrWhiteSpace(existing.Employer)
                ? existing.Employer
                : employer?.Trim() ?? "";
            var person = existing is not null && existing.PersonId != Guid.Empty
                ? existing.PersonId
                : personId ?? Guid.Empty;
            _payslip = existing is null
                ? new Payslip { PayDate = DateTime.Today, Employer = prefill, PersonId = person }
                : new Payslip
                {
                    Id = existing.Id,
                    CreatedOn = existing.CreatedOn,
                    PayDate = existing.PayDate,
                    Employer = prefill,
                    PersonId = person,
                    Gross = existing.Gross,
                    GrossYearToDate = existing.GrossYearToDate,
                    Traditional401k = existing.Traditional401k,
                    Traditional401kYearToDate = existing.Traditional401kYearToDate,
                    Roth401k = existing.Roth401k,
                    Roth401kYearToDate = existing.Roth401kYearToDate,
                    SocialSecurityTax = existing.SocialSecurityTax,
                    SocialSecurityTaxYearToDate = existing.SocialSecurityTaxYearToDate,
                    MedicareTax = existing.MedicareTax,
                    MedicareTaxYearToDate = existing.MedicareTaxYearToDate,
                    FederalIncomeTax = existing.FederalIncomeTax,
                    FederalIncomeTaxYearToDate = existing.FederalIncomeTaxYearToDate,
                    GroupTermLifeTaxable = existing.GroupTermLifeTaxable,
                    GroupTermLifeTaxableYearToDate = existing.GroupTermLifeTaxableYearToDate,
                    Hsa = existing.Hsa,
                    HsaYearToDate = existing.HsaYearToDate
                };

            Text = existing is null ? "Add Payslip" : "Edit Payslip";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            var captions = new[]
            {
                "Person", "Employer", "Pay date", "Gross pay", "Federal income tax", "Social Security tax", "Medicare tax",
                "Traditional 401(k)", "Roth 401(k)", "GTLI taxable", "HSA"
            };
            const int valueWidth = 180;
            const int rowHeight = 40;
            var headerFont = new Font(Font, FontStyle.Bold);
            var headerHeight = TextRenderer.MeasureText("Year to date", headerFont).Height + 16;
            var labelWidth = captions.Max(text => TextRenderer.MeasureText(text, Font).Width) + 24;
            var fitWidth = labelWidth + valueWidth * 2 + 48;
            var personRow = _person.Height + 8;
            var fitHeight = headerHeight + rowHeight * captions.Length + Math.Max(0, personRow - rowHeight) + 24 + 72;
            ClientSize = new Size(fitWidth, fitHeight);
            var frameWidth = Width - ClientSize.Width;
            var frameHeight = Height - ClientSize.Height;
            MinimumSize = new Size(fitWidth + frameWidth, fitHeight + frameHeight);
            ClientSize = new Size(fitWidth + 40, fitHeight);

            _person.SelectPerson(_payslip.PersonId);
            _employer.Text = _payslip.Employer;
            _payDate.Value = _payslip.PayDate;
            Set(_gross, _payslip.Gross);
            Set(_grossYearToDate, _payslip.GrossYearToDate);
            Set(_traditional, _payslip.Traditional401k);
            Set(_traditionalYearToDate, _payslip.Traditional401kYearToDate);
            Set(_roth, _payslip.Roth401k);
            Set(_rothYearToDate, _payslip.Roth401kYearToDate);
            Set(_socialSecurityTax, _payslip.SocialSecurityTax);
            Set(_socialSecurityTaxYearToDate, _payslip.SocialSecurityTaxYearToDate);
            Set(_medicareTax, _payslip.MedicareTax);
            Set(_medicareTaxYearToDate, _payslip.MedicareTaxYearToDate);
            Set(_federalIncomeTax, _payslip.FederalIncomeTax);
            Set(_federalIncomeTaxYearToDate, _payslip.FederalIncomeTaxYearToDate);
            Set(_groupTermLife, _payslip.GroupTermLifeTaxable);
            Set(_groupTermLifeYearToDate, _payslip.GroupTermLifeTaxableYearToDate);
            Set(_hsa, _payslip.Hsa);
            Set(_hsaYearToDate, _payslip.HsaYearToDate);

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                ColumnCount = 3,
                Padding = new Padding(20, 16, 20, 8)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            AddRow(stack, "Person", _person, null);
            stack.SetColumnSpan(_person, 2);
            FitRow(stack, _person);
            AddRow(stack, "Employer", _employer, null);
            stack.SetColumnSpan(_employer, 2);
            AddRow(stack, "Pay date", _payDate, null);
            AddHeaderRow(stack, headerFont, headerHeight);
            AddRow(stack, "Gross pay", _gross, _grossYearToDate);
            AddRow(stack, "Federal income tax", _federalIncomeTax, _federalIncomeTaxYearToDate);
            AddRow(stack, "Social Security tax", _socialSecurityTax, _socialSecurityTaxYearToDate);
            AddRow(stack, "Medicare tax", _medicareTax, _medicareTaxYearToDate);
            AddRow(stack, "Traditional 401(k)", _traditional, _traditionalYearToDate);
            AddRow(stack, "Roth 401(k)", _roth, _rothYearToDate);
            AddRow(stack, "GTLI taxable", _groupTermLife, _groupTermLifeYearToDate);
            AddRow(stack, "HSA", _hsa, _hsaYearToDate);

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
                FitRow(stack, _person);
                _employer.SelectionStart = 0;
                _employer.SelectionLength = 0;
            };
        }

        public Guid PayslipId => _payslip.Id;

        internal ToolTip Tips => _tips;

        private void OnSave()
        {
            try
            {
                _payslip.PersonId = _person.Resolve();
                _payslip.Employer = _employer.Text;
                _payslip.PayDate = _payDate.Value.Date;
                _payslip.Gross = _gross.Value;
                _payslip.GrossYearToDate = _grossYearToDate.Value;
                _payslip.Traditional401k = _traditional.Value;
                _payslip.Traditional401kYearToDate = _traditionalYearToDate.Value;
                _payslip.Roth401k = _roth.Value;
                _payslip.Roth401kYearToDate = _rothYearToDate.Value;
                _payslip.SocialSecurityTax = _socialSecurityTax.Value;
                _payslip.SocialSecurityTaxYearToDate = _socialSecurityTaxYearToDate.Value;
                _payslip.MedicareTax = _medicareTax.Value;
                _payslip.MedicareTaxYearToDate = _medicareTaxYearToDate.Value;
                _payslip.FederalIncomeTax = _federalIncomeTax.Value;
                _payslip.FederalIncomeTaxYearToDate = _federalIncomeTaxYearToDate.Value;
                _payslip.GroupTermLifeTaxable = _groupTermLife.Value;
                _payslip.GroupTermLifeTaxableYearToDate = _groupTermLifeYearToDate.Value;
                _payslip.Hsa = _hsa.Value;
                _payslip.HsaYearToDate = _hsaYearToDate.Value;
                _income.Save(_payslip);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void AddRow(TableLayoutPanel stack, string caption, Control current, Control? yearToDate)
        {
            var row = stack.RowCount;
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            stack.RowCount = row + 1;
            stack.Controls.Add(new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 12, 0)
            }, 0, row);
            current.Dock = DockStyle.None;
            current.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            current.Margin = new Padding(0, 4, 8, 4);
            stack.Controls.Add(current, 1, row);
            if (yearToDate is null)
                return;
            yearToDate.Dock = DockStyle.None;
            yearToDate.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            yearToDate.Margin = new Padding(8, 4, 0, 4);
            stack.Controls.Add(yearToDate, 2, row);
        }

        private static void FitRow(TableLayoutPanel stack, Control value)
        {
            var row = stack.GetRow(value);
            var needed = value.Height + value.Margin.Vertical;
            if (needed > stack.RowStyles[row].Height)
                stack.RowStyles[row].Height = needed;
            stack.PerformLayout();
        }

        private void AddHeaderRow(TableLayoutPanel stack, Font font, int height)
        {
            var row = stack.RowCount;
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            stack.RowCount = row + 1;
            stack.Controls.Add(ColumnHeading("Current", font), 1, row);
            stack.Controls.Add(YearToDateHeading(font), 2, row);
        }

        private static Label ColumnHeading(string text, Font font) => new()
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = font,
            Padding = new Padding(0, 4, 0, 6),
            Margin = new Padding(0, 0, 8, 0),
            UseMnemonic = false
        };

        private Control YearToDateHeading(Font font)
        {
            var help = new Label
            {
                Name = "YearToDateHelp",
                Text = "?",
                AutoSize = false,
                Dock = DockStyle.Right,
                Width = TextRenderer.MeasureText("?", font).Width + 8,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = font,
                Cursor = Cursors.Help,
                UseMnemonic = false
            };
            var title = ColumnHeading("Year to date", font);
            title.Padding = new Padding(0, 4, 2, 6);
            var host = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
            host.Controls.Add(title);
            host.Controls.Add(help);
            _tips.SetToolTip(help, "Year-to-date amounts include this paycheck.");
            return host;
        }

        private static void Set(NumericUpDown input, decimal value) =>
            input.Value = Math.Clamp(value, input.Minimum, input.Maximum);

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
