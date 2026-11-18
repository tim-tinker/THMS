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
                AssertMenu(GridWithColumn(form, "Value"), "Edit");
                AssertMenu(GridWithColumn(form, "Tax year"), "Edit", "Delete");
                Assert.That(problems, Is.Empty);
            }
            finally
            {
                form.Close();
            }
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
