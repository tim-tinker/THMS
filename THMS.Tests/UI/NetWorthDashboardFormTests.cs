using System.Reflection;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class NetWorthDashboardFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Cards_OpenABreakdownForEveryTotalExceptNetWorth()
        {
            var snapshot = new NetWorthSnapshot
            {
                NetWorth = 1000m,
                Cash = 1500m,
                Investments = 100m,
                OtherAssets = 450m,
                Debt = -1050m,
                Lines =
                [
                    new NetWorthLine { Group = "Cash", Name = "Checking", Amount = 300m, Source = "Current balance" },
                    new NetWorthLine { Group = "Cash", Name = "Savings", Amount = 1200m, Source = "Current balance" },
                    new NetWorthLine { Group = "Investments", Name = "HSA", Amount = 100m, Source = "No position history" },
                    new NetWorthLine { Group = "Property", Name = "House", Amount = 50m, Source = "Total market as of 11/1/2026" },
                    new NetWorthLine { Group = "Vehicles", Name = "Bluey", Amount = 400m, Source = "Private party median of 2" },
                    new NetWorthLine { Group = "Personal", Name = "Piano", Amount = null, Source = "No valuation" },
                    new NetWorthLine { Group = "Debt", Name = "Mortgage", Amount = -450m, Source = "Current balance, secured by House" },
                    new NetWorthLine { Group = "Debt", Name = "Visa", Amount = -600m, Source = "Current balance" }
                ]
            };

            using var form = new NetWorthDashboardForm();
            form.ClientSize = new Size(1200, 800);
            form.InitializeDashboard();
            form.Show();
            try
            {
                form.Bind(snapshot);
                form.Refresh();

                Assert.That(Caption(form, "Net worth").Text, Is.EqualTo("Net worth"));
                Assert.That(Caption(form, "Net worth").Cursor, Is.EqualTo(Cursors.Default));
                Assert.That(Caption(form, "Cash").Text, Is.EqualTo("Cash  ▾"));
                Assert.That(Caption(form, "Cash").Cursor, Is.EqualTo(Cursors.Hand));
                Assert.That(Caption(form, "Investments").Text, Is.EqualTo("Investments  ▾"));
                Assert.That(Caption(form, "Other assets").Text, Is.EqualTo("Other assets  ▾"));
                Assert.That(Caption(form, "Debt").Text, Is.EqualTo("Debt  ▾"));

                Click(Caption(form, "Net worth"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.Empty);

                Click(Caption(form, "Cash"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.EqualTo(new[]
                {
                    new[] { "Savings", 1200m.ToString("c2") },
                    new[] { "Checking", 300m.ToString("c2") }
                }));

                Click(Caption(form, "Cash"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.Empty);

                Click(Caption(form, "Investments"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.EqualTo(new[]
                {
                    new[] { "HSA", 100m.ToString("c2") }
                }));

                Click(Caption(form, "Investments"));
                Application.DoEvents();

                Click(Caption(form, "Other assets"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.EqualTo(new[]
                {
                    new[] { "Vehicles", "Bluey", 400m.ToString("c2") },
                    new[] { "Property", "House", 50m.ToString("c2") }
                }));

                Click(Caption(form, "Other assets"));
                Application.DoEvents();

                Click(Caption(form, "Debt"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.EqualTo(new[]
                {
                    new[] { "Visa", (-600m).ToString("c2") },
                    new[] { "Mortgage", (-450m).ToString("c2") }
                }));
            }
            finally
            {
                form.Close();
            }
        }

        private static Label Caption(Form form, string title) =>
            All(form).OfType<Label>().Single(label => label.Text == title || label.Text == title + "  ▾");

        private static void Click(Control control)
        {
            var method = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
            method!.Invoke(control, [EventArgs.Empty]);
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
