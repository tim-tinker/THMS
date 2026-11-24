using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class AssetEditorDialogTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void PropertyFields_AreFullyVisibleAndDoNotOverlap()
        {
            var accounts = new InMemoryAccountDataStore();
            accounts.UpsertAccount(new MortgageAccount
            {
                Name = "Mortgage: RoundPoint",
                Institution = "RoundPoint",
                AccountNumber = "1",
                WebsiteUrl = "",
                Type = AccountType.Mortgage,
                NextPaymentDate = DateTime.Today
            });
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                accounts,
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                new InMemoryVehicleDataStore());

            using var dialog = new AssetEditorDialog(orchestrator, null);
            SelectKind(dialog, AssetKinds.Property);
            dialog.Show();
            try
            {
                dialog.Refresh();
                const string address = "6219 Cresting Knolls Cir, Richmond, TX 77407";
                const string legal = "Long Meadow Farms Sec 32, Block 1, Lot 32";
                Field(dialog, "Address").Text = address;
                Field(dialog, "Legal description").Text = legal;
                dialog.Refresh();
                AssertLayout(dialog, "Property");
                AssertTextFits(Field(dialog, "Address"), address);
                AssertTextFits(Field(dialog, "Legal description"), legal);
                Assert.That(dialog.FormBorderStyle, Is.EqualTo(FormBorderStyle.Sizable));

                var grown = new Size(dialog.ClientSize.Width + 120, dialog.ClientSize.Height + 80);
                dialog.ClientSize = grown;
                dialog.Refresh();
                Assert.That(dialog.ClientSize, Is.EqualTo(grown));
                AssertLayout(dialog, "Property resized");

                SelectKind(dialog, AssetKinds.Vehicle);
                dialog.Refresh();
                AssertLayout(dialog, "Vehicle");
            }
            finally
            {
                dialog.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void VehicleFuelCapacity_SavesGallonsOrKilowattHoursOnTheSameVehicle()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ice = vehicles.GetAllVehicles().OfType<VehicleIce>().Single();
            ice.FuelTankCapacityGallons = 14.5m;
            ice.FuelType = "Gasoline";
            vehicles.UpsertVehicle(ice);
            var orchestrator = new AssetOrchestrator(
                new InMemoryAssetDataStore(),
                new InMemoryAccountDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryTransactionDataStore(),
                vehicles);
            var asset = new VehicleAsset { Name = ice.Name, Category = "Vehicle" };
            orchestrator.SaveAsset(asset, null, ice);

            using var dialog = new AssetEditorDialog(orchestrator, orchestrator.GetAsset(asset.Id));
            dialog.Show();
            try
            {
                Assert.That(Unit(dialog).Text, Is.EqualTo("gallons"));
                Assert.That(Capacity(dialog).Value, Is.EqualTo(14.5m));
                Assert.That(Unit(dialog).Left, Is.GreaterThan(Capacity(dialog).Right - 2));

                Capacity(dialog).Value = 16;
                Powertrain(dialog).SelectedItem = "EV";
                Assert.That(Unit(dialog).Text, Is.EqualTo("kWh"));
                Capacity(dialog).Value = 88;
                AllControls(dialog).OfType<ThmsButton>().Single(button => button.Text == "Save").PerformClick();
            }
            finally
            {
                if (!dialog.IsDisposed)
                    dialog.Close();
            }

            var stored = vehicles.GetVehicle(ice.Id);
            Assert.That(stored, Is.InstanceOf<VehicleEv>());
            var ev = (VehicleEv)stored!;
            Assert.That(ev.Id, Is.EqualTo(ice.Id));
            Assert.That(ev.BatteryCapacityKwh, Is.EqualTo(88m));
            Assert.That(ev.Vin, Is.EqualTo(ice.Vin));
            Assert.That(ev.ChargePortType, Is.EqualTo("J1772"));
        }

        private static void AssertLayout(Form dialog, string kind)
        {
            Assert.That(dialog.AcceptButton, Is.InstanceOf<ThmsButton>());
            Assert.That(dialog.CancelButton, Is.InstanceOf<ThmsButton>());

            var visible = AllControls(dialog).Where(control => control.Visible).ToList();
            var problems = new List<string>();
            foreach (var label in visible.OfType<Label>())
            {
                var needed = TextRenderer.MeasureText(label.Text, label.Font).Width;
                if (label.ClientSize.Width < needed - 2)
                    problems.Add($"{kind} label '{label.Text}' is clipped.");
            }

            foreach (var group in visible.Where(control => control.Parent is not null).GroupBy(control => control.Parent))
            {
                var siblings = group.Where(control => control.Width > 0 && control.Height > 0).ToList();
                for (var i = 0; i < siblings.Count; i++)
                {
                    for (var j = i + 1; j < siblings.Count; j++)
                    {
                        var overlap = Rectangle.Intersect(siblings[i].Bounds, siblings[j].Bounds);
                        if (overlap.Width > 2 && overlap.Height > 2)
                            problems.Add($"{kind}: '{Describe(siblings[i])}' overlaps '{Describe(siblings[j])}'.");
                    }
                }
            }

            Assert.That(problems, Is.Empty);
            AssertFullyInside(dialog, (Control)dialog.AcceptButton!);
            AssertFullyInside(dialog, (Control)dialog.CancelButton!);
        }

        private static void AssertTextFits(TextBox box, string text)
        {
            var needed = TextRenderer.MeasureText(text, box.Font).Width + 8;
            Assert.That(box.ClientSize.Width, Is.GreaterThanOrEqualTo(needed),
                $"'{text}' does not fit in the field.");
        }

        private static void AssertFullyInside(Form dialog, Control control)
        {
            var origin = dialog.PointToClient(control.PointToScreen(Point.Empty));
            var bounds = new Rectangle(origin, control.Size);
            var inset = Rectangle.Inflate(dialog.ClientRectangle, -4, -4);
            Assert.That(inset.Contains(bounds), Is.True, $"{control.Text} is cropped by the dialog.");
            Assert.That(control.Parent!.ClientRectangle.Contains(control.Bounds), Is.True,
                $"{control.Text} is cropped by its parent.");
        }

        private static NumericUpDown Capacity(Form dialog) =>
            AllControls(AllControls(dialog).OfType<Label>().Single(label => label.Text == "Fuel capacity").Parent!)
                .OfType<NumericUpDown>().Single();

        private static Label Unit(Form dialog) =>
            Capacity(dialog).Parent!.Controls.OfType<Label>().Single();

        private static ComboBox Powertrain(Form dialog) =>
            AllControls(dialog).OfType<Label>().Single(label => label.Text == "Powertrain")
                .Parent!.Controls.OfType<ComboBox>().Single();

        private static TextBox Field(Form dialog, string caption)
        {
            var label = AllControls(dialog).OfType<Label>().First(item => item.Text == caption);
            return label.Parent!.Controls.OfType<TextBox>().First();
        }

        private static void SelectKind(Form dialog, string kind)
        {
            var combo = AllControls(dialog).OfType<ComboBox>()
                .First(item => item.Items.Contains(kind));
            combo.SelectedItem = kind;
        }

        private static string Describe(Control control) =>
            string.IsNullOrWhiteSpace(control.Text) ? control.GetType().Name : control.Text;

        private static IEnumerable<Control> AllControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var nested in AllControls(child))
                    yield return nested;
            }
        }
    }
}
