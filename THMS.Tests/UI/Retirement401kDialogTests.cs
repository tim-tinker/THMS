using THMS.Data.Stores;
using THMS.Domain.Finance.Income;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Household;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class Retirement401kDialogTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_PricesTheSelectedPercents_AndResetsThePublishedLimits()
        {
            using var dialog = new Retirement401kDialog();
            dialog.Show();
            var pay = Input(dialog, "AnnualPay");
            var limit = Input(dialog, "ElectiveDeferralLimit");
            pay.Value = 100_000m;
            Input(dialog, "StateRate").Value = 5m;
            Input(dialog, "SelectedPreTaxPercent").Value = Input(dialog, "MaxRegularPercent").Value;

            Assert.That(Input(dialog, "MaxRegularPercent").Value, Is.EqualTo(24.50m));
            Assert.That(Input(dialog, "TakeHomeLowerBy").Value, Is.EqualTo(687.88m));

            limit.Value = 10_000m;
            Assert.That(Input(dialog, "MaxRegularPercent").Value, Is.EqualTo(10.00m));
            Assert.That(Label(dialog, "SelectionNotice").Text, Does.Contain("above the 10.00% maximum"));
            Assert.That(Input(dialog, "TakeHomeLowerBy").Value, Is.EqualTo(687.88m));
            Assert.That(pay.Value, Is.EqualTo(100_000m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_ShowsSeparateMaximumsWhenCatchUpMustBeRoth()
        {
            using var dialog = new Retirement401kDialog();
            Input(dialog, "BirthYear").Value = 1976;
            Assert.That(Input(dialog, "Age").Value, Is.EqualTo(50));
            Input(dialog, "AnnualPay").Value = 200_000m;
            Input(dialog, "PriorYearWages").Value = 150_000.01m;
            Input(dialog, "SelectedPreTaxPercent").Value = 12.25m;
            Input(dialog, "SelectedCatchUpPercent").Value = 4m;

            Assert.That(Input(dialog, "MaxRegularPercent").Value, Is.EqualTo(12.25m));
            Assert.That(Input(dialog, "MaxCatchUpPercent").Value, Is.EqualTo(4.00m));
            Assert.That(Input(dialog, "PreTaxWithheld").Value, Is.EqualTo(942.31m));
            Assert.That(Input(dialog, "RothWithheld").Value, Is.EqualTo(307.69m));
            Assert.That(Label(dialog, "SelectionNotice").Text, Does.Contain("has to be Roth"));
            Assert.That(Label(dialog, "SelectionNotice").Text, Does.Contain("can still be pre-tax"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_LowersTakeHomeOnlyByTheChangeFromTheCurrentContribution()
        {
            var store = new InMemoryIncomeDataStore();
            store.Save(new Payslip
            {
                PayDate = new DateTime(2026, 11, 27),
                Gross = 6_630.77m,
                GrossYearToDate = 6_630.77m,
                Traditional401k = 663.08m
            });
            using var dialog = new Retirement401kDialog(
                new IncomeOrchestrator(store),
                () => new DateTime(2026, 11, 27));

            Assert.That(Input(dialog, "CurrentPreTaxPercent").Value, Is.EqualTo(10.00m));
            Assert.That(Input(dialog, "SelectedPreTaxPercent").Value, Is.EqualTo(10.00m));
            Assert.That(Input(dialog, "TakeHomeLowerBy").Value, Is.EqualTo(0m));
            Assert.That(Label(dialog, "CurrentCatchUp").Text, Is.EqualTo("Not on the payslip"));

            Input(dialog, "SelectedPreTaxPercent").Value = 12.25m;
            Input(dialog, "SelectedCatchUpPercent").Value = 4m;

            Assert.That(Input(dialog, "PreTaxWithheld").Value, Is.EqualTo(812.27m));
            Assert.That(Input(dialog, "RothWithheld").Value, Is.EqualTo(265.23m));
            Assert.That(Input(dialog, "TakeHomeLowerBy").Value, Is.EqualTo(381.60m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_SavesTheMatchFormula_AndShowsThePaycheckMatch()
        {
            var income = new InMemoryIncomeDataStore();
            income.Save(new Payslip
            {
                PayDate = new DateTime(2026, 11, 27),
                Gross = 6_630.77m,
                GrossYearToDate = 6_630.77m,
                Traditional401k = 663.08m
            });
            var match = new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore());
            var clock = () => new DateTime(2026, 11, 27);
            using var dialog = new Retirement401kDialog(
                new IncomeOrchestrator(income),
                clock,
                new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore()),
                match);
            dialog.Show();
            Tabs(dialog).SelectedTab = Tabs(dialog).TabPages["EmployerMatch"];
            Input(dialog, "FirstMatchRate").Value = 100m;
            Input(dialog, "FirstMatchPercent").Value = 6m;
            Input(dialog, "NextMatchRate").Value = 50m;
            Input(dialog, "NextMatchPercent").Value = 4m;
            Check(dialog, "PlanOffersRoth").Checked = false;
            Button(dialog, "SaveMatch").PerformClick();

            Assert.That(Input(dialog, "CurrentEmployerMatch").Value, Is.EqualTo(530.47m));
            Assert.That(Input(dialog, "SelectedEmployerMatch").Value, Is.EqualTo(530.47m));
            Assert.That(Input(dialog, "EmployeeContributionIncrease").Value, Is.EqualTo(0m));

            Input(dialog, "SelectedPreTaxPercent").Value = 12.25m;
            Input(dialog, "SelectedCatchUpPercent").Value = 4m;

            Assert.That(Input(dialog, "SelectedEmployerMatch").Value, Is.EqualTo(530.47m));
            Assert.That(Input(dialog, "EmployeeContributionIncrease").Value, Is.EqualTo(414.42m));
            Assert.That(Input(dialog, "EmployerContributionIncrease").Value, Is.EqualTo(0m));

            using var again = new Retirement401kDialog(
                new IncomeOrchestrator(income),
                clock,
                new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore()),
                match);

            Assert.That(Input(again, "FirstMatchRate").Value, Is.EqualTo(100m));
            Assert.That(Input(again, "FirstMatchPercent").Value, Is.EqualTo(6m));
            Assert.That(Input(again, "NextMatchRate").Value, Is.EqualTo(50m));
            Assert.That(Input(again, "NextMatchPercent").Value, Is.EqualTo(4m));
            Assert.That(Check(again, "PlanOffersRoth").Checked, Is.False);
            Assert.That(Input(again, "CurrentEmployerMatch").Value, Is.EqualTo(530.47m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_FillsPayAndWagesFromIncome_AndRefreshesWhenOpened()
        {
            var store = new InMemoryIncomeDataStore();
            store.Save(new Payslip
            {
                PayDate = new DateTime(2026, 11, 27),
                Gross = 100m,
                GrossYearToDate = 1_000m
            });
            store.Save(new FormW2
            {
                TaxYear = 2025,
                Employer = "Schlumberger Technology Corp",
                SocialSecurityWages = 176_100m
            });
            using var dialog = new Retirement401kDialog(
                new IncomeOrchestrator(store),
                () => new DateTime(2026, 11, 27));

            Assert.That(Input(dialog, "AnnualPay").Value, Is.EqualTo(1_200m));
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(176_100m));
            Assert.That(dialog.Tips.GetToolTip(Input(dialog, "AnnualPay")), Does.Contain("2 biweekly"));
            Assert.That(dialog.Tips.GetToolTip(Input(dialog, "PriorYearWages")), Does.Contain("box 3"));
            Assert.That(dialog.Controls.Find("AnnualPaySource", true), Is.Empty);

            Input(dialog, "AnnualPay").Value = 50_000m;
            Input(dialog, "FederalRate").Value = 24m;
            Assert.That(Input(dialog, "AnnualPay").Value, Is.EqualTo(50_000m));

            ((ComboBox)dialog.Controls.Find("PayFrequency", true).Single()).SelectedIndex = 0;
            Assert.That(Input(dialog, "AnnualPay").Value, Is.EqualTo(1_400m));
            Assert.That(dialog.Tips.GetToolTip(Input(dialog, "AnnualPay")), Does.Contain("4 weekly"));
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(176_100m));

            store.Save(new Payslip
            {
                PayDate = new DateTime(2026, 12, 11),
                Gross = 200m,
                GrossYearToDate = 2_000m
            });
            dialog.Show();

            Assert.That(Input(dialog, "AnnualPay").Value, Is.EqualTo(2_400m));
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(176_100m));

            dialog.Hide();
            store.Save(new Payslip
            {
                PayDate = new DateTime(2026, 12, 25),
                Gross = 200m,
                GrossYearToDate = 3_000m
            });
            dialog.Show();

            Assert.That(Input(dialog, "AnnualPay").Value, Is.EqualTo(3_000m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_SavesLimitsByYear_AndOpensOnTheLatestYear()
        {
            var limits = new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore());
            var clock = () => new DateTime(2026, 11, 27);
            using var dialog = new Retirement401kDialog(
                new IncomeOrchestrator(new InMemoryIncomeDataStore()),
                clock,
                limits);
            dialog.Show();
            Tabs(dialog).SelectedTab = Tabs(dialog).TabPages["LegalLimits"];

            Assert.That(Input(dialog, "TaxYear").Value, Is.EqualTo(2026));
            Assert.That(Input(dialog, "ElectiveDeferralLimit").Value, Is.EqualTo(24_500m));

            Input(dialog, "ElectiveDeferralLimit").Value = 25_000m;
            Button(dialog, "SaveLimits").PerformClick();

            Input(dialog, "TaxYear").Value = 2027;
            Assert.That(Input(dialog, "ElectiveDeferralLimit").Value, Is.EqualTo(0m));
            Input(dialog, "ElectiveDeferralLimit").Value = 26_000m;
            Button(dialog, "SaveLimits").PerformClick();

            Input(dialog, "TaxYear").Value = 2026;
            Assert.That(Input(dialog, "ElectiveDeferralLimit").Value, Is.EqualTo(25_000m));

            using var again = new Retirement401kDialog(
                new IncomeOrchestrator(new InMemoryIncomeDataStore()),
                clock,
                limits);
            Assert.That(Input(again, "TaxYear").Value, Is.EqualTo(2027));
            Assert.That(Input(again, "ElectiveDeferralLimit").Value, Is.EqualTo(26_000m));
            Assert.That(Input(again, "CatchUpLimit").Value, Is.EqualTo(0m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_SavesBirthYear_AndAgesItToTheTaxYear()
        {
            var birthYear = new Retirement401kBirthYearOrchestrator(new InMemoryRetirement401kBirthYearStore());
            var clock = () => new DateTime(2026, 11, 27);
            using var dialog = new Retirement401kDialog(
                new IncomeOrchestrator(new InMemoryIncomeDataStore()),
                clock,
                new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore()),
                new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore()),
                birthYear);
            dialog.Show();
            Tabs(dialog).SelectedTab = Tabs(dialog).TabPages["PayDetails"];

            Assert.That(Input(dialog, "Age").Value, Is.EqualTo(0));
            Input(dialog, "BirthYear").Value = 1976;
            Assert.That(Input(dialog, "Age").Value, Is.EqualTo(50));
            Button(dialog, "SaveBirthYear").PerformClick();

            Input(dialog, "TaxYear").Value = 2027;
            Assert.That(Input(dialog, "Age").Value, Is.EqualTo(51));

            using var again = new Retirement401kDialog(
                new IncomeOrchestrator(new InMemoryIncomeDataStore()),
                clock,
                new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore()),
                new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore()),
                birthYear);

            Assert.That(Input(again, "BirthYear").Value, Is.EqualTo(1976));
            Assert.That(Input(again, "TaxYear").Value, Is.EqualTo(2026));
            Assert.That(Input(again, "Age").Value, Is.EqualTo(50));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_FollowsTheSelectedPersonAndEmployer()
        {
            var peopleStore = new InMemoryHouseholdPersonStore();
            var people = new HouseholdPersonOrchestrator(peopleStore);
            var alex = new HouseholdPerson { Name = "Alex", BirthYear = 1976 };
            var blair = new HouseholdPerson { Name = "Blair", BirthYear = 1990 };
            people.Save(alex);
            people.Save(blair);
            var income = new InMemoryIncomeDataStore();
            income.Save(new Payslip
            {
                PersonId = alex.Id,
                Employer = "Contoso",
                PayDate = new DateTime(2026, 11, 27),
                Gross = 6_630.77m,
                GrossYearToDate = 6_630.77m,
                Traditional401k = 663.08m
            });
            income.Save(new Payslip
            {
                PersonId = blair.Id,
                Employer = "Contoso",
                PayDate = new DateTime(2026, 11, 27),
                Gross = 1_000m,
                GrossYearToDate = 1_000m,
                Traditional401k = 100m
            });
            income.Save(new FormW2
            {
                PersonId = alex.Id,
                Employer = "Contoso",
                TaxYear = 2025,
                SocialSecurityWages = 176_100m
            });
            income.Save(new FormW2
            {
                PersonId = blair.Id,
                Employer = "Northwind",
                TaxYear = 2025,
                SocialSecurityWages = 40_000m
            });
            var match = new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore());
            using var dialog = new Retirement401kDialog(
                new IncomeOrchestrator(income),
                () => new DateTime(2026, 11, 27),
                new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore()),
                match,
                new Retirement401kBirthYearOrchestrator(new InMemoryRetirement401kBirthYearStore()),
                people);
            dialog.Show();

            var person = (ComboBox)dialog.Controls.Find("Person", true).Single();
            var employer = (ComboBox)dialog.Controls.Find("Employer", true).Single();
            Assert.That(person.Text, Is.EqualTo("Alex"));
            Assert.That(employer.Items.Cast<object>().Select(item => item.ToString()), Is.EqualTo(new[] { "Contoso" }));
            Assert.That(Input(dialog, "BirthYear").Value, Is.EqualTo(1976));
            Assert.That(Input(dialog, "Age").Value, Is.EqualTo(50));
            Assert.That(Input(dialog, "CurrentPreTaxPercent").Value, Is.EqualTo(10.00m));
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(176_100m));

            Tabs(dialog).SelectedTab = Tabs(dialog).TabPages["EmployerMatch"];
            Input(dialog, "FirstMatchRate").Value = 100m;
            Input(dialog, "FirstMatchPercent").Value = 6m;
            Input(dialog, "NextMatchRate").Value = 50m;
            Input(dialog, "NextMatchPercent").Value = 4m;
            Button(dialog, "SaveMatch").PerformClick();
            Assert.That(Input(dialog, "CurrentEmployerMatch").Value, Is.EqualTo(530.47m));

            person.SelectedItem = person.Items.Cast<HouseholdPerson>().Single(item => item.Name == "Blair");
            Assert.That(employer.Items.Cast<object>().Select(item => item.ToString()), Is.EqualTo(new[] { "Contoso", "Northwind" }));
            Assert.That(employer.Text, Is.EqualTo("Contoso"));
            Assert.That(Input(dialog, "BirthYear").Value, Is.EqualTo(1990));
            Assert.That(Input(dialog, "Age").Value, Is.EqualTo(36));
            Assert.That(Input(dialog, "CurrentPreTaxPercent").Value, Is.EqualTo(10.00m));
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(0m));
            Assert.That(Input(dialog, "FirstMatchRate").Value, Is.EqualTo(0m));

            employer.SelectedItem = "Northwind";
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(40_000m));
            Assert.That(Input(dialog, "CurrentPreTaxPercent").Value, Is.EqualTo(0m));

            person.SelectedItem = person.Items.Cast<HouseholdPerson>().Single(item => item.Name == "Alex");
            Assert.That(Input(dialog, "BirthYear").Value, Is.EqualTo(1976));
            Assert.That(Input(dialog, "PriorYearWages").Value, Is.EqualTo(176_100m));
            Assert.That(Input(dialog, "FirstMatchRate").Value, Is.EqualTo(100m));
            Assert.That(Input(dialog, "FirstMatchPercent").Value, Is.EqualTo(6m));
            Assert.That(Input(dialog, "CurrentEmployerMatch").Value, Is.EqualTo(530.47m));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dialog_OpensOnTabsForPayMatchLimitsAndSelection()
        {
            using var dialog = new Retirement401kDialog();
            dialog.Show();

            var tabs = Tabs(dialog);

            Assert.That(tabs.TabPages.Cast<TabPage>().Select(page => page.Text), Is.EqualTo(new[]
            {
                "Selection",
                "Pay Details",
                "Employer Match",
                "Legal Limits"
            }));
            Assert.That(dialog.Controls.Find("AddPerson", true), Is.Empty);
            Assert.That(OnPage(Input(dialog, "TaxYear"), tabs.TabPages["Selection"]!), Is.True);
            Assert.That(OnPage(Input(dialog, "TaxYear"), tabs.TabPages["LegalLimits"]!), Is.False);
            Assert.That(dialog.Controls.Find("CalculatorSplit", true), Is.Empty);
            Assert.That(tabs.Width, Is.GreaterThanOrEqualTo(tabs.ItemSize.Width * tabs.TabCount));
            Assert.That(dialog.ClientSize.Height, Is.GreaterThanOrEqualTo(960));
        }

        private static bool OnPage(Control control, TabPage page)
        {
            for (var node = control.Parent; node is not null; node = node.Parent)
            {
                if (node == page)
                    return true;
            }

            return false;
        }

        private static TabControl Tabs(Control root) =>
            (TabControl)root.Controls.Find("CalculatorTabs", true).Single();

        private static NumericUpDown Input(Control root, string name) =>
            (NumericUpDown)root.Controls.Find(name, true).Single();

        private static Label Label(Control root, string name) =>
            (Label)root.Controls.Find(name, true).Single();

        private static Button Button(Control root, string name) =>
            (Button)root.Controls.Find(name, true).Single();

        private static CheckBox Check(Control root, string name) =>
            (CheckBox)root.Controls.Find(name, true).Single();
    }
}
