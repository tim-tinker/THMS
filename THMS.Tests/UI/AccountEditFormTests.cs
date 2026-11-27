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
                Assert.That(form.LeaseHelpText(), Does.Contain("3% APR"));

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
                Assert.That(Box(form, "Gross cap cost:").Value, Is.EqualTo(-68272.33m));
                Box(form, "Cap reduction:").Value = 19750m;
                Assert.That(Box(form, "Adjusted cap cost:").Value, Is.EqualTo(-48522.33m));
                Assert.That(Box(form, "Adjusted cap cost:").ReadOnly, Is.True);

                Box(form, "Residual:").Value = 23744.70m;
                Assert.That(Box(form, "Residual:").Value, Is.EqualTo(-23744.70m));
                Box(form, "Rent charge:").Value = 3678.72m;
                Assert.That(Box(form, "Rent charge:").Value, Is.EqualTo(-3678.72m));
                Assert.That(Box(form, "Money factor:").ReadOnly, Is.True);

                var shown = Math.Round(3678.72m / (39m * (48522.33m + 23744.70m)), 8, MidpointRounding.AwayFromZero);
                Assert.That(Box(form, "Money factor:").Value, Is.EqualTo(shown));

                var hint = form.LeaseHelpText();
                Assert.That(hint, Does.Contain("Money factor ="));
                Assert.That(hint, Does.Contain("3.133% APR"));
                Assert.That(hint, Does.Contain(24777.63m.ToString("C2")));
                Assert.That(hint, Does.Contain(28456.35m.ToString("C2")));

                All(form).OfType<Button>().Single(button => button.Text == "Save").PerformClick();
                var saved = (LeaseAccount)form.Account;
                Assert.That(saved.GrossCapitalizedCost, Is.EqualTo(68272.33m));
                Assert.That(saved.RentCharge, Is.EqualTo(3678.72m));
                Assert.That(saved.Residual, Is.EqualTo(23744.70m));
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
                Assert.That(factor.ReadOnly, Is.True);
                Assert.That(form.LeaseHelpText(), Does.Contain("4.8% APR"));
                Assert.That(All(form).OfType<Button>().Any(button => button.Visible && button.Text == "?"), Is.True);
            }
            finally
            {
                form.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void LeaseEditor_FillsChargesInOrderAndShowsThemAsNegative()
        {
            using var form = new AccountEditForm(new LeaseAccount
            {
                Name = "Car lease",
                Type = AccountType.Lease,
                TermMonths = 39,
                Principal = -3648.25m
            });
            form.Show();
            try
            {
                var labels = new[]
                {
                    "Term (months):",
                    "Gross cap cost:",
                    "Cap reduction:",
                    "Adjusted cap cost:",
                    "Residual:",
                    "Rent charge:",
                    "Money factor:",
                    "Lease amount:",
                    "Balance:"
                };
                var tops = labels.Select(label => Box(form, label).Top).ToArray();
                for (var i = 1; i < tops.Length; i++)
                    Assert.That(tops[i], Is.GreaterThan(tops[i - 1]), labels[i]);

                Box(form, "Lease amount:").Value = 28456.35m;
                Assert.That(Box(form, "Lease amount:").Value, Is.EqualTo(-28456.35m));
                Box(form, "Balance:").Value = 3648.25m;
                Assert.That(Box(form, "Balance:").Value, Is.EqualTo(-3648.25m));

                foreach (var label in new[] { "Gross cap cost:", "Residual:", "Rent charge:", "Lease amount:", "Balance:" })
                {
                    var tip = Tip(form, Box(form, label));
                    Assert.That(tip, Does.Contain("negative"));
                    Assert.That(tip, Does.Contain("charge"));
                }

                Assert.That(Box(form, "Adjusted cap cost:").ReadOnly, Is.True);
                Assert.That(Box(form, "Money factor:").ReadOnly, Is.True);
                Assert.That(Box(form, "Cap reduction:").ReadOnly, Is.False);

                var name = All(form).OfType<TextBox>().Single(box =>
                    box.Top == All(form).OfType<Label>().Single(label => label.Text == "Name:").Top);
                var gross = Box(form, "Gross cap cost:");
                var grossLabel = All(form).OfType<Label>().Single(label => label.Visible && label.Text == "Gross cap cost:");
                Assert.That(gross.TextAlign, Is.EqualTo(HorizontalAlignment.Right));
                Assert.That(grossLabel.Right, Is.LessThan(gross.Left));
                Assert.That(
                    gross.PointToScreen(Point.Empty).X + gross.Width,
                    Is.EqualTo(name.PointToScreen(Point.Empty).X + name.Width).Within(2));
                Assert.That(All(form).OfType<Button>().Single(button => button.Text == "?").Visible, Is.True);
                Assert.That(All(form).OfType<Label>().Any(label => label.Visible && label.Text.Contains("Money factor =")), Is.False);
            }
            finally
            {
                form.Close();
            }
        }

        private static string Tip(Control form, Control control)
        {
            var tips = (ToolTip)typeof(AccountEditForm)
                .GetField("_leaseTips", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(form)!;
            return tips.GetToolTip(control) ?? "";
        }

        private static NumericUpDown Box(Control form, string labelText)
        {
            var label = All(form).OfType<Label>().Single(item => item.Visible && item.Text == labelText);
            return All(form).OfType<NumericUpDown>().Single(box => box.Visible && box.Top == label.Top && box.Parent == label.Parent);
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
