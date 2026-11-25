using System.ComponentModel;
using System.Reflection;
using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Assets;
using THMS.UI.WinForms;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class AssetsFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void SectionTitlesAreVisibleAndAddsSitUnderEachGrid()
        {
            using var form = new AssetsForm();
            form.ClientSize = new Size(1100, 760);
            form.Show();
            try
            {
                form.Refresh();
                var problems = new List<string>();
                var labels = All(form).OfType<Label>().Where(label => label.Visible).ToList();
                foreach (var title in new[] { "Assets", "Valuations" })
                {
                    var label = labels.Single(item => item.Text == title);
                    var needed = TextRenderer.MeasureText(label.Text, label.Font);
                    if (label.ClientSize.Width < needed.Width - 2 || label.ClientSize.Height < needed.Height - 2)
                        problems.Add($"'{title}' is cropped.");
                    var grid = label.Parent?.Controls.OfType<DataGridView>().Single();
                    if (grid is not null && Rectangle.Intersect(label.Bounds, grid.Bounds) is { Width: > 2, Height: > 2 })
                        problems.Add($"'{title}' overlaps its grid.");
                }

                var adds = All(form).OfType<ThmsButton>().Where(button => button.Visible && button.Text == "Add").ToList();
                if (adds.Count != 2)
                    problems.Add($"Expected 2 Add buttons, found {adds.Count}.");

                foreach (var button in adds)
                {
                    var host = button.Parent?.Parent;
                    var grid = host?.Controls.OfType<DataGridView>().Single();
                    if (grid is null || button.Parent is null)
                    {
                        problems.Add("Add is not under a grid.");
                        continue;
                    }

                    if (button.Parent.Top < grid.Bottom - 1)
                        problems.Add("Add overlaps its grid.");
                    var origin = form.PointToClient(button.PointToScreen(Point.Empty));
                    var bounds = new Rectangle(origin, button.Size);
                    if (!form.ClientRectangle.Contains(bounds))
                        problems.Add("Add is outside the form.");
                }

                AssertMenu(GridWithColumn(form, "Name"), "Edit", "Delete");
                AssertMenu(GridUnder(form, "Valuations"), "Edit", "Add property tax statement");
                var valuations = GridUnder(form, "Valuations").Parent!;
                var split = (SplitContainer)valuations.Parent!.Parent!;
                Assert.That(split.Orientation, Is.EqualTo(Orientation.Horizontal));
                Assert.That(split.SplitterDistance, Is.EqualTo(split.Height / 2).Within(8));
                Assert.That(problems, Is.Empty);
            }
            finally
            {
                form.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PropertyValuations_AreOneRowPerDate()
        {
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);
            orchestrator.SavePropertyAppraisal(property, new PropertyAppraisal
            {
                AsOf = new DateTime(2026, 5, 1),
                Source = "Fort Bend CAD",
                LandMarketValue = 84500m,
                ImprovementMarketValue = 542600m
            });
            orchestrator.SavePropertyAppraisal(property, new PropertyAppraisal
            {
                AsOf = new DateTime(2025, 5, 1),
                Source = "Fort Bend CAD",
                LandMarketValue = 84500m,
                ImprovementMarketValue = 546900m
            });

            using var form = new AssetsForm(orchestrator);
            form.ClientSize = new Size(1400, 760);
            form.Show();
            try
            {
                var assets = GridWithColumn(form, "Name");
                var assetRow = assets.Rows.Cast<DataGridViewRow>()
                    .Single(row => row.DataBoundItem is AssetListRow item && item.Name == "Casa Tinker");
                assets.CurrentCell = assetRow.Cells[0];

                var grid = GridUnder(form, "Valuations");
                Assert.That(grid.Columns.Cast<DataGridViewColumn>().Select(column => column.HeaderText).ToArray(),
                    Is.EqualTo(new[]
                    {
                        "As of", "Land market", "Impr market", "Total market", "Homestead cap", "Assessed", "Source"
                    }));
                Assert.That(grid.Rows.Count, Is.EqualTo(2));

                var latest = (PropertyAppraisal)grid.Rows[0].DataBoundItem!;
                Assert.That(latest.AsOf, Is.EqualTo(new DateTime(2026, 5, 1)));
                Assert.That(latest.LandMarketValue, Is.EqualTo(84500m));
                Assert.That(latest.ImprovementMarketValue, Is.EqualTo(542600m));
                Assert.That(latest.TotalMarketValue, Is.EqualTo(627100m));
                Assert.That(latest.HomesteadCap, Is.EqualTo(0m));
                Assert.That(latest.AssessedValue, Is.EqualTo(627100m));
                Assert.That(latest.Source, Is.EqualTo("Fort Bend CAD"));
                Assert.That(grid.Rows[0].Cells[nameof(PropertyAppraisal.LandMarketValue)].FormattedValue,
                    Is.EqualTo(84500m.ToString("c2")));

                var prior = (PropertyAppraisal)grid.Rows[1].DataBoundItem!;
                Assert.That(prior.AsOf, Is.EqualTo(new DateTime(2025, 5, 1)));
                Assert.That(prior.TotalMarketValue, Is.EqualTo(631400m));

                var taxItem = MenuItem(grid, "Add property tax statement");
                OpenMenu(grid);
                Assert.That(taxItem.Available, Is.True);
                Assert.That(taxItem.Enabled, Is.True);
                Assert.That(All(form).OfType<DataGridView>().Any(item =>
                    item.Columns.Cast<DataGridViewColumn>().Any(column => column.HeaderText == "Tax year")), Is.False);
            }
            finally
            {
                form.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void VehicleValuations_DoNotOfferAPropertyTaxStatement()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var details = new VehicleIce
            {
                Year = 2024,
                Make = "Ford",
                Model = "Mustang Mach-E",
                Vin = "1FMCU0G6XPUA12345",
                Name = "Bluey"
            };
            vehicles.UpsertVehicle(details);
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                vehicles);
            var car = new VehicleAsset { Name = "Bluey", Category = "Vehicle" };
            orchestrator.SaveAsset(car, null, details);
            orchestrator.SaveValuation(new AssetValuation
            {
                AssetId = car.Id,
                ValueType = ValuationTypes.PrivateParty,
                Amount = 24710m,
                AsOf = new DateTime(2026, 11, 24),
                Source = "Edmunds"
            });

            using var form = new AssetsForm(orchestrator);
            form.ClientSize = new Size(1100, 760);
            form.Show();
            try
            {
                var grid = GridUnder(form, "Valuations");
                Assert.That(grid.Rows.Count, Is.EqualTo(1));
                var taxItem = MenuItem(grid, "Add property tax statement");
                OpenMenu(grid);
                Assert.That(taxItem.Available, Is.False);
            }
            finally
            {
                form.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void SourcesTab_ListsValuationSourcesWithTheAssetsTheyAppraise()
        {
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());
            orchestrator.SaveValuationSource(new ValuationSource
            {
                Name = "Fort Bend CAD",
                WebsiteUrl = "www.fbcad.org",
                Kinds = AppraisedAssetKind.Property
            });
            orchestrator.SaveValuationSource(new ValuationSource
            {
                Name = "Kelley Blue Book",
                WebsiteUrl = "https://www.kbb.com",
                Kinds = AppraisedAssetKind.Vehicle
            });
            orchestrator.SaveValuationSource(new ValuationSource
            {
                Name = "Tax notice",
                WebsiteUrl = "",
                Kinds = AppraisedAssetKind.Property
            });

            using var form = new AssetsForm(orchestrator);
            form.ClientSize = new Size(1100, 760);
            form.Show();
            try
            {
                var tabs = All(form).OfType<TabControl>().Single();
                tabs.SelectedTab = tabs.TabPages.Cast<TabPage>().Single(page => page.Text == "Sources");
                form.Refresh();

                var grid = All(form).OfType<DataGridView>().Single(item =>
                    item.Columns.Cast<DataGridViewColumn>().Any(column => column.HeaderText == "Appraises"));
                Assert.That(grid.Columns.Cast<DataGridViewColumn>().Select(column => column.HeaderText).ToArray(),
                    Is.EqualTo(new[] { "Source", "Appraises", "Website" }));
                Assert.That(grid.Columns[nameof(ValuationSource.Name)], Is.InstanceOf<DataGridViewLinkColumn>());
                Assert.That(grid.Rows.Cast<DataGridViewRow>().Select(row => ((ValuationSource)row.DataBoundItem!).Name).ToArray(),
                    Is.EqualTo(new[] { "Fort Bend CAD", "Kelley Blue Book", "Tax notice" }));
                var cad = (ValuationSource)grid.Rows[0].DataBoundItem!;
                Assert.That(cad.AppraisesLabel, Is.EqualTo("Property"));
                Assert.That(cad.Website, Is.Not.Null);
                Assert.That(cad.Website!.Host, Is.EqualTo("www.fbcad.org"));
                var name = grid.Columns[nameof(ValuationSource.Name)].Index;
                Assert.That(((DataGridViewLinkCell)grid.Rows[0].Cells[name]).LinkColor, Is.EqualTo(DataGridViewUtil.LinkFore));
                Assert.That(((DataGridViewLinkCell)grid.Rows[2].Cells[name]).LinkColor, Is.Not.EqualTo(DataGridViewUtil.LinkFore));

                var menu = grid.ContextMenuStrip!.Items.Cast<ToolStripItem>().Select(item => item.Text).ToArray();
                Assert.That(menu, Is.EqualTo(new[] { "Edit", "Delete" }));
            }
            finally
            {
                form.Close();
            }
        }

        private static DataGridView GridUnder(Form form, string title)
        {
            var label = All(form).OfType<Label>().Single(item => item.Text == title);
            return label.Parent!.Controls.OfType<DataGridView>().Single();
        }

        private static ToolStripMenuItem MenuItem(DataGridView grid, string text) =>
            grid.ContextMenuStrip!.Items.Cast<ToolStripMenuItem>().Single(item => item.Text == text);

        private static void OpenMenu(DataGridView grid)
        {
            var opening = typeof(ToolStripDropDown).GetMethod(
                "OnOpening",
                BindingFlags.Instance | BindingFlags.NonPublic);
            opening!.Invoke(grid.ContextMenuStrip, [new CancelEventArgs()]);
        }

        private static void AssertMenu(DataGridView grid, params string[] items)
        {
            var menu = grid.ContextMenuStrip;
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu!.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(items));
        }

        private static DataGridView GridWithColumn(Form form, string header) =>
            All(form).OfType<DataGridView>().Single(grid =>
                grid.Columns.Cast<DataGridViewColumn>().Any(column => column.HeaderText == header));

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
