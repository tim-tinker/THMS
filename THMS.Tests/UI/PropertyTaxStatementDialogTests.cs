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

        [Test]
        [Apartment(ApartmentState.STA)]
        public void AppraisedValues_FollowTheAssessmentMethod()
        {
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);

            using var dialog = new PropertyTaxStatementDialog(orchestrator, null, null, property);
            dialog.Show();
            try
            {
                Editor(dialog, "Land market").Value = 84500m;
                Editor(dialog, "Improvement market").Value = 542600m;

                var total = Editor(dialog, "Total market");
                var assessed = Editor(dialog, "Assessed");
                Assert.That(total.ReadOnly, Is.True);
                Assert.That(assessed.ReadOnly, Is.True);
                Assert.That(Editor(dialog, "Land market").ReadOnly, Is.False);
                Assert.That(total.Value, Is.EqualTo(627100m));
                Assert.That(assessed.Value, Is.EqualTo(627100m));

                Editor(dialog, "Less homestead").Value = 27000m;
                Assert.That(assessed.Value, Is.EqualTo(600100m));
                Assert.That(total.ReadOnly, Is.True);

                Method(dialog).SelectedItem = AssessmentMethods.EqualMarketValue;
                Assert.That(Label(dialog, "Less homestead").Visible, Is.False);
                Assert.That(assessed.Value, Is.EqualTo(627100m));
                Assert.That(assessed.ReadOnly, Is.True);

                Method(dialog).SelectedItem = AssessmentMethods.AssessmentRatio;
                var ratio = Editor(dialog, "Assessment ratio %");
                Assert.That(ratio.Visible, Is.True);
                ratio.Value = 40m;
                Assert.That(assessed.Value, Is.EqualTo(250840m));
                Assert.That(total.ReadOnly, Is.True);
                Assert.That(assessed.ReadOnly, Is.True);

                Method(dialog).SelectedItem = AssessmentMethods.Manual;
                Assert.That(total.ReadOnly, Is.False);
                Assert.That(assessed.ReadOnly, Is.False);
                Assert.That(Label(dialog, "Less homestead").Visible, Is.True);
                total.Value = 1000m;
                assessed.Value = 800m;
                Assert.That(total.Value, Is.EqualTo(1000m));
                Assert.That(assessed.Value, Is.EqualTo(800m));
            }
            finally
            {
                dialog.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void JurisdictionLines_CalculateTaxableValueAndTaxes()
        {
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);

            using var dialog = new PropertyTaxStatementDialog(orchestrator, null, null, property);
            dialog.Show();
            try
            {
                Editor(dialog, "Land market").Value = 84500m;
                Editor(dialog, "Improvement market").Value = 542600m;

                var grid = All(dialog).OfType<DataGridView>().Single();
                SetLine(grid, "ExemptionAmount", 145000m);
                SetLine(grid, "RatePerHundred", 1.139m);

                Assert.That(grid.Columns["TaxableValue"]!.ReadOnly, Is.True);
                Assert.That(grid.Columns["TaxAmount"]!.ReadOnly, Is.True);
                Assert.That(grid.Columns["ExemptionAmount"]!.ReadOnly, Is.False);
                Assert.That(grid.Columns["RatePerHundred"]!.HeaderText, Is.EqualTo("Rate per $100"));
                Assert.That(Line(grid, "TaxableValue"), Is.EqualTo(482100m));
                Assert.That(Cell(grid, "TaxableValue"), Is.EqualTo(482100m));
                Assert.That(Cell(grid, "TaxAmount"), Is.EqualTo(5491.12m));

                Levy(dialog).SelectedItem = LevyMethods.Millage;
                Assert.That(grid.Columns["RatePerHundred"]!.HeaderText, Is.EqualTo("Mills"));
                SetLine(grid, "RatePerHundred", 10m);
                Assert.That(Cell(grid, "TaxAmount"), Is.EqualTo(4821m));

                Levy(dialog).SelectedItem = LevyMethods.Manual;
                Assert.That(grid.Columns["TaxableValue"]!.ReadOnly, Is.False);
                Assert.That(grid.Columns["TaxAmount"]!.ReadOnly, Is.False);
                SetLine(grid, "TaxableValue", 10m);
                SetLine(grid, "TaxAmount", 3m);
                Assert.That(Cell(grid, "TaxableValue"), Is.EqualTo(10m));
                Assert.That(Cell(grid, "TaxAmount"), Is.EqualTo(3m));
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

        private static Label Label(Control dialog, string caption) =>
            All(dialog).OfType<Label>().Single(label => label.Text == caption);

        private static NumericUpDown Editor(Control dialog, string caption)
        {
            var row = Label(dialog, caption).Parent!;
            return All(row).OfType<NumericUpDown>().Single(box => box.Visible);
        }

        private static ComboBox Method(Control dialog) =>
            All(dialog).OfType<ComboBox>().Single(combo => combo.Items.Contains(AssessmentMethods.Manual));

        private static ComboBox Levy(Control dialog) =>
            All(dialog).OfType<ComboBox>().Single(combo => combo.Items.Contains(LevyMethods.Millage));

        private static void SetLine(DataGridView grid, string property, decimal value) =>
            grid.Rows[0].DataBoundItem!.GetType().GetProperty(property)!.SetValue(grid.Rows[0].DataBoundItem, value);

        private static decimal Line(DataGridView grid, string property) =>
            (decimal)grid.Rows[0].DataBoundItem!.GetType().GetProperty(property)!.GetValue(grid.Rows[0].DataBoundItem)!;

        private static decimal Cell(DataGridView grid, string property) =>
            Convert.ToDecimal(grid.Rows[0].Cells[property].Value);

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
