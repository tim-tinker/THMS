using THMS.Data.Stores;
using THMS.Logic.Orchestrators;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class ValuationSourceDialogTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void AddSourceDialog_IsLargeEnoughAndCanBeResized()
        {
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());

            using var dialog = new ValuationSourceDialog(orchestrator, null);
            dialog.Show();
            try
            {
                dialog.Refresh();
                Assert.That(dialog.FormBorderStyle, Is.EqualTo(FormBorderStyle.Sizable));
                Assert.That(dialog.MaximumSize, Is.EqualTo(Size.Empty));
                Assert.That(dialog.MinimumSize.Width, Is.GreaterThan(0));
                Assert.That(dialog.MinimumSize.Height, Is.GreaterThan(0));

                AssertFits(dialog);

                var website = All(dialog).OfType<TextBox>().Single(box => box.Name == "Website");
                var before = website.Width;
                dialog.ClientSize = new Size(dialog.ClientSize.Width + 160, dialog.ClientSize.Height + 80);
                dialog.Refresh();
                Assert.That(website.Width, Is.GreaterThan(before + 100));
                AssertFits(dialog);

                dialog.Size = dialog.MinimumSize;
                dialog.Refresh();
                AssertFits(dialog);
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void AssertFits(Form dialog)
        {
            var problems = new List<string>();
            foreach (var label in All(dialog).OfType<Label>().Where(label => label.Visible && label.Text.Length > 0 && label is not LinkLabel))
            {
                var needed = TextRenderer.MeasureText(
                    label.Text,
                    label.Font,
                    new Size(Math.Max(1, label.ClientSize.Width), int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                if (label.ClientSize.Width < 8 || label.ClientSize.Height < needed.Height - 2)
                    problems.Add($"'{label.Text}' is cropped ({label.ClientSize.Width}x{label.ClientSize.Height}, needs {needed.Width}x{needed.Height}).");
            }

            foreach (var check in All(dialog).OfType<CheckBox>())
            {
                var needed = TextRenderer.MeasureText(check.Text, check.Font);
                if (check.ClientSize.Width < needed.Width - 4 || check.ClientSize.Height < needed.Height - 2)
                    problems.Add($"'{check.Text}' is cropped.");
            }

            var hint = All(dialog).OfType<Label>().Single(label => label.Text.StartsWith("The website"));
            var save = (Control)dialog.AcceptButton!;
            var cancel = (Control)dialog.CancelButton!;
            if (Intersects(hint, save) || Intersects(hint, cancel))
                problems.Add("The hint overlaps the buttons.");
            if (!Inside(dialog, save) || !Inside(dialog, cancel))
                problems.Add("A button is outside the dialog.");

            Assert.That(problems, Is.Empty);
        }

        private static bool Intersects(Control left, Control right)
        {
            var overlap = Rectangle.Intersect(BoundsOnScreen(left), BoundsOnScreen(right));
            return overlap.Width > 2 && overlap.Height > 2;
        }

        private static bool Inside(Form dialog, Control control)
        {
            var origin = dialog.PointToClient(control.PointToScreen(Point.Empty));
            var bounds = new Rectangle(origin, control.Size);
            return Rectangle.Inflate(dialog.ClientRectangle, -4, -4).Contains(bounds);
        }

        private static Rectangle BoundsOnScreen(Control control) =>
            new(control.PointToScreen(Point.Empty), control.Size);

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
