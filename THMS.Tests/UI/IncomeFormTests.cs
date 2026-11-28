using THMS.Data.Stores;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Income;
using THMS.UI.WinForms;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class IncomeFormTests
    {
        private static readonly Guid PersonId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

        [Test]
        [Apartment(ApartmentState.STA)]
        public void List_ShowsPayslipsNewestFirst()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            income.Save(new THMS.Domain.Finance.Income.Payslip
            {
                PersonId = PersonId,
                Employer = "Northwind",
                PayDate = new DateTime(2026, 1, 15),
                Gross = 2000m,
                GrossYearToDate = 2000m,
                Traditional401kYearToDate = 100m
            });
            income.Save(new THMS.Domain.Finance.Income.Payslip
            {
                PersonId = PersonId,
                Employer = "Northwind",
                PayDate = new DateTime(2026, 1, 31),
                Gross = 2100m,
                GrossYearToDate = 4100m,
                Traditional401kYearToDate = 250m
            });

            using var form = new IncomeForm(income);
            form.Show();

            var grid = (DataGridView)form.Controls.Find("Payslips", true).Single();
            var rows = grid.Rows.Cast<DataGridViewRow>().Select(row => (PayslipListRow)row.DataBoundItem!).ToList();

            Assert.That(rows.Select(row => row.PayDate), Is.EqualTo(new[]
            {
                new DateTime(2026, 1, 31),
                new DateTime(2026, 1, 15)
            }));
            Assert.That(rows[0].Traditional401kYearToDate, Is.EqualTo(250m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_SavesThePayslipAmounts()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            var person = new THMS.Domain.Household.HouseholdPerson { Name = "Alex" };
            using var dialog = new PayslipDialog(income, null, null, [person]);
            dialog.Show();
            var traditional = All(dialog).OfType<Label>().Single(label => label.Text == "Traditional 401(k)");
            var needed = TextRenderer.MeasureText(traditional.Text, traditional.Font);
            Assert.That(traditional.ClientSize.Width, Is.GreaterThanOrEqualTo(needed.Width - 2));
            var yearToDate = All(dialog).OfType<Label>().Single(label => label.Text == "Year to date");
            var headingNeeded = TextRenderer.MeasureText(yearToDate.Text, yearToDate.Font);
            Assert.That(yearToDate.ClientSize.Height - yearToDate.Padding.Vertical, Is.GreaterThanOrEqualTo(headingNeeded.Height));
            var help = (Label)dialog.Controls.Find("YearToDateHelp", true).Single();
            Assert.That(help.Text, Is.EqualTo("?"));
            Assert.That(dialog.Tips.GetToolTip(help), Is.EqualTo("Year-to-date amounts include this paycheck."));
            Assert.That(All(dialog).OfType<Label>().Any(label => label.Text.StartsWith("Year-to-date amounts")), Is.False);
            var order = new[]
            {
                "Gross pay", "Federal income tax", "Social Security tax", "Medicare tax",
                "Traditional 401(k)", "Roth 401(k)", "GTLI taxable", "HSA"
            };
            var tops = order.Select(text => All(dialog).OfType<Label>().Single(label => label.Text == text).PointToScreen(Point.Empty).Y).ToArray();
            Assert.That(tops, Is.EqualTo(tops.OrderBy(top => top).ToArray()));
            ((TextBox)dialog.Controls.Find("Employer", true).Single()).Text = "Contoso";
            ((DateTimePicker)dialog.Controls.Find("PayDate", true).Single()).Value = new DateTime(2026, 4, 10);
            ((NumericUpDown)dialog.Controls.Find("Gross", true).Single()).Value = 3000m;
            ((NumericUpDown)dialog.Controls.Find("GrossYearToDate", true).Single()).Value = 9000m;
            ((NumericUpDown)dialog.Controls.Find("Traditional401k", true).Single()).Value = 400m;
            ((NumericUpDown)dialog.Controls.Find("Traditional401kYearToDate", true).Single()).Value = 1200m;
            ((NumericUpDown)dialog.Controls.Find("Roth401k", true).Single()).Value = 100m;
            ((NumericUpDown)dialog.Controls.Find("Roth401kYearToDate", true).Single()).Value = 300m;
            ((NumericUpDown)dialog.Controls.Find("SocialSecurityTax", true).Single()).Value = 180m;
            ((NumericUpDown)dialog.Controls.Find("SocialSecurityTaxYearToDate", true).Single()).Value = 540m;
            ((NumericUpDown)dialog.Controls.Find("MedicareTax", true).Single()).Value = 42m;
            ((NumericUpDown)dialog.Controls.Find("MedicareTaxYearToDate", true).Single()).Value = 126m;
            ((NumericUpDown)dialog.Controls.Find("FederalIncomeTax", true).Single()).Value = 450m;
            ((NumericUpDown)dialog.Controls.Find("FederalIncomeTaxYearToDate", true).Single()).Value = 1350m;
            ((NumericUpDown)dialog.Controls.Find("GroupTermLifeTaxable", true).Single()).Value = 18m;
            ((NumericUpDown)dialog.Controls.Find("GroupTermLifeTaxableYearToDate", true).Single()).Value = 54m;
            ((NumericUpDown)dialog.Controls.Find("Hsa", true).Single()).Value = 120m;
            ((NumericUpDown)dialog.Controls.Find("HsaYearToDate", true).Single()).Value = 360m;

            var addPerson = dialog.Controls.Find("AddPerson", true).Single();
            Assert.That(addPerson, Is.InstanceOf<ThmsButton>());
            Assert.That(addPerson.Text, Is.EqualTo("Add person"));
            AssertFullyVisible(addPerson);
            AssertFullyVisible(dialog.Controls.Find("Person", true).Single());
            ((Button)dialog.Controls.Find("Save", true).Single()).PerformClick();

            var saved = income.GetPayslip(dialog.PayslipId);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.PersonId, Is.EqualTo(person.Id));
            Assert.That(saved.Employer, Is.EqualTo("Contoso"));
            Assert.That(saved.PayDate, Is.EqualTo(new DateTime(2026, 4, 10)));
            Assert.That(saved.Gross, Is.EqualTo(3000m));
            Assert.That(saved.GrossYearToDate, Is.EqualTo(9000m));
            Assert.That(saved.Traditional401k, Is.EqualTo(400m));
            Assert.That(saved.Traditional401kYearToDate, Is.EqualTo(1200m));
            Assert.That(saved.Roth401k, Is.EqualTo(100m));
            Assert.That(saved.Roth401kYearToDate, Is.EqualTo(300m));
            Assert.That(saved.SocialSecurityTax, Is.EqualTo(180m));
            Assert.That(saved.SocialSecurityTaxYearToDate, Is.EqualTo(540m));
            Assert.That(saved.MedicareTax, Is.EqualTo(42m));
            Assert.That(saved.MedicareTaxYearToDate, Is.EqualTo(126m));
            Assert.That(saved.FederalIncomeTax, Is.EqualTo(450m));
            Assert.That(saved.FederalIncomeTaxYearToDate, Is.EqualTo(1350m));
            Assert.That(saved.GroupTermLifeTaxable, Is.EqualTo(18m));
            Assert.That(saved.GroupTermLifeTaxableYearToDate, Is.EqualTo(54m));
            Assert.That(saved.Hsa, Is.EqualTo(120m));
            Assert.That(saved.HsaYearToDate, Is.EqualTo(360m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void BothLists_AddFromAButton_AndDeleteFromTheMenu()
        {
            using var form = new IncomeForm(new IncomeOrchestrator(new InMemoryIncomeDataStore()));
            form.Show();

            var payslips = (DataGridView)form.Controls.Find("Payslips", true).Single();
            var years = (DataGridView)form.Controls.Find("Years", true).Single();

            Assert.That(form.Controls.Find("AddPayslip", true).OfType<Button>().Single().Text, Is.EqualTo("Add payslip"));
            Assert.That(form.Controls.Find("AddFormW2", true).OfType<Button>().Single().Text, Is.EqualTo("Add W-2"));
            var addW2 = form.Controls.Find("AddFormW2", true).Single();
            var addPayslip = form.Controls.Find("AddPayslip", true).Single();
            Assert.That(addW2.Parent?.Parent, Is.SameAs(years.Parent));
            Assert.That(addPayslip.Parent?.Parent, Is.SameAs(payslips.Parent));
            var heading = (Label)form.Controls.Find("PayslipsHeading", true).Single();
            var headingText = TextRenderer.MeasureText(heading.Text, heading.Font);
            Assert.That(heading.ClientSize.Height - heading.Padding.Vertical, Is.GreaterThanOrEqualTo(headingText.Height));
            Assert.That(payslips.ContextMenuStrip!.Items.Cast<ToolStripItem>().Select(item => item.Text),
                Is.EqualTo(new[] { "Edit", "Delete" }));
            Assert.That(years.ContextMenuStrip!.Items.Cast<ToolStripItem>().Select(item => item.Text),
                Is.EqualTo(new[] { "Edit", "Delete" }));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void EmployerTabs_GroupYearsAndLeaveAPlaceholderUntilTheW2Exists()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            income.Save(new THMS.Domain.Finance.Income.FormW2
            {
                PersonId = PersonId,
                TaxYear = 2025,
                Employer = "Contoso",
                Wages = 80000m,
                SocialSecurityWages = 78000m,
                Traditional401k = 12000m
            });
            income.Save(new THMS.Domain.Finance.Income.FormW2
            {
                PersonId = PersonId,
                TaxYear = 2024,
                Employer = "Northwind",
                Wages = 70000m,
                SocialSecurityWages = 70000m
            });
            income.Save(new THMS.Domain.Finance.Income.Payslip
            {
                PersonId = PersonId,
                Employer = "Contoso",
                PayDate = new DateTime(2026, 1, 15),
                Gross = 2000m,
                GrossYearToDate = 2000m,
                Traditional401kYearToDate = 100m
            });
            income.Save(new THMS.Domain.Finance.Income.Payslip
            {
                PersonId = PersonId,
                Employer = "Contoso",
                PayDate = new DateTime(2026, 1, 31),
                Gross = 2100m,
                GrossYearToDate = 4100m,
                Traditional401kYearToDate = 250m,
                Roth401kYearToDate = 40m,
                HsaYearToDate = 80m
            });

            using var form = new IncomeForm(income);
            form.Show();
            var tabs = form.Controls.OfType<TabControl>().Single();

            Assert.That(tabs.TabPages.Cast<TabPage>().Select(page => page.Text), Is.EqualTo(new[] { "Contoso", "Northwind" }));

            tabs.SelectedTab = tabs.TabPages.Cast<TabPage>().Single(page => page.Text == "Contoso");
            var years = (DataGridView)form.Controls.Find("Years", true).Single();
            var yearRows = years.Rows.Cast<DataGridViewRow>().Select(row => (IncomeYearRow)row.DataBoundItem!).ToList();

            Assert.That(yearRows.Select(row => row.Year), Is.EqualTo(new[] { 2026, 2025 }));
            Assert.That(yearRows[0].IsPlaceholder, Is.True);
            Assert.That(yearRows[0].Record, Is.EqualTo("Payslips so far"));
            Assert.That(yearRows[0].Wages, Is.Null);
            Assert.That(yearRows[0].SocialSecurityWages, Is.Null);
            Assert.That(yearRows[0].Traditional401k, Is.EqualTo(250m));
            Assert.That(yearRows[0].Roth401k, Is.EqualTo(40m));
            Assert.That(yearRows[0].Hsa, Is.EqualTo(80m));
            Assert.That(yearRows[0].GrossSoFar, Is.EqualTo(4100m));
            Assert.That(yearRows[1].Wages, Is.EqualTo(80000m));
            Assert.That(yearRows[1].SocialSecurityWages, Is.EqualTo(78000m));
            Assert.That(yearRows[1].Traditional401k, Is.EqualTo(12000m));

            var payslips = (DataGridView)form.Controls.Find("Payslips", true).Single();
            var slipRows = payslips.Rows.Cast<DataGridViewRow>().Select(row => (PayslipListRow)row.DataBoundItem!).ToList();
            Assert.That(slipRows.Select(row => row.PayDate), Is.EqualTo(new[]
            {
                new DateTime(2026, 1, 31),
                new DateTime(2026, 1, 15)
            }));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PersonChoice_ShowsOnlyThatPersonsEmployers()
        {
            var people = new HouseholdPersonOrchestrator(new InMemoryHouseholdPersonStore());
            var alex = new THMS.Domain.Household.HouseholdPerson { Name = "Alex" };
            var blair = new THMS.Domain.Household.HouseholdPerson { Name = "Blair" };
            people.Save(alex);
            people.Save(blair);
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            income.Save(new THMS.Domain.Finance.Income.FormW2
            {
                PersonId = alex.Id,
                TaxYear = 2025,
                Employer = "Contoso",
                Wages = 10m,
                SocialSecurityWages = 10m
            });
            income.Save(new THMS.Domain.Finance.Income.FormW2
            {
                PersonId = blair.Id,
                TaxYear = 2025,
                Employer = "Northwind",
                Wages = 20m,
                SocialSecurityWages = 20m
            });

            using var form = new IncomeForm(income, people);
            form.Show();
            var person = (ComboBox)form.Controls.Find("Person", true).Single();
            var tabs = form.Controls.OfType<TabControl>().Single();

            person.SelectedItem = person.Items.Cast<object>().Single(item => item.ToString() == "Blair");

            Assert.That(tabs.TabPages.Cast<TabPage>().Select(page => page.Text), Is.EqualTo(new[] { "Northwind" }));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Splitter_StaysWhereItWasPut_AfterTheFormReloads()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            income.Save(new THMS.Domain.Finance.Income.Payslip
            {
                PersonId = PersonId,
                Employer = "Contoso",
                PayDate = new DateTime(2026, 1, 15),
                Gross = 2000m,
                GrossYearToDate = 2000m
            });

            using var form = new IncomeForm(income);
            form.ClientSize = new Size(1100, 800);
            form.Show();
            var split = (SplitContainer)form.Controls.Find("IncomeSplit", true).Single();
            Assert.That(split.SplitterWidth, Is.GreaterThanOrEqualTo(split.LogicalToDeviceUnits(12)));

            var max = split.Height - split.Panel2MinSize - split.SplitterWidth;
            var placed = Math.Clamp(split.Height / 2, split.Panel1MinSize, max);
            if (placed == split.SplitterDistance)
                placed = Math.Clamp(placed + 40, split.Panel1MinSize, max);
            split.SplitterDistance = placed;
            var kept = split.SplitterDistance;

            form.Reload();

            Assert.That(split.SplitterDistance, Is.EqualTo(kept));
            Assert.That(split.Parent, Is.SameAs(form.Controls.OfType<TabControl>().Single().SelectedTab));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_AddsAPersonTypedOnThePayslip()
        {
            var people = new HouseholdPersonOrchestrator(new InMemoryHouseholdPersonStore());
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            using var dialog = new PayslipDialog(income, null, "Contoso", null, null, people);
            dialog.Show();
            ((ComboBox)dialog.Controls.Find("Person", true).Single()).Text = "Alex";
            ((NumericUpDown)dialog.Controls.Find("Gross", true).Single()).Value = 100m;
            ((NumericUpDown)dialog.Controls.Find("GrossYearToDate", true).Single()).Value = 100m;

            ((Button)dialog.Controls.Find("Save", true).Single()).PerformClick();

            var saved = income.GetPayslip(dialog.PayslipId);
            var person = people.GetAll().Single();
            Assert.That(person.Name, Is.EqualTo("Alex"));
            Assert.That(saved!.PersonId, Is.EqualTo(person.Id));
            Assert.That(saved.Employer, Is.EqualTo("Contoso"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_SavesTheW2Amounts()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            var person = new THMS.Domain.Household.HouseholdPerson { Name = "Alex" };
            using var dialog = new FormW2Dialog(income, null, null, null, [person]);
            dialog.Show();
            ((NumericUpDown)dialog.Controls.Find("TaxYear", true).Single()).Value = 2025;
            ((TextBox)dialog.Controls.Find("Employer", true).Single()).Text = "Contoso";
            ((NumericUpDown)dialog.Controls.Find("Wages", true).Single()).Value = 90000m;
            ((NumericUpDown)dialog.Controls.Find("SocialSecurityWages", true).Single()).Value = 85000m;
            ((NumericUpDown)dialog.Controls.Find("Traditional401k", true).Single()).Value = 10000m;
            ((NumericUpDown)dialog.Controls.Find("Roth401k", true).Single()).Value = 2000m;
            ((NumericUpDown)dialog.Controls.Find("Hsa", true).Single()).Value = 1500m;
            var addPerson = dialog.Controls.Find("AddPerson", true).Single();
            Assert.That(addPerson, Is.InstanceOf<ThmsButton>());
            Assert.That(addPerson.Text, Is.EqualTo("Add person"));
            AssertFullyVisible(addPerson);
            AssertFullyVisible(dialog.Controls.Find("Person", true).Single());

            ((Button)dialog.Controls.Find("Save", true).Single()).PerformClick();

            var saved = income.GetFormW2(dialog.FormId);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.PersonId, Is.EqualTo(person.Id));
            Assert.That(saved.TaxYear, Is.EqualTo(2025));
            Assert.That(saved.Employer, Is.EqualTo("Contoso"));
            Assert.That(saved.Wages, Is.EqualTo(90000m));
            Assert.That(saved.SocialSecurityWages, Is.EqualTo(85000m));
            Assert.That(saved.Traditional401k, Is.EqualTo(10000m));
            Assert.That(saved.Roth401k, Is.EqualTo(2000m));
            Assert.That(saved.Hsa, Is.EqualTo(1500m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_ShowsEachBoxNumberAndGivesTheLabelsTheExtraWidth()
        {
            using var dialog = new FormW2Dialog(new IncomeOrchestrator(new InMemoryIncomeDataStore()), null);
            dialog.Show();
            try
            {
                var socialSecurity = Label(dialog, "Box 3  Social Security wages");
                Assert.That(Label(dialog, "Box c  Employer name"), Is.Not.Null);
                Assert.That(Label(dialog, "Box 1  Wages, tips, other compensation"), Is.Not.Null);
                Assert.That(Label(dialog, "Box 12, code D  Traditional 401(k)"), Is.Not.Null);
                Assert.That(Label(dialog, "Box 12, code AA  Roth 401(k)"), Is.Not.Null);
                Assert.That(Label(dialog, "Box 12, code W  HSA"), Is.Not.Null);
                AssertFits(dialog);
                AssertLinesUp(dialog);
                var employer = (TextBox)dialog.Controls.Find("Employer", true).Single();
                employer.Text = "Schlumberger Technology Corp";
                var employerText = TextRenderer.MeasureText(employer.Text, employer.Font);
                Assert.That(employer.ClientSize.Width, Is.GreaterThanOrEqualTo(employerText.Width));

                var wages = dialog.Controls.Find("SocialSecurityWages", true).Single();
                var before = wages.Left;
                dialog.ClientSize = new Size(dialog.ClientSize.Width + 160, dialog.ClientSize.Height + 200);
                dialog.Refresh();
                Assert.That(wages.Left, Is.GreaterThan(before + 100));
                AssertFits(dialog);
                AssertLinesUp(dialog);

                dialog.Size = dialog.MinimumSize;
                dialog.Refresh();
                AssertFits(dialog);
                AssertLinesUp(dialog);
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void AssertFullyVisible(Control control)
        {
            var text = TextRenderer.MeasureText(control.Text, control.Font);
            Assert.That(control.ClientSize.Width, Is.GreaterThanOrEqualTo(text.Width + control.Padding.Horizontal));
            Assert.That(control.ClientSize.Height, Is.GreaterThanOrEqualTo(text.Height + control.Padding.Vertical));
            Assert.That(control.Bottom, Is.LessThanOrEqualTo(control.Parent!.ClientSize.Height));
            Assert.That(control.Right, Is.LessThanOrEqualTo(control.Parent.ClientSize.Width));
        }

        private static Label Label(Form dialog, string text) =>
            All(dialog).OfType<Label>().Single(label => label.Text == text);

        private static void AssertFits(Form dialog)
        {
            var problems = new List<string>();
            foreach (var label in All(dialog).OfType<Label>().Where(label => label.Text.Length > 0))
            {
                var needed = TextRenderer.MeasureText(label.Text, label.Font);
                if (label.ClientSize.Width < needed.Width - 2)
                    problems.Add($"'{label.Text}' is cropped ({label.ClientSize.Width} wide, needs {needed.Width}).");
            }

            Assert.That(problems, Is.Empty);
        }

        private static void AssertLinesUp(Form dialog)
        {
            var problems = new List<string>();
            var fields = new (string Label, string Editor)[]
            {
                ("Tax year", "TaxYear"),
                ("Box c  Employer name", "Employer"),
                ("Box 1  Wages, tips, other compensation", "Wages"),
                ("Box 3  Social Security wages", "SocialSecurityWages"),
                ("Box 12, code D  Traditional 401(k)", "Traditional401k"),
                ("Box 12, code AA  Roth 401(k)", "Roth401k"),
                ("Box 12, code W  HSA", "Hsa")
            };
            foreach (var field in fields)
            {
                var label = Label(dialog, field.Label);
                var editor = dialog.Controls.Find(field.Editor, true).Single();
                var labelMid = label.PointToScreen(new Point(0, label.Height / 2)).Y;
                var editorMid = editor.PointToScreen(new Point(0, editor.Height / 2)).Y;
                if (Math.Abs(labelMid - editorMid) > 4)
                    problems.Add($"'{field.Label}' is {labelMid - editorMid}px off its field.");
            }

            Assert.That(problems, Is.Empty);
        }

        private static IEnumerable<Control> All(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var nested in All(child))
                    yield return nested;
            }
        }
    }
}
