using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class VehicleValuationDialogTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void VehicleValuation_UsesVehicleValuesAndFits()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var car = new VehicleIce
            {
                Year = 2023,
                Make = "Ford",
                Model = "Mustang Mach-E",
                Vin = "1FMCU0G6XPUA12345",
                Name = "Mach-E"
            };
            vehicles.UpsertVehicle(car);
            var orchestrator = Orchestrator(vehicles);
            var asset = new VehicleAsset { Name = "Mach-E", Category = "Vehicle", HowAcquired = "Lease" };
            orchestrator.SaveAsset(asset, null, car);

            using var dialog = new VehicleValuationDialog(orchestrator, asset, null);
            dialog.Show();
            try
            {
                dialog.Refresh();
                Assert.That(dialog.Text, Is.EqualTo("Add Vehicle Valuation"));
                Assert.That(dialog.AcceptButton, Is.InstanceOf<ThmsButton>());
                Assert.That(dialog.CancelButton, Is.InstanceOf<ThmsButton>());
                var values = ValueList(dialog);
                Assert.That(values.Items, Is.EqualTo(ValuationTypes.Vehicle));
                Assert.That(values.SelectedItem, Is.EqualTo(ValuationTypes.Market));
                Assert.That(values.Items, Does.Not.Contain(ValuationTypes.LandMarket));
                Assert.That(values.Items, Does.Not.Contain(ValuationTypes.HomesteadCap));
                AssertLayout(dialog);
                Assert.That(All(dialog).OfType<TextBox>().Any(box => box.Text == car.Vin), Is.True);

                Amount(dialog).Value = 30000m;
                Source(dialog).Text = "Kelley Blue Book";
                ((ThmsButton)dialog.AcceptButton!).PerformClick();
            }
            finally
            {
                if (!dialog.IsDisposed)
                    dialog.Close();
            }

            var saved = orchestrator.GetValuations(asset.Id).Single();
            Assert.That(saved.ValueType, Is.EqualTo(ValuationTypes.Market));
            Assert.That(saved.Amount, Is.EqualTo(30000m));
            Assert.That(saved.Source, Is.EqualTo("Kelley Blue Book"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PersonalValuation_UsesPersonalValuesAndFits()
        {
            var orchestrator = Orchestrator(new InMemoryVehicleDataStore());
            var piano = new PersonalAsset
            {
                Name = "Piano",
                Category = "Instrument",
                HowAcquired = "Gift",
                CostBasis = 1200m
            };
            orchestrator.SaveAsset(piano, null, null);

            using var dialog = new PersonalValuationDialog(orchestrator, piano, null);
            dialog.Show();
            try
            {
                dialog.Refresh();
                Assert.That(dialog.Text, Is.EqualTo("Add Personal Property Valuation"));
                var values = ValueList(dialog);
                Assert.That(values.Items, Is.EqualTo(ValuationTypes.Personal));
                Assert.That(values.SelectedItem, Is.EqualTo(ValuationTypes.Market));
                Assert.That(values.Items, Does.Not.Contain(ValuationTypes.TradeIn));
                Assert.That(values.Items, Does.Not.Contain(ValuationTypes.LandMarket));
                AssertLayout(dialog);
            }
            finally
            {
                dialog.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void ValuationDialog_OffersSourcesForThatAssetAndCanOpenTheWebsite()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var details = new VehicleIce { Year = 2023, Make = "Ford", Model = "Mustang Mach-E", Vin = "1FMCU0G6XPUA12345", Name = "Mach-E" };
            vehicles.UpsertVehicle(details);
            var orchestrator = Orchestrator(vehicles);
            orchestrator.SaveValuationSource(new ValuationSource
            {
                Name = "Fort Bend CAD",
                WebsiteUrl = "https://www.fbcad.org",
                Kinds = AppraisedAssetKind.Property
            });
            orchestrator.SaveValuationSource(new ValuationSource
            {
                Name = "Kelley Blue Book",
                WebsiteUrl = "https://www.kbb.com",
                Kinds = AppraisedAssetKind.Vehicle
            });
            var car = new VehicleAsset { Name = "Mach-E", Category = "Vehicle" };
            orchestrator.SaveAsset(car, null, details);
            var house = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(house, null, null);

            using var vehicle = new VehicleValuationDialog(orchestrator, car, null);
            vehicle.Show();
            try
            {
                var source = Source(vehicle);
                Assert.That(source.Items.Cast<object>().Select(item => item.ToString()).ToArray(),
                    Is.EqualTo(new[] { "Kelley Blue Book" }));
                source.SelectedItem = "Kelley Blue Book";
                vehicle.Refresh();
                var link = All(vehicle).OfType<LinkLabel>().Single();
                Assert.That(link.Text, Is.EqualTo("Kelley Blue Book"));
                Assert.That(link.Visible, Is.True);
                Assert.That(link.Width, Is.GreaterThan(40));
                Assert.That(link.Right, Is.LessThanOrEqualTo(source.Right - SystemInformation.VerticalScrollBarWidth + 4));
                source.Text = "County tax notice";
                Assert.That(link.Visible, Is.False);
            }
            finally
            {
                vehicle.Close();
            }

            using var property = new PropertyValuationDialog(orchestrator, house, null);
            property.Show();
            try
            {
                var source = Source(property);
                Assert.That(source.Items.Cast<object>().Select(item => item.ToString()).ToArray(),
                    Is.EqualTo(new[] { "Fort Bend CAD" }));
                source.SelectedItem = "Fort Bend CAD";
                property.Refresh();
                Assert.That(All(property).OfType<LinkLabel>().Single().Text, Is.EqualTo("Fort Bend CAD"));
            }
            finally
            {
                property.Close();
            }
        }

        private static void AssertLayout(Form dialog)
        {
            var problems = new List<string>();
            foreach (var label in All(dialog).OfType<Label>().Where(label => label.Visible && label.Text.Length > 0))
            {
                var needed = TextRenderer.MeasureText(label.Text, label.Font);
                if (label.ClientSize.Width < needed.Width - 4 || label.ClientSize.Height < needed.Height - 2)
                    problems.Add($"'{label.Text}' is cropped.");
            }

            foreach (var group in All(dialog).Where(control => control.Visible && control.Parent is not null).GroupBy(control => control.Parent))
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

            Assert.That(problems, Is.Empty);
            AssertFullyInside(dialog, (Control)dialog.AcceptButton!);
            AssertFullyInside(dialog, (Control)dialog.CancelButton!);
        }

        private static void AssertFullyInside(Form dialog, Control control)
        {
            var origin = dialog.PointToClient(control.PointToScreen(Point.Empty));
            var bounds = new Rectangle(origin, control.Size);
            var inset = Rectangle.Inflate(dialog.ClientRectangle, -4, -4);
            Assert.That(inset.Contains(bounds), Is.True, $"{control.Text} is cropped by the dialog.");
        }

        private static ComboBox ValueList(Form dialog) =>
            All(dialog).OfType<ComboBox>().Single(combo => combo.DropDownStyle == ComboBoxStyle.DropDownList);

        private static NumericUpDown Amount(Form dialog) =>
            All(dialog).OfType<NumericUpDown>().Single();

        private static ComboBox Source(Form dialog) =>
            All(dialog).OfType<ComboBox>().Single(combo => combo.DropDownStyle == ComboBoxStyle.DropDown);

        private static string Describe(Control control) =>
            string.IsNullOrWhiteSpace(control.Text) ? control.GetType().Name : control.Text;

        private static AssetOrchestrator Orchestrator(InMemoryVehicleDataStore vehicles) =>
            new(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                vehicles);

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
