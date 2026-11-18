using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Logic.Orchestrators;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class PropertyTaxStatementDialogTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void FieldsAreStackedAndButtonsUseThmsButton()
        {
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());
            var property = new PropertyAsset
            {
                Name = "Casa Tinker",
                Category = "Real estate",
                ParcelAddress = "6219 Cresting Knolls Cir, Richmond, TX 77407",
                CadNumber = "R424196",
                TaxAccountNumber = "5121-32-001-0130-901",
                LegalDescription = "Long Meadow Farms Sec 32, Block 1, Lot 32",
                RollCode = "Real Property"
            };
            orchestrator.SaveAsset(property, null, null);

            using var dialog = new PropertyTaxStatementDialog(orchestrator, null, null, property);
            dialog.Show();
            try
            {
                dialog.Refresh();
                var problems = new List<string>();
                foreach (var label in All(dialog).OfType<Label>().Where(label => label.Visible && label.Text.Length > 0))
                {
                    var needed = TextRenderer.MeasureText(label.Text, label.Font);
                    if (label.ClientSize.Width < needed.Width - 4 || label.ClientSize.Height < needed.Height - 2)
                        problems.Add($"'{label.Text}' is cropped.");
                }

                foreach (var group in All(dialog).Where(control => control.Visible).GroupBy(control => control.Parent))
                {
                    var siblings = group.Where(control => control.Width > 0 && control.Height > 0).ToList();
                    for (var i = 0; i < siblings.Count; i++)
                    {
                        for (var j = i + 1; j < siblings.Count; j++)
                        {
                            var overlap = Rectangle.Intersect(siblings[i].Bounds, siblings[j].Bounds);
                            if (overlap.Width > 2 && overlap.Height > 2)
                                problems.Add($"'{Describe(siblings[i])}' overlaps '{Describe(siblings[j])}'.");
                        }
                    }
                }

                var propertyCombo = All(dialog).OfType<ComboBox>().First(combo => combo.Items.Contains(property));
                Assert.That(propertyCombo.GetItemText(property), Is.EqualTo("Casa Tinker"));

                var address = All(dialog).OfType<TextBox>().Single(box => box.Text == property.ParcelAddress);
                var legal = All(dialog).OfType<TextBox>().Single(box => box.Text == property.LegalDescription);
                var addressWidth = TextRenderer.MeasureText(property.ParcelAddress, address.Font).Width + 8;
                var legalWidth = TextRenderer.MeasureText(property.LegalDescription, legal.Font).Width + 8;
                if (address.ClientSize.Width < addressWidth)
                    problems.Add("Address does not fit.");
                if (legal.ClientSize.Width < legalWidth)
                    problems.Add("Legal description does not fit.");

                Assert.That(dialog.AcceptButton, Is.InstanceOf<ThmsButton>());
                Assert.That(dialog.CancelButton, Is.InstanceOf<ThmsButton>());
                Assert.That(All(dialog).OfType<ThmsButton>().Select(button => button.Text),
                    Is.EquivalentTo(new[] { "Save", "Cancel", "Add", "Remove" }));
                AssertFullyInside(dialog, (Control)dialog.AcceptButton!);
                AssertFullyInside(dialog, (Control)dialog.CancelButton!);
                foreach (var button in All(dialog).OfType<ThmsButton>())
                {
                    AssertFullyInside(dialog, button);
                    Assert.That(button.Parent!.ClientRectangle.Contains(button.Bounds), Is.True,
                        $"{button.Text} is cropped by its parent.");
                }

                var heading = All(dialog).OfType<Label>().Single(label => label.Text == "Jurisdictions");
                AssertFullyInside(dialog, heading);
                var grid = All(dialog).OfType<DataGridView>().Single();
                Assert.That(grid.Height, Is.GreaterThan(80));
                var before = grid.Height;
                dialog.ClientSize = new Size(dialog.ClientSize.Width, dialog.ClientSize.Height + 180);
                dialog.PerformLayout();
                Assert.That(grid.Height, Is.GreaterThan(before + 120));

                var add = All(dialog).OfType<ThmsButton>().Single(button => button.Text == "Add");
                var total = All(dialog).OfType<Label>().Single(label => label.Text == "Total taxes due");
                var addBottom = dialog.PointToClient(add.PointToScreen(new Point(0, add.Height))).Y;
                var totalTop = dialog.PointToClient(total.PointToScreen(Point.Empty)).Y;
                Assert.That(totalTop, Is.GreaterThanOrEqualTo(addBottom - 4));
                Assert.That(problems, Is.Empty);
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void AssertFullyInside(Form dialog, Control control)
        {
            var origin = dialog.PointToClient(control.PointToScreen(Point.Empty));
            var bounds = new Rectangle(origin, control.Size);
            Assert.That(Rectangle.Inflate(dialog.ClientRectangle, -4, -4).Contains(bounds), Is.True,
                $"{control.Text} is cropped by the dialog.");
        }

        private static string Describe(Control control) =>
            string.IsNullOrWhiteSpace(control.Text) ? control.GetType().Name : control.Text;

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
