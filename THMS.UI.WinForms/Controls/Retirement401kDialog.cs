using THMS.Domain.Finance.Planning;
using THMS.Domain.Household;
using THMS.Logic.Finance.Income;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class Retirement401kDialog : Form
    {
        private readonly Font _boldFont;
        private readonly IncomeOrchestrator _income;
        private readonly Retirement401kLimitOrchestrator _limits;
        private readonly Retirement401kMatchOrchestrator _match;
        private readonly Retirement401kBirthYearOrchestrator _birthYearStore;
        private readonly HouseholdPersonOrchestrator _people;
        private readonly ToolTip _tips = new() { ShowAlways = true };
        private readonly ComboBox _person = new() { DropDownStyle = ComboBoxStyle.DropDownList, Name = "Person" };
        private readonly ComboBox _employer = new() { DropDownStyle = ComboBoxStyle.DropDownList, Name = "Employer" };
        private readonly NumericUpDown _birthYear = Whole(0, 2100, "BirthYear");
        private readonly NumericUpDown _age = ReadOnlyWhole("Age");
        private readonly NumericUpDown _pay = MoneyInput(0m, "AnnualPay");
        private readonly NumericUpDown _priorWages = MoneyInput(0m, "PriorYearWages");
        private readonly ComboBox _frequency = new() { DropDownStyle = ComboBoxStyle.DropDownList, Name = "PayFrequency" };
        private readonly NumericUpDown _federal = PercentInput(22m, "FederalRate");
        private readonly NumericUpDown _state = PercentInput(0m, "StateRate");
        private readonly NumericUpDown _firstRate = PercentInput(0m, "FirstMatchRate", 500m);
        private readonly NumericUpDown _firstOfPay = PercentInput(0m, "FirstMatchPercent");
        private readonly NumericUpDown _nextRate = PercentInput(0m, "NextMatchRate", 500m);
        private readonly NumericUpDown _nextOfPay = PercentInput(0m, "NextMatchPercent");
        private readonly CheckBox _offersRoth = new()
        {
            Text = "Plan offers a Roth 401(k)",
            Checked = true,
            AutoSize = true,
            Name = "PlanOffersRoth",
            Margin = new Padding(0, 8, 0, 4)
        };
        private readonly NumericUpDown _taxYear;
        private readonly NumericUpDown _elective;
        private readonly NumericUpDown _catchUp;
        private readonly NumericUpDown _higherCatchUp;
        private readonly NumericUpDown _threshold;
        private readonly NumericUpDown _additions;
        private readonly NumericUpDown _compensationCap;
        private readonly NumericUpDown _maxRegular = ReadOnlyPercent("MaxRegularPercent");
        private readonly NumericUpDown _maxCatchUp = ReadOnlyPercent("MaxCatchUpPercent");
        private readonly NumericUpDown _currentPreTax = ReadOnlyPercent("CurrentPreTaxPercent");
        private readonly NumericUpDown _currentRoth = ReadOnlyPercent("CurrentRothPercent");
        private readonly NumericUpDown _selectedPreTax = FinePercent("SelectedPreTaxPercent");
        private readonly NumericUpDown _selectedRoth = FinePercent("SelectedRothPercent");
        private readonly NumericUpDown _selectedCatchUp = FinePercent("SelectedCatchUpPercent");
        private readonly NumericUpDown _preTaxWithheld = ReadOnlyMoney("PreTaxWithheld");
        private readonly NumericUpDown _rothWithheld = ReadOnlyMoney("RothWithheld");
        private readonly NumericUpDown _federalSaved = ReadOnlyMoney("FederalTaxSaved");
        private readonly NumericUpDown _stateSaved = ReadOnlyMoney("StateTaxSaved");
        private readonly NumericUpDown _takeHome = ReadOnlySignedMoney("TakeHomeLowerBy");
        private readonly NumericUpDown _currentMatch = ReadOnlyMoney("CurrentEmployerMatch");
        private readonly NumericUpDown _selectedMatch = ReadOnlyMoney("SelectedEmployerMatch");
        private readonly NumericUpDown _employeeIncrease = ReadOnlySignedMoney("EmployeeContributionIncrease");
        private readonly NumericUpDown _employerIncrease = ReadOnlySignedMoney("EmployerContributionIncrease");
        private readonly Label _notice = new()
        {
            Name = "SelectionNotice",
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(0, 4, 0, 8)
        };
        private readonly ThmsTabControl _tabs = new() { Name = "CalculatorTabs", Dock = DockStyle.Fill, MaxTabWidth = 180 };
        private decimal? _periodGross;
        private bool _selectionEdited;
        private bool _ready;
        private bool _suspend;
        private bool _compact;
        private bool _fitted;

        internal ToolTip Tips => _tips;

        public Retirement401kDialog()
            : this(new IncomeOrchestrator(), static () => DateTime.Today, new Retirement401kLimitOrchestrator(), new Retirement401kMatchOrchestrator(), new Retirement401kBirthYearOrchestrator(), new HouseholdPersonOrchestrator())
        {
        }

        internal Retirement401kDialog(IncomeOrchestrator income, Func<DateTime> today)
            : this(income, today, new Retirement401kLimitOrchestrator(), new Retirement401kMatchOrchestrator(), new Retirement401kBirthYearOrchestrator(), new HouseholdPersonOrchestrator())
        {
        }

        internal Retirement401kDialog(
            IncomeOrchestrator income,
            Func<DateTime> today,
            Retirement401kLimitOrchestrator limits)
            : this(income, today, limits, new Retirement401kMatchOrchestrator(), new Retirement401kBirthYearOrchestrator(), new HouseholdPersonOrchestrator())
        {
        }

        internal Retirement401kDialog(
            IncomeOrchestrator income,
            Func<DateTime> today,
            Retirement401kLimitOrchestrator limits,
            Retirement401kMatchOrchestrator match)
            : this(income, today, limits, match, new Retirement401kBirthYearOrchestrator(), new HouseholdPersonOrchestrator())
        {
        }

        internal Retirement401kDialog(
            IncomeOrchestrator income,
            Func<DateTime> today,
            Retirement401kLimitOrchestrator limits,
            Retirement401kMatchOrchestrator match,
            Retirement401kBirthYearOrchestrator birthYear)
            : this(income, today, limits, match, birthYear, new HouseholdPersonOrchestrator())
        {
        }

        internal Retirement401kDialog(
            IncomeOrchestrator income,
            Func<DateTime> today,
            Retirement401kLimitOrchestrator limits,
            Retirement401kMatchOrchestrator match,
            Retirement401kBirthYearOrchestrator birthYear,
            HouseholdPersonOrchestrator people)
        {
            ArgumentNullException.ThrowIfNull(today);
            _income = income;
            _limits = limits;
            _match = match;
            _birthYearStore = birthYear;
            _people = people;
            var published = Retirement401kLimits.For2026();
            _taxYear = YearInput(published.TaxYear);
            _elective = MoneyInput(published.ElectiveDeferral, "ElectiveDeferralLimit");
            _catchUp = MoneyInput(published.CatchUp, "CatchUpLimit");
            _higherCatchUp = MoneyInput(published.HigherCatchUp, "SuperCatchUpLimit");
            _threshold = MoneyInput(published.RothCatchUpWageThreshold, "RothWageThreshold");
            _additions = MoneyInput(published.AnnualAdditions, "AnnualAdditionsLimit");
            _compensationCap = MoneyInput(published.CompensationCap, "CompensationCap");
            _boldFont = new Font(Font, FontStyle.Bold);

            Text = "401(k) calculator";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(960, 860);
            ClientSize = new Size(1120, 1020);

            _frequency.Items.AddRange(new object[]
            {
                new PayChoice(PayFrequency.Weekly, "Weekly (52)"),
                new PayChoice(PayFrequency.Biweekly, "Biweekly (26)"),
                new PayChoice(PayFrequency.Semimonthly, "Semimonthly (24)"),
                new PayChoice(PayFrequency.Monthly, "Monthly (12)")
            });
            _frequency.SelectedIndex = 1;

            var selection = AddPage("Selection");
            _compact = true;
            Section(selection, "Person, employer, and year");
            _person.DropDownWidth = 420;
            _employer.DropDownWidth = 420;
            AddRow(selection, "Person", _person);
            AddRow(selection, "Employer", _employer);
            AddRow(selection, "Tax year", _taxYear);
            Section(selection, "Maximums");
            AddRow(selection, "Regular deferral, traditional plus Roth (%)", _maxRegular);
            AddRow(selection, "Catch-up (%)", _maxCatchUp);
            Section(selection, "Current paycheck");
            AddRow(selection, "Pre-tax (%)", _currentPreTax);
            AddRow(selection, "Roth (%)", _currentRoth);
            AddRow(selection, "Roth catch-up", new Label
            {
                Name = "CurrentCatchUp",
                Text = "Not on the payslip",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 2, 0, 2)
            });
            Section(selection, "Selected percentages");
            AddRow(selection, "Pre-tax (%)", _selectedPreTax);
            AddRow(selection, "Roth (%)", _selectedRoth);
            AddRow(selection, "Roth catch-up (%)", _selectedCatchUp);
            AddSpan(selection, _notice);
            Section(selection, "This paycheck");
            AddRow(selection, "Pre-tax withheld", _preTaxWithheld);
            AddRow(selection, "Roth withheld", _rothWithheld);
            AddRow(selection, "Federal income tax saved", _federalSaved);
            AddRow(selection, "State income tax saved", _stateSaved);
            AddRow(selection, "Take-home pay is lower by", _takeHome);
            _tips.SetToolTip(_takeHome, "Compared with the contribution on the latest payslip. A negative amount means take-home pay goes up.");
            Section(selection, "Employer match this paycheck");
            AddRow(selection, "Employer match, current", _currentMatch);
            AddRow(selection, "Employer match, selected", _selectedMatch);
            AddRow(selection, "Employee contribution increase", _employeeIncrease);
            AddRow(selection, "Employer contribution increase", _employerIncrease);
            _tips.SetToolTip(_currentMatch, "The saved match formula applied to this paycheck's current contribution. Catch-up is not listed separately on the payslip, so it is not in the current match.");
            _tips.SetToolTip(_selectedMatch, "The saved match formula applied to the selected percentages, including Roth catch-up.");
            _tips.SetToolTip(_employeeIncrease, "Selected employee deferral minus the deferral on the latest payslip. A negative amount is a decrease.");
            _tips.SetToolTip(_employerIncrease, "Selected employer match minus the match on the current contribution. A negative amount is a decrease. The match stops once the bands are full.");
            _compact = false;

            var pay = AddPage("Pay Details");
            AddNote(pay, "Birth year is saved on the person selected under Selection. Age at year end is December 31 of the tax year, so it moves forward when the tax year changes. Annual pay and prior-year wages come from that person's payslips and W-2s at the selected employer.");
            AddRow(pay, "Birth year", _birthYear);
            AddRow(pay, "Age at year end", _age);
            _tips.SetToolTip(_birthYear, "Saved with Save. Age at year end is the tax year minus this year.");
            _tips.SetToolTip(_age, "Age on December 31 of the tax year.");
            var saveBirthYear = new Button
            {
                Text = "Save",
                Name = "SaveBirthYear",
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 4)
            };
            saveBirthYear.Click += OnSaveBirthYear;
            AddSpan(pay, saveBirthYear);
            AddRow(pay, "Annual pay", _pay);
            AddRow(pay, "Prior-year wages from this employer", _priorWages);
            AddRow(pay, "Pay frequency", _frequency);
            AddRow(pay, "Federal rate saved on pre-tax (%)", _federal);
            AddRow(pay, "State rate saved on pre-tax (%)", _state);

            var matchPage = AddPage("Employer Match");
            AddNote(matchPage, "Save stores this formula for the person and employer selected under Selection. Two people at the same employer can have different formulas. A common formula is 100% of the first 6% of pay, then 50% of the next 4%. Leave a band at 0 when the plan does not use it.");
            AddRow(matchPage, "Match on the first band (%)", _firstRate);
            AddRow(matchPage, "First band, percent of pay", _firstOfPay);
            AddRow(matchPage, "Match on the next band (%)", _nextRate);
            AddRow(matchPage, "Next band, percent of pay", _nextOfPay);
            AddSpan(matchPage, _offersRoth);
            var saveMatch = new Button
            {
                Text = "Save",
                Name = "SaveMatch",
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 4)
            };
            saveMatch.Click += OnSaveMatch;
            AddSpan(matchPage, saveMatch);

            var legal = AddPage("Legal Limits");
            AddNote(legal, "Enter the IRS dollar limits for the tax year chosen on Selection. These limits are the same for every person and employer. Traditional and Roth elective deferrals share the employee limit. Catch-up is extra. It has to be Roth when prior-year wages from the selected employer are over the wage threshold. Age does not, by itself, force that Roth treatment. Save stores these amounts for that tax year.");
            AddRow(legal, "Employee limit, traditional plus Roth", _elective);
            AddRow(legal, "Catch-up, age 50 or older", _catchUp);
            AddRow(legal, "Higher catch-up, ages 60-63", _higherCatchUp);
            AddRow(legal, "Roth catch-up when prior-year wages exceed", _threshold);
            AddRow(legal, "Annual additions limit", _additions);
            AddRow(legal, "Pay counted by the match", _compensationCap);
            var save = new Button
            {
                Text = "Save",
                Name = "SaveLimits",
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 4)
            };
            save.Click += OnSaveLimits;
            AddSpan(legal, save);

            var close = new ThmsButton { Text = "Close", Name = "Close", DialogResult = DialogResult.Cancel };
            var closeBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(12, 8, 12, 12)
            };
            closeBar.Controls.Add(close);
            Controls.Add(_tabs);
            Controls.Add(closeBar);
            CancelButton = close;

            foreach (var input in Inputs())
                input.ValueChanged += OnInputChanged;
            _frequency.SelectedIndexChanged += OnInputChanged;
            _offersRoth.CheckedChanged += OnInputChanged;
            _person.SelectedIndexChanged += OnScopeChanged;
            _employer.SelectedIndexChanged += OnScopeChanged;
            _taxYear.ValueChanged += OnTaxYearChanged;
            _selectedPreTax.ValueChanged += OnSelectedChanged;
            _selectedRoth.ValueChanged += OnSelectedChanged;
            _selectedCatchUp.ValueChanged += OnSelectedChanged;

            FillPeople(null);
            _ready = true;
            LoadBirthYear();
            LoadMatch();
            ShowInitialLimits();
            FitWindow();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            FitWindow();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            FitWindow();
        }

        private void FitWindow()
        {
            var strip = _tabs.ItemSize.Width * Math.Max(_tabs.TabCount, 1) + 8;
            var width = Math.Max(strip, _fitted ? ClientSize.Width : Math.Max(1120, ClientSize.Width));
            var height = _fitted ? ClientSize.Height : Math.Max(1020, ClientSize.Height);
            _fitted = true;
            if (ClientSize.Width != width || ClientSize.Height != height)
                ClientSize = new Size(width, height);

            var frameWidth = Math.Max(0, Width - ClientSize.Width);
            var frameHeight = Math.Max(0, Height - ClientSize.Height);
            MinimumSize = new Size(strip + frameWidth, 800 + frameHeight);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || IsDisposed || _suspend || !_ready)
                return;
            _selectionEdited = false;
            FillPeople(SelectedPerson()?.Id);
            LoadBirthYear();
            LoadMatch();
            ShowInitialLimits();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _boldFont.Dispose();
                _tips.Dispose();
            }
        }

        private void OnSaveLimits(object? sender, EventArgs e)
        {
            try
            {
                _limits.Save(ReadLimits());
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnScopeChanged(object? sender, EventArgs e)
        {
            if (_suspend || !_ready)
                return;
            if (ReferenceEquals(sender, _person))
            {
                _suspend = true;
                try
                {
                    FillEmployers(keep: true);
                }
                finally
                {
                    _suspend = false;
                }
            }

            _selectionEdited = false;
            ClearPayFigures();
            LoadBirthYear();
            LoadMatch();
            ApplyIncome();
        }

        private void FillPeople(Guid? keepId)
        {
            var people = _people.GetAll();
            var selected = keepId ?? SelectedPerson()?.Id;
            _suspend = true;
            try
            {
                _person.Items.Clear();
                foreach (var person in people)
                    _person.Items.Add(person);
                var index = -1;
                if (selected is Guid id)
                {
                    for (var i = 0; i < _person.Items.Count; i++)
                    {
                        if (_person.Items[i] is HouseholdPerson person && person.Id == id)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                _person.SelectedIndex = index >= 0 ? index : _person.Items.Count > 0 ? 0 : -1;
                FillEmployers(keep: true);
            }
            finally
            {
                _suspend = false;
            }
        }

        private void FillEmployers(bool keep)
        {
            var keepName = keep ? SelectedEmployer() : null;
            var people = _people.GetAll();
            var person = SelectedPerson();
            var (slips, forms) = PersonIncome.ForSelection(
                _income.GetPayslips(),
                _income.GetFormW2s(),
                person?.Id,
                people.Count,
                null);
            var names = PersonIncome.Employers(forms, slips);
            _employer.Items.Clear();
            foreach (var name in names)
                _employer.Items.Add(name);
            if (person is null)
            {
                _employer.SelectedIndex = -1;
                return;
            }

            var index = -1;
            if (keepName is not null)
            {
                for (var i = 0; i < _employer.Items.Count; i++)
                {
                    if (string.Equals(_employer.Items[i]?.ToString(), keepName, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
            }

            _employer.SelectedIndex = index >= 0 ? index : _employer.Items.Count > 0 ? 0 : -1;
        }

        private void ClearPayFigures()
        {
            _suspend = true;
            try
            {
                SetValue(_pay, 0);
                SetValue(_priorWages, 0);
                _periodGross = null;
            }
            finally
            {
                _suspend = false;
            }
        }

        private HouseholdPerson? SelectedPerson() => _person.SelectedItem as HouseholdPerson;

        private string? SelectedEmployer() =>
            _employer.SelectedIndex < 0 ? null : _employer.SelectedItem?.ToString();

        private void OnSaveBirthYear(object? sender, EventArgs e)
        {
            try
            {
                var year = (int)_birthYear.Value;
                if (SelectedPerson() is { } person)
                {
                    _people.Save(new HouseholdPerson
                    {
                        Id = person.Id,
                        CreatedOn = person.CreatedOn,
                        Name = person.Name,
                        BirthYear = year
                    });
                    person.BirthYear = year;
                }
                else
                {
                    _birthYearStore.Save(new Retirement401kBirthYear { Year = year });
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadBirthYear()
        {
            var person = SelectedPerson();
            var people = _people.GetAll();
            int year;
            if (person is null)
                year = _birthYearStore.Get()?.Year ?? 0;
            else if (person.BirthYear >= 1900)
                year = person.BirthYear;
            else if (people.Count == 1)
                year = _birthYearStore.Get()?.Year ?? 0;
            else
                year = 0;

            _suspend = true;
            try
            {
                SetValue(_birthYear, year);
                ApplyAge();
            }
            finally
            {
                _suspend = false;
            }
        }

        private void ApplyAge()
        {
            var age = Retirement401kBirthYearOrchestrator.AgeAtYearEnd((int)_birthYear.Value, (int)_taxYear.Value);
            var suspended = _suspend;
            _suspend = true;
            try
            {
                SetValue(_age, age);
            }
            finally
            {
                _suspend = suspended;
            }
        }

        private void OnSaveMatch(object? sender, EventArgs e)
        {
            try
            {
                var person = SelectedPerson();
                var employer = SelectedEmployer();
                if (person is not null && employer is null)
                    throw new InvalidOperationException("Choose an employer on the Selection tab.");
                if (person is null || employer is null)
                    _match.Save(ReadMatch());
                else
                    _match.Save(person.Id, employer, ReadMatch());
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadMatch()
        {
            var person = SelectedPerson();
            var employer = SelectedEmployer();
            var saved = person is not null && employer is not null
                ? _match.Get(person.Id, employer) ?? new Retirement401kMatchFormula()
                : _match.Get() ?? new Retirement401kMatchFormula();
            _suspend = true;
            try
            {
                SetValue(_firstRate, saved.FirstMatchRate * 100m);
                SetValue(_firstOfPay, saved.FirstMatchOfPay * 100m);
                SetValue(_nextRate, saved.NextMatchRate * 100m);
                SetValue(_nextOfPay, saved.NextMatchOfPay * 100m);
                _offersRoth.Checked = saved.PlanOffersRoth;
            }
            finally
            {
                _suspend = false;
            }
        }

        private Retirement401kMatchFormula ReadMatch() => new()
        {
            FirstMatchRate = _firstRate.Value / 100m,
            FirstMatchOfPay = _firstOfPay.Value / 100m,
            NextMatchRate = _nextRate.Value / 100m,
            NextMatchOfPay = _nextOfPay.Value / 100m,
            PlanOffersRoth = _offersRoth.Checked
        };

        private void OnTaxYearChanged(object? sender, EventArgs e)
        {
            if (_suspend || !_ready)
                return;
            ShowYear((int)_taxYear.Value);
        }

        private void ShowInitialLimits()
        {
            ApplyLimitValues(_limits.GetLatest() ?? Retirement401kLimits.For2026());
        }

        private void ShowYear(int year)
        {
            var saved = _limits.Get(year);
            if (saved is not null)
            {
                ApplyLimitValues(saved);
                return;
            }

            if (_limits.GetLatest() is null && year == Retirement401kLimits.For2026().TaxYear)
            {
                ApplyLimitValues(Retirement401kLimits.For2026());
                return;
            }

            ApplyLimitValues(new Retirement401kLimits { TaxYear = year });
        }

        private void ApplyLimitValues(Retirement401kLimits limits)
        {
            _suspend = true;
            try
            {
                _taxYear.Value = Math.Clamp(limits.TaxYear, (int)_taxYear.Minimum, (int)_taxYear.Maximum);
                _elective.Value = limits.ElectiveDeferral;
                _catchUp.Value = limits.CatchUp;
                _higherCatchUp.Value = limits.HigherCatchUp;
                _threshold.Value = limits.RothCatchUpWageThreshold;
                _additions.Value = limits.AnnualAdditions;
                _compensationCap.Value = limits.CompensationCap;
                ApplyAge();
            }
            finally
            {
                _suspend = false;
            }

            ApplyIncome();
        }

        private Retirement401kLimits ReadLimits() => new()
        {
            TaxYear = (int)_taxYear.Value,
            ElectiveDeferral = _elective.Value,
            CatchUp = _catchUp.Value,
            HigherCatchUp = _higherCatchUp.Value,
            RothCatchUpWageThreshold = _threshold.Value,
            AnnualAdditions = _additions.Value,
            CompensationCap = _compensationCap.Value
        };

        private void OnInputChanged(object? sender, EventArgs e)
        {
            if (_suspend || !_ready)
                return;
            if (ReferenceEquals(sender, _birthYear))
                ApplyAge();
            if (ReferenceEquals(sender, _frequency))
                ApplyAnnualPay();
            else
                ShowImpact();
        }

        private void OnSelectedChanged(object? sender, EventArgs e)
        {
            if (_suspend || !_ready)
                return;
            _selectionEdited = true;
            ShowImpact();
        }

        private void ApplyIncome()
        {
            var figures = ReadIncome();
            _suspend = true;
            try
            {
                if (figures.AnnualPay is decimal annual)
                    SetValue(_pay, annual);
                if (figures.PriorYearWages is decimal wages)
                    SetValue(_priorWages, wages);
                _tips.SetToolTip(_pay, figures.AnnualPayDetail);
                _tips.SetToolTip(_priorWages, figures.PriorYearWagesDetail);
                _periodGross = figures.PeriodGross;
                SetValue(_currentPreTax, figures.CurrentPreTaxPercent ?? 0m);
                SetValue(_currentRoth, figures.CurrentRothPercent ?? 0m);
                _tips.SetToolTip(_currentPreTax, figures.CurrentContributionDetail);
                _tips.SetToolTip(_currentRoth, figures.CurrentContributionDetail);
                if (!_selectionEdited)
                {
                    SetValue(_selectedPreTax, _currentPreTax.Value);
                    SetValue(_selectedRoth, _currentRoth.Value);
                    SetValue(_selectedCatchUp, 0m);
                }
            }
            finally
            {
                _suspend = false;
            }

            ShowImpact();
        }

        private void ApplyAnnualPay()
        {
            var figures = ReadIncome();
            _suspend = true;
            try
            {
                if (figures.AnnualPay is decimal annual)
                    SetValue(_pay, annual);
                _tips.SetToolTip(_pay, figures.AnnualPayDetail);
                _periodGross = figures.PeriodGross;
            }
            finally
            {
                _suspend = false;
            }

            ShowImpact();
        }

        private void ShowImpact()
        {
            var quote = Retirement401kCalculator.QuoteSelection(
                BuildRequest(),
                PeriodGross(),
                _currentPreTax.Value,
                _currentRoth.Value,
                _selectedPreTax.Value,
                _selectedRoth.Value,
                _selectedCatchUp.Value);
            _suspend = true;
            try
            {
                SetValue(_maxRegular, quote.RegularMaxPercent);
                SetValue(_maxCatchUp, quote.CatchUpMaxPercent);
                SetValue(_preTaxWithheld, quote.PreTaxWithheld);
                SetValue(_rothWithheld, quote.RothWithheld);
                SetValue(_federalSaved, quote.FederalTaxSaved);
                SetValue(_stateSaved, quote.StateTaxSaved);
                SetValue(_takeHome, quote.TakeHomeLowerThanCurrent);
                SetValue(_currentMatch, quote.CurrentEmployerMatch);
                SetValue(_selectedMatch, quote.SelectedEmployerMatch);
                SetValue(_employeeIncrease, quote.EmployeeContributionIncrease);
                SetValue(_employerIncrease, quote.EmployerContributionIncrease);
            }
            finally
            {
                _suspend = false;
            }

            _notice.Text = Notice(quote);
        }

        private static string Notice(Retirement401kSelectionQuote quote)
        {
            var lines = new List<string>();
            if (quote.CatchUpMustBeRoth)
                lines.Add("Catch-up has to be Roth because prior-year wages from this employer exceed the threshold. The regular deferral can still be pre-tax.");
            if (quote.RegularExceedsMaximum)
                lines.Add($"Pre-tax plus Roth is above the {quote.RegularMaxPercent:0.00}% maximum for regular deferrals.");
            if (quote.CatchUpExceedsMaximum)
                lines.Add($"Roth catch-up is above the {quote.CatchUpMaxPercent:0.00}% maximum.");
            if (quote.RothSelectedWithoutARothAccount)
                lines.Add("This plan has no Roth account, so those Roth deferrals cannot be made.");
            return string.Join(Environment.NewLine, lines);
        }

        private Retirement401kIncomeFigures ReadIncome()
        {
            var people = _people.GetAll();
            var person = SelectedPerson();
            var (slips, forms) = PersonIncome.ForSelection(
                _income.GetPayslips(),
                _income.GetFormW2s(),
                person?.Id,
                people.Count,
                person is null ? null : SelectedEmployer());
            return Retirement401kIncome.Estimate(slips, forms, Frequency(), (int)_taxYear.Value);
        }

        private decimal PeriodGross()
        {
            if (_periodGross is decimal gross && gross > 0m)
                return gross;
            var periods = Retirement401kCalculator.PeriodsPerYear(Frequency());
            return periods > 0 ? _pay.Value / periods : 0m;
        }

        private Retirement401kRequest BuildRequest()
        {
            return new Retirement401kRequest
            {
                AgeAtYearEnd = (int)_age.Value,
                AnnualPay = _pay.Value,
                PriorYearWages = _priorWages.Value,
                PayFrequency = Frequency(),
                PlanOffersRoth = _offersRoth.Checked,
                FederalMarginalRate = _federal.Value / 100m,
                StateMarginalRate = _state.Value / 100m,
                FirstMatchRate = _firstRate.Value / 100m,
                FirstMatchOfPay = _firstOfPay.Value / 100m,
                NextMatchRate = _nextRate.Value / 100m,
                NextMatchOfPay = _nextOfPay.Value / 100m,
                Limits = new Retirement401kLimits
                {
                    TaxYear = (int)_taxYear.Value,
                    ElectiveDeferral = _elective.Value,
                    CatchUp = _catchUp.Value,
                    HigherCatchUp = _higherCatchUp.Value,
                    RothCatchUpWageThreshold = _threshold.Value,
                    AnnualAdditions = _additions.Value,
                    CompensationCap = _compensationCap.Value
                }
            };
        }

        private PayFrequency Frequency() =>
            _frequency.SelectedItem is PayChoice choice ? choice.Frequency : PayFrequency.Biweekly;

        private IEnumerable<NumericUpDown> Inputs()
        {
            yield return _birthYear;
            yield return _pay;
            yield return _priorWages;
            yield return _federal;
            yield return _state;
            yield return _firstRate;
            yield return _firstOfPay;
            yield return _nextRate;
            yield return _nextOfPay;
            yield return _elective;
            yield return _catchUp;
            yield return _higherCatchUp;
            yield return _threshold;
            yield return _additions;
            yield return _compensationCap;
        }

        private TableLayoutPanel AddPage(string title)
        {
            var page = new TabPage(title) { Name = title.Replace(" ", "") };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16, 12, 16, 8) };
            var table = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            scroll.Controls.Add(table);
            page.Controls.Add(scroll);
            _tabs.TabPages.Add(page);
            return table;
        }

        private void Section(TableLayoutPanel table, string text)
        {
            AddSpan(table, new Label
            {
                Text = text,
                AutoSize = true,
                Font = _boldFont,
                Margin = new Padding(0, table.RowCount == 0 ? 0 : _compact ? 6 : 14, 0, _compact ? 1 : 6)
            });
        }

        private void AddNote(TableLayoutPanel table, string text)
        {
            AddSpan(table, new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(520, 0),
                ForeColor = Color.FromArgb(80, 80, 80),
                Margin = new Padding(0, 0, 0, 8)
            });
        }

        private void AddRow(TableLayoutPanel table, string label, Control field)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var row = table.RowCount++;
            table.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                MaximumSize = new Size(460, 0),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, _compact ? 2 : 8, 12, _compact ? 2 : 8)
            }, 0, row);
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(0, _compact ? 1 : 4, 0, _compact ? 1 : 4);
            if (field.MinimumSize.Height < 28)
                field.MinimumSize = new Size(field.MinimumSize.Width, 28);
            table.Controls.Add(field, 1, row);
        }

        private static void AddSpan(TableLayoutPanel table, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var row = table.RowCount++;
            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 2);
        }

        private static void SetValue(NumericUpDown input, decimal amount)
        {
            if (amount < input.Minimum)
                amount = input.Minimum;
            if (amount > input.Maximum)
                amount = input.Maximum;
            input.Value = amount;
        }

        private static NumericUpDown YearInput(int year) => new()
        {
            Name = "TaxYear",
            DecimalPlaces = 0,
            Minimum = 1950,
            Maximum = 2100,
            Increment = 1,
            Value = Math.Clamp(year, 1950, 2100),
            MinimumSize = new Size(120, 28),
            TextAlign = HorizontalAlignment.Right
        };

        private static NumericUpDown Whole(decimal value, decimal maximum, string name) => new()
        {
            Name = name,
            DecimalPlaces = 0,
            Minimum = 0,
            Maximum = maximum,
            Increment = 1,
            Value = value,
            MinimumSize = new Size(120, 28),
            TextAlign = HorizontalAlignment.Right
        };

        private static NumericUpDown MoneyInput(decimal value, string name) => new()
        {
            Name = name,
            DecimalPlaces = 2,
            Minimum = 0,
            Maximum = 10_000_000m,
            Increment = 1000m,
            ThousandsSeparator = true,
            Value = value,
            MinimumSize = new Size(120, 28),
            TextAlign = HorizontalAlignment.Right
        };

        private static NumericUpDown PercentInput(decimal value, string name, decimal maximum = 100m) => new()
        {
            Name = name,
            DecimalPlaces = 2,
            Minimum = 0,
            Maximum = maximum,
            Increment = 1m,
            Value = value,
            MinimumSize = new Size(120, 28),
            TextAlign = HorizontalAlignment.Right
        };

        private static NumericUpDown FinePercent(string name)
        {
            var input = PercentInput(0m, name);
            input.Increment = 0.01m;
            return input;
        }

        private static NumericUpDown ReadOnlyPercent(string name)
        {
            var input = FinePercent(name);
            input.ReadOnly = true;
            input.TabStop = false;
            input.Increment = 0;
            return input;
        }

        private static NumericUpDown ReadOnlyWhole(string name)
        {
            var input = Whole(0, 200, name);
            input.ReadOnly = true;
            input.TabStop = false;
            input.Increment = 0;
            return input;
        }

        private static NumericUpDown ReadOnlyMoney(string name)
        {
            var input = MoneyInput(0m, name);
            input.ReadOnly = true;
            input.TabStop = false;
            input.Increment = 0;
            return input;
        }

        private static NumericUpDown ReadOnlySignedMoney(string name)
        {
            var input = ReadOnlyMoney(name);
            input.Minimum = -10_000_000m;
            return input;
        }

        private sealed record PayChoice(PayFrequency Frequency, string Label)
        {
            public override string ToString() => Label;
        }
    }
}
