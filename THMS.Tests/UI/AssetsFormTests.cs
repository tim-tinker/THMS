using THMS.Data.Stores;
using THMS.Domain.Assets;
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
                foreach (var title in new[] { "Assets", "Valuations", "Property tax statements" })
                {
                    var label = labels.Single(item => item.Text == title);
                    var needed = TextRenderer.MeasureText(label.Text, label.Font);
                    if (label.ClientSize.Width < needed.Width - 2 || label.ClientSize.Height < needed.Height - 2)
                        problems.Add($"'{title}' is cropped.");
                    var grid = label.Parent?.Controls.OfType<DataGridView>().Single();
                    if (grid is not null && Rectangle.Intersect(label.Bounds, grid.Bounds) is { Width: > 2, Height: > 2 })
                        problems.Add($"'{title}' overlaps its grid.");
                }

                var adds = All(form).OfType<ThmsButton>().Where(button => button.Text == "Add").ToList();
                if (adds.Count != 3)
                    problems.Add($"Expected 3 Add buttons, found {adds.Count}.");

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
                AssertMenu(GridUnder(form, "Valuations"), "Edit");
                AssertMenu(GridWithColumn(form, "Tax year"), "Edit", "Delete");
                var valuationsWidth = GridUnder(form, "Valuations").Parent!.Width;
                var expected = (int)Math.Round(form.ClientSize.Width * 0.6);
                Assert.That(valuationsWidth, Is.EqualTo(expected).Within(8));
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
