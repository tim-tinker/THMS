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

        [Test]
        [Apartment(ApartmentState.STA)]
        public void HistoryCell_ShowsThatMonthAndColumn()
        {
            var julyCash = new NetWorthLine { Group = "Cash", Name = "Checking", Amount = 10m, Source = "Balance as of 7/31/2026" };
            var julyBrokerage = new NetWorthLine { Group = "Investments", Name = "Brokerage", Amount = 20m, Source = "Market value" };
            var julyHsa = new NetWorthLine { Group = "Investments", Name = "HSA", Amount = 200m, Source = "Market value" };
            var julyAuto = new NetWorthLine { Group = "Debt", Name = "Auto", Amount = -50m, Source = "Balance as of 7/31/2026" };
            var julyMortgage = new NetWorthLine { Group = "Debt", Name = "Mortgage", Amount = -600m, Source = "Balance as of 7/31/2026" };
            var today = new[]
            {
                new NetWorthLine { Group = "Cash", Name = "Checking", Amount = 300m, Source = "Current balance" },
                new NetWorthLine { Group = "Cash", Name = "Savings", Amount = 1200m, Source = "Current balance" },
                new NetWorthLine { Group = "Investments", Name = "HSA", Amount = 100m, Source = "No position history" }
            };
            var snapshot = new NetWorthSnapshot
            {
                Lines = today,
                Months =
                [
                    new NetWorthMonthRow
                    {
                        Month = new DateTime(2026, 7, 1),
                        Label = "Jul 2026",
                        Cash = 10m,
                        Investments = 100m,
                        NetWorth = 110m,
                        Lines = [julyCash, julyBrokerage, julyHsa, julyAuto, julyMortgage]
                    },
                    new NetWorthMonthRow
                    {
                        Month = new DateTime(2026, 11, 1),
                        Label = "Nov 2026",
                        Cash = 1500m,
                        Investments = 100m,
                        NetWorth = 1600m,
                        Lines = today
                    }
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

                form.SelectHistory("Nov 2026", nameof(NetWorthMonthRow.Label));
                Assert.That(form.DetailHeading, Is.EqualTo("Nov 2026"));
                Assert.That(form.DetailNames(), Is.EqualTo(new[] { "Savings", "Checking", "HSA" }));

                form.SelectHistory("Jul 2026", nameof(NetWorthMonthRow.Investments));
                Assert.That(form.DetailHeading, Is.EqualTo("Jul 2026 · Investments"));
                Assert.That(form.DetailNames(), Is.EqualTo(new[] { "HSA", "Brokerage" }));

                form.SelectHistory("Jul 2026", nameof(NetWorthMonthRow.Debt));
                Assert.That(form.DetailHeading, Is.EqualTo("Jul 2026 · Debt"));
                Assert.That(form.DetailNames(), Is.EqualTo(new[] { "Mortgage", "Auto" }));

                form.SelectHistory("Jul 2026", nameof(NetWorthMonthRow.Cash));
                Assert.That(form.DetailHeading, Is.EqualTo("Jul 2026 · Cash"));
                Assert.That(form.DetailNames(), Is.EqualTo(new[] { "Checking" }));

                form.SelectHistory("Jul 2026", nameof(NetWorthMonthRow.NetWorth));
                Assert.That(form.DetailHeading, Is.EqualTo("Jul 2026"));
                Assert.That(form.DetailNames(), Is.EqualTo(new[] { "Checking", "HSA", "Brokerage", "Mortgage", "Auto" }));

                form.SelectHistory("Jul 2026", nameof(NetWorthMonthRow.Change));
                Assert.That(form.DetailHeading, Is.EqualTo("Jul 2026"));
                Assert.That(form.DetailNames(), Is.EqualTo(new[] { "Checking", "HSA", "Brokerage", "Mortgage", "Auto" }));

                Click(Caption(form, "Cash"));
                Application.DoEvents();
                Assert.That(form.OpenBreakdownRows(), Is.EqualTo(new[]
                {
                    new[] { "Savings", 1200m.ToString("c2") },
                    new[] { "Checking", 300m.ToString("c2") }
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
