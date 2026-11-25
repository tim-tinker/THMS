using THMS.Domain.Finance.Accounts;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class AccountEditFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void LeaseEditor_UsesMoneyFactorAndShowsApproximateApr()
        {
            using var form = new AccountEditForm(new LeaseAccount
            {
                Name = "Car lease",
                Type = AccountType.Lease,
                MoneyFactor = 0.00125m
            });
            form.Show();
            try
            {
                var labels = All(form).OfType<Label>().Where(label => label.Visible).Select(label => label.Text).ToList();
                Assert.That(labels, Does.Contain("Money factor:"));
                Assert.That(labels, Has.None.EqualTo("Interest Rate (%):"));
                Assert.That(labels, Has.Some.Contains("3% APR"));

                var factor = All(form).OfType<NumericUpDown>().Single(box => box.Visible && box.DecimalPlaces == 8);
                factor.Value = 0.002m;
                All(form).OfType<Button>().Single(button => button.Text == "Save").PerformClick();

                var saved = (LeaseAccount)form.Account;
                Assert.That(form.DialogResult, Is.EqualTo(DialogResult.OK));
                Assert.That(saved.MoneyFactor, Is.EqualTo(0.002m));
                Assert.That(saved.InterestRate, Is.EqualTo(0m));
                Assert.That(saved.ApproximateAprPercent, Is.EqualTo(4.8m));
            }
            finally
            {
                form.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void LeaseEditor_CalculatesMoneyFactorFromRentCharge()
        {
            using var form = new AccountEditForm(new LeaseAccount
            {
                Name = "Car lease",
                Type = AccountType.Lease,
                TermMonths = 39
            });
            form.Show();
            try
            {
                Box(form, "Gross cap cost:").Value = 68272.33m;
                Box(form, "Cap reduction:").Value = 19750m;
                Assert.That(Box(form, "Adjusted cap cost:").Value, Is.EqualTo(48522.33m));

                Box(form, "Residual:").Value = 23744.70m;
                Box(form, "Rent charge:").Value = 3678.72m;

                var shown = Math.Round(3678.72m / (39m * (48522.33m + 23744.70m)), 8, MidpointRounding.AwayFromZero);
                Assert.That(Box(form, "Money factor:").Value, Is.EqualTo(shown));

                var hint = All(form).OfType<Label>().Single(label => label.Visible && label.Text.Contains("Money factor ="));
                Assert.That(hint.Text, Does.Contain("3.133% APR"));
                Assert.That(hint.Text, Does.Contain(24777.63m.ToString("C2")));
                Assert.That(hint.Text, Does.Contain(28456.35m.ToString("C2")));

                All(form).OfType<Button>().Single(button => button.Text == "Save").PerformClick();
                var saved = (LeaseAccount)form.Account;
                Assert.That(saved.RentCharge, Is.EqualTo(3678.72m));
                Assert.That(saved.AdjustedCapitalizedCost, Is.EqualTo(48522.33m));
                Assert.That(saved.MoneyFactor, Is.EqualTo(3678.72m / (39m * (48522.33m + 23744.70m))));
            }
            finally
            {
                form.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void LoanEditor_KeepsAnInterestRateAndConvertsItWhenSwitchedToALease()
        {
            using var form = new AccountEditForm(new LoanAccount
            {
                Name = "Auto",
                Type = AccountType.Loan,
                InterestRate = 4.8m
            });
            form.Show();
            try
            {
                Assert.That(
                    All(form).OfType<Label>().Any(label => label.Visible && label.Text == "Interest Rate (%):"),
                    Is.True);

                var combo = All(form).OfType<ComboBox>().First(box => box.Items.Contains("Lease"));
                combo.SelectedItem = "Lease";

                var factor = All(form).OfType<NumericUpDown>().Single(box => box.Visible && box.DecimalPlaces == 8);
                Assert.That(factor.Value, Is.EqualTo(0.002m));
                Assert.That(
                    All(form).OfType<Label>().Any(label => label.Visible && label.Text.Contains("4.8% APR")),
                    Is.True);
            }
            finally
            {
                form.Close();
            }
        }

        private static NumericUpDown Box(Control form, string labelText)
        {
            var label = All(form).OfType<Label>().Single(item => item.Text == labelText);
            return All(form).OfType<NumericUpDown>().Single(box => box.Top == label.Top && box.Parent == label.Parent);
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
