using THMS.Domain.Finance.Income;
using THMS.Domain.Household;
using THMS.Logic.Finance.Income;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Income;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public class IncomeForm : BaseEmbeddedForm
    {
        private readonly IncomeOrchestrator _income;
        private readonly HouseholdPersonOrchestrator _people;
        private readonly ComboBox _person = new() { DropDownStyle = ComboBoxStyle.DropDownList, Name = "Person", Width = 280 };
        private readonly ThmsTabControl _tabs = new() { Dock = DockStyle.Fill, Name = "IncomeTabs", MaxTabWidth = 280 };
        private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, Name = "IncomeSplit" };
        private readonly DataGridView _years = Grid("Years");
        private readonly DataGridView _payslips = Grid("Payslips");
        private readonly BindingSource _yearRows = new();
        private readonly BindingSource _payslipRows = new();
        private readonly Label _yearNote = new()
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 36,
            Padding = new Padding(8, 6, 8, 2),
            Text = "A year before its W-2 uses the latest payslip's year-to-date amounts. Wages and Social Security wages stay blank."
        };
        private readonly Label _payslipHeading = new()
        {
            Name = "PayslipsHeading",
            Text = "Payslips",
            Dock = DockStyle.Top,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };
        private readonly Panel _yearButtons = new() { Dock = DockStyle.Bottom, BackColor = Color.White };
        private readonly Panel _payslipButtons = new() { Dock = DockStyle.Bottom, BackColor = Color.White };
        private const int SplitterWidthDip = 12;
        private int? _splitterDistance;
        private Guid? _personKeep;
        private bool _holdingSplitter;
        private bool _suspend;

        public IncomeForm()
            : this(new IncomeOrchestrator(), new HouseholdPersonOrchestrator())
        {
        }

        internal IncomeForm(IncomeOrchestrator income)
            : this(income, new HouseholdPersonOrchestrator())
        {
        }

        internal IncomeForm(IncomeOrchestrator income, HouseholdPersonOrchestrator people)
        {
            _income = income;
            _people = people;
            Text = "Income";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;

            _years.DataSource = _yearRows;
            _years.Columns.AddRange(
                TextColumn(nameof(IncomeYearRow.Year), "Year"),
                TextColumn(nameof(IncomeYearRow.Record), "Record"),
                MoneyText(nameof(IncomeYearRow.WagesText), "Wages"),
                MoneyText(nameof(IncomeYearRow.SocialSecurityWagesText), "SS wages"),
                MoneyText(nameof(IncomeYearRow.Traditional401kText), "Traditional 401(k)"),
                MoneyText(nameof(IncomeYearRow.Roth401kText), "Roth 401(k)"),
                MoneyText(nameof(IncomeYearRow.HsaText), "HSA"),
                MoneyText(nameof(IncomeYearRow.GrossSoFarText), "Gross so far"));
            _years.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    EditYear();
            };
            _years.SelectionChanged += (_, _) =>
            {
                if (!_suspend)
                    ShowPayslips();
            };
            UseMenu(
                _years,
                () => SelectedYear() is not null,
                () => SelectedYear()?.FormId is not null,
                ("Edit", EditYear),
                ("Delete", DeleteYear));

            _payslips.DataSource = _payslipRows;
            _payslips.Columns.AddRange(
                Date(nameof(PayslipListRow.PayDate), "Pay date"),
                Money(nameof(PayslipListRow.Gross), "Gross"),
                Money(nameof(PayslipListRow.GrossYearToDate), "Gross YTD"),
                Money(nameof(PayslipListRow.Traditional401kYearToDate), "Traditional 401(k) YTD"),
                Money(nameof(PayslipListRow.Roth401kYearToDate), "Roth 401(k) YTD"),
                Money(nameof(PayslipListRow.HsaYearToDate), "HSA YTD"));
            _payslips.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    EditPayslip(SelectedPayslip());
            };
            UseMenu(
                _payslips,
                () => SelectedPayslip() is not null,
                () => SelectedPayslip() is not null,
                ("Edit", () => EditPayslip(SelectedPayslip())),
                ("Delete", DeletePayslip));

            var addForm = new ThmsButton { Text = "Add W-2", Name = "AddFormW2", Margin = Padding.Empty };
            addForm.Click += (_, _) => EditForm(null, SelectedYear() is { IsPlaceholder: true } row ? row.Year : null);
            var addPayslip = new ThmsButton { Text = "Add payslip", Name = "AddPayslip", Margin = Padding.Empty };
            addPayslip.Click += (_, _) => EditPayslip(null);
            _yearButtons.Controls.Add(addForm);
            _payslipButtons.Controls.Add(addPayslip);
            _payslipHeading.Font = new Font(Font.FontFamily, Font.Size + 2f, FontStyle.Bold);

            var yearsHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _years.Dock = DockStyle.Fill;
            yearsHost.Controls.Add(_years);
            yearsHost.Controls.Add(_yearButtons);
            yearsHost.Controls.Add(_yearNote);
            var paysHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _payslips.Dock = DockStyle.Fill;
            paysHost.Controls.Add(_payslips);
            paysHost.Controls.Add(_payslipButtons);
            paysHost.Controls.Add(_payslipHeading);
            _split.Panel1.Controls.Add(yearsHost);
            _split.Panel2.Controls.Add(paysHost);
            _split.Panel1MinSize = 80;
            _split.Panel2MinSize = 80;
            _split.SplitterMoved += (_, _) => RememberSplitter();
            _split.SizeChanged += (_, _) => OnSplitSized();
            FitChrome();

            _tabs.SelectedIndexChanged += (_, _) =>
            {
                if (_suspend || _holdingSplitter)
                    return;
                RememberSplitter();
                HostSplit();
                ShowYears(null);
                RestoreSplitter();
            };

            var addPerson = new ThmsButton { Text = "Add person", Name = "AddPerson", Margin = new Padding(0, 2, 0, 0) };
            addPerson.Click += (_, _) => AddPerson();
            _person.Margin = new Padding(0, 4, 12, 0);
            _person.DropDownWidth = 360;
            _person.SelectedIndexChanged += (_, _) =>
            {
                if (!_suspend)
                    Reload();
            };
            var personBar = new FlowLayoutPanel
            {
                Name = "PersonBar",
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(8, 8, 8, 4),
                BackColor = Color.White
            };
            personBar.Controls.Add(new Label
            {
                Text = "Person",
                AutoSize = true,
                Margin = new Padding(0, 8, 8, 0),
                UseMnemonic = false
            });
            personBar.Controls.Add(_person);
            personBar.Controls.Add(addPerson);

            // Parent the split before the handle exists so its heading and buttons scale with the form.
            // The person bar is added last so it docks along the top and the tabs fill what remains.
            Controls.Add(_split);
            Controls.Add(_tabs);
            Controls.Add(personBar);
            Load += (_, _) => Reload();
        }

        protected override void OnLoad(EventArgs e)
        {
            FitChrome();
            base.OnLoad(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            FitChrome();
            RestoreSplitter();
        }

        private void FitChrome()
        {
            var pad = LogicalToDeviceUnits(8);
            _payslipHeading.Padding = new Padding(pad, LogicalToDeviceUnits(6), pad, LogicalToDeviceUnits(6));
            var text = TextRenderer.MeasureText(_payslipHeading.Text, _payslipHeading.Font);
            _payslipHeading.Height = text.Height + _payslipHeading.Padding.Vertical;
            FitButtonBar(_yearButtons);
            FitButtonBar(_payslipButtons);
        }

        private void FitButtonBar(Panel bar)
        {
            var pad = LogicalToDeviceUnits(8);
            bar.Padding = new Padding(pad);
            if (bar.Controls.Count == 0)
                return;
            var button = bar.Controls[0];
            button.Location = new Point(pad, pad);
            bar.Height = button.GetPreferredSize(Size.Empty).Height + bar.Padding.Vertical;
        }

        private void OnSplitSized()
        {
            if (_holdingSplitter)
                return;
            SplitContainerUtil.MakeSplitterVisible(_split, SplitterWidthDip);
            RestoreSplitter();
        }

        private void RememberSplitter()
        {
            if (_holdingSplitter || _split.IsDisposed)
                return;
            var max = _split.Height - _split.Panel2MinSize - _split.SplitterWidth;
            if (max < _split.Panel1MinSize)
                return;
            if (_split.SplitterDistance < _split.Panel1MinSize || _split.SplitterDistance > max)
                return;
            _splitterDistance = _split.SplitterDistance;
        }

        private void RestoreSplitter()
        {
            if (_split.IsDisposed)
                return;
            var max = _split.Height - _split.Panel2MinSize - _split.SplitterWidth;
            if (max < _split.Panel1MinSize)
                return;
            var distance = _splitterDistance ?? Math.Max(_split.Panel1MinSize, _split.Height / 3);
            distance = Math.Clamp(distance, _split.Panel1MinSize, max);
            if (_split.SplitterDistance == distance)
                return;
            _holdingSplitter = true;
            try
            {
                _split.SplitterDistance = distance;
            }
            catch (InvalidOperationException)
            {
                // The split is mid-layout. The next size change applies the saved distance.
            }
            finally
            {
                _holdingSplitter = false;
            }
        }

        internal void Reload(string? employerKey = null, int? year = null, Guid? payslipId = null)
        {
            RememberSplitter();
            employerKey ??= SelectedPage()?.Key;
            year ??= SelectedYear()?.Year;
            payslipId ??= SelectedPayslip()?.Id;
            FillPersonChoices();
            var (slips, forms) = VisibleDocuments();
            var pages = IncomeEmployerTabs.Build(forms, slips);

            _suspend = true;
            _holdingSplitter = true;
            try
            {
                _split.Parent = null;
                _tabs.TabPages.Clear();
                foreach (var page in pages)
                {
                    _tabs.TabPages.Add(new TabPage(page.Caption)
                    {
                        Name = "EmployerTab",
                        Tag = page,
                        BackColor = ThmsTabControl.PageTint,
                        UseVisualStyleBackColor = false
                    });
                }

                _tabs.Visible = pages.Count > 0;
                if (pages.Count == 0)
                {
                    Controls.Add(_split);
                    _split.Dock = DockStyle.Fill;
                }
                else
                {
                    var index = 0;
                    if (employerKey is not null)
                    {
                        for (var i = 0; i < _tabs.TabPages.Count; i++)
                        {
                            if (_tabs.TabPages[i].Tag is IncomeEmployerTab tab
                                && string.Equals(tab.Key, employerKey, StringComparison.OrdinalIgnoreCase))
                            {
                                index = i;
                                break;
                            }
                        }
                    }

                    _tabs.SelectedIndex = index;
                }
            }
            finally
            {
                _holdingSplitter = false;
                _suspend = false;
            }

            if (pages.Count == 0)
            {
                SplitContainerUtil.MakeSplitterVisible(_split, SplitterWidthDip);
                RestoreSplitter();
                return;
            }

            HostSplit();
            ShowYears(year);
            ShowPayslips(payslipId);
            RestoreSplitter();
        }

        private void HostSplit()
        {
            if (_tabs.SelectedTab is null)
                return;
            _tabs.SelectedTab.Controls.Add(_split);
            _split.Dock = DockStyle.Fill;
            SplitContainerUtil.MakeSplitterVisible(_split, SplitterWidthDip);
        }

        private void ShowYears(int? year)
        {
            var page = SelectedPage();
            _suspend = true;
            try
            {
                _yearRows.DataSource = page?.Years.ToList() ?? [];
            }
            finally
            {
                _suspend = false;
            }

            if (year is int keep)
            {
                foreach (DataGridViewRow row in _years.Rows)
                {
                    if (row.DataBoundItem is not IncomeYearRow item || item.Year != keep)
                        continue;
                    row.Selected = true;
                    _years.CurrentCell = row.Cells[0];
                    break;
                }
            }

            ShowPayslips();
        }

        private void ShowPayslips(Guid? keepId = null)
        {
            var page = SelectedPage();
            var year = SelectedYear()?.Year;
            var rows = _income.GetPayslips()
                .Where(slip => page is not null && page.PayslipIds.Contains(slip.Id) && slip.PayDate.Year == year)
                .OrderByDescending(slip => slip.PayDate)
                .ThenByDescending(slip => slip.CreatedOn)
                .Select(slip => new PayslipListRow
                {
                    Id = slip.Id,
                    PayDate = slip.PayDate,
                    Gross = slip.Gross,
                    GrossYearToDate = slip.GrossYearToDate,
                    Traditional401kYearToDate = slip.Traditional401kYearToDate,
                    Roth401kYearToDate = slip.Roth401kYearToDate,
                    HsaYearToDate = slip.HsaYearToDate
                })
                .ToList();
            _payslipRows.DataSource = rows;
            if (keepId is null)
                return;
            foreach (DataGridViewRow row in _payslips.Rows)
            {
                if (row.DataBoundItem is not PayslipListRow item || item.Id != keepId)
                    continue;
                row.Selected = true;
                _payslips.CurrentCell = row.Cells[0];
                return;
            }
        }

        private void EditPayslip(PayslipListRow? row)
        {
            Payslip? existing = row is null ? null : _income.GetPayslip(row.Id);
            if (row is not null && existing is null)
                return;

            var employer = existing is null || string.IsNullOrWhiteSpace(existing.Employer)
                ? PrefillEmployer()
                : null;
            using var dialog = new PayslipDialog(_income, existing, employer, _people.GetAll(), SelectedPersonId(), _people);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var saved = _income.GetPayslip(dialog.PayslipId);
                if (saved?.PersonId is Guid personId && personId != Guid.Empty)
                    _personKeep = personId;
                Reload(saved?.Employer, saved?.PayDate.Year, dialog.PayslipId);
            }
        }

        private void EditYear()
        {
            var row = SelectedYear();
            if (row is null)
                return;
            if (row.FormId is null)
            {
                EditForm(null, row.Year);
                return;
            }

            EditForm(_income.GetFormW2(row.FormId.Value), null);
        }

        private void EditForm(FormW2? existing, int? taxYear)
        {
            if (existing is null && taxYear is null && SelectedYear() is { IsPlaceholder: true } placeholder)
                taxYear = placeholder.Year;
            using var dialog = new FormW2Dialog(
                _income,
                existing,
                taxYear,
                existing is null ? PrefillEmployer() : null,
                _people.GetAll(),
                SelectedPersonId(),
                _people);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var saved = _income.GetFormW2(dialog.FormId);
                if (saved?.PersonId is Guid personId && personId != Guid.Empty)
                    _personKeep = personId;
                Reload(saved?.Employer, saved?.TaxYear);
            }
        }

        private void AddPerson()
        {
            using var dialog = new HouseholdPersonDialog(_people, null);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            _personKeep = dialog.PersonId;
            Reload();
        }

        private void FillPersonChoices()
        {
            var people = _people.GetAll();
            var keep = _personKeep ?? SelectedPersonId();
            var keepUnassigned = _personKeep is null && _person.SelectedItem is PersonChoice { Id: null };
            _personKeep = null;
            var unassigned = _income.GetPayslips().Any(slip => slip.PersonId == Guid.Empty)
                || _income.GetFormW2s().Any(form => form.PersonId == Guid.Empty);
            _suspend = true;
            try
            {
                _person.Items.Clear();
                _person.Enabled = people.Count > 0;
                foreach (var person in people)
                    _person.Items.Add(new PersonChoice(person.Id, person.Name));
                if (people.Count > 1 && unassigned)
                    _person.Items.Add(PersonChoice.Unassigned);

                var index = -1;
                if (keep is Guid id)
                {
                    for (var i = 0; i < _person.Items.Count; i++)
                    {
                        if (_person.Items[i] is PersonChoice choice && choice.Id == id)
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else if (keepUnassigned)
                {
                    for (var i = 0; i < _person.Items.Count; i++)
                    {
                        if (_person.Items[i] is PersonChoice { Id: null })
                        {
                            index = i;
                            break;
                        }
                    }
                }

                _person.SelectedIndex = index >= 0 ? index : _person.Items.Count > 0 ? 0 : -1;
            }
            finally
            {
                _suspend = false;
            }
        }

        private (IReadOnlyList<Payslip> Payslips, IReadOnlyList<FormW2> Forms) VisibleDocuments()
        {
            var slips = _income.GetPayslips();
            var forms = _income.GetFormW2s();
            var people = _people.GetAll();
            if (people.Count == 0)
                return (slips, forms);
            if (SelectedPersonId() is Guid id)
                return PersonIncome.ForSelection(slips, forms, id, people.Count, null);
            return (
                slips.Where(slip => slip.PersonId == Guid.Empty).ToList(),
                forms.Where(form => form.PersonId == Guid.Empty).ToList());
        }

        private Guid? SelectedPersonId() => (_person.SelectedItem as PersonChoice)?.Id;

        private sealed class PersonChoice(Guid? id, string caption)
        {
            public static PersonChoice Unassigned { get; } = new(null, "No person");
            public Guid? Id { get; } = id;
            public override string ToString() => caption;
        }

        private void DeletePayslip()
        {
            var row = SelectedPayslip();
            if (row is null)
                return;
            if (MessageBox.Show(
                    this,
                    $"Delete the payslip from {row.PayDate:d}?",
                    "Delete Payslip",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            _income.Delete(row.Id);
            Reload();
        }

        private void DeleteYear()
        {
            var row = SelectedYear();
            if (row?.FormId is not Guid id)
                return;
            if (MessageBox.Show(
                    this,
                    $"Delete the {row.Year} W-2?",
                    "Delete W-2",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            _income.DeleteFormW2(id);
            Reload();
        }

        private string? PrefillEmployer()
        {
            var caption = SelectedPage()?.Caption;
            return caption is null || caption == IncomeEmployerTabs.UnassignedCaption ? null : caption;
        }

        private IncomeEmployerTab? SelectedPage() => _tabs.SelectedTab?.Tag as IncomeEmployerTab;

        private IncomeYearRow? SelectedYear() => _years.CurrentRow?.DataBoundItem as IncomeYearRow;

        private PayslipListRow? SelectedPayslip() => _payslips.CurrentRow?.DataBoundItem as PayslipListRow;

        private static void UseMenu(
            DataGridView grid,
            Func<bool> hasRow,
            Func<bool> canDelete,
            params (string Text, Action Command)[] commands)
        {
            var menu = new ContextMenuStrip();
            foreach (var command in commands)
            {
                var item = new ToolStripMenuItem(command.Text);
                var action = command.Command;
                item.Click += (_, _) => action();
                menu.Items.Add(item);
            }

            menu.Opening += (_, _) =>
            {
                foreach (ToolStripItem item in menu.Items)
                    item.Enabled = item.Text == "Delete" ? canDelete() : hasRow();
            };
            grid.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Right)
                    return;
                var hit = grid.HitTest(e.X, e.Y);
                if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
                    return;
                grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
            };
            grid.ContextMenuStrip = menu;
        }

        private static DataGridView Grid(string name)
        {
            var grid = new DataGridView
            {
                Name = name,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            return grid;
        }

        private static DataGridViewTextBoxColumn Money(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn MoneyText(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn Date(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "d";
            return column;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) => new()
        {
            DataPropertyName = property,
            HeaderText = header,
            Name = property,
            ReadOnly = true
        };
    }
}
