using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels;
using THMS.UI.WinForms;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class AddVehicleFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void EditDialog_ShowsSavedVehicleAndUpdatesIt()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ev = vehicles.GetAllVehicles().OfType<VehicleEv>().Single();
            var vm = new VehicleListViewModel(vehicles, new InMemoryFinanceDataStore());
            vm.Initialize();
            var count = vm.Vehicles.Count;

            using var host = new Form();
            var grid = new DataGridView { Dock = DockStyle.Fill, Parent = host };
            grid.DataSource = vm.Vehicles;
            host.Show();

            using var dialog = new AddVehicleForm(vm, ev);
            dialog.Show();
            try
            {
                Assert.That(dialog.Text, Is.EqualTo("Edit Vehicle"));
                Assert.That(Field<TextBox>(dialog, "txtName").Text, Is.EqualTo(ev.Name));
                Assert.That(Field<TextBox>(dialog, "txtMake").Text, Is.EqualTo(ev.Make));
                Assert.That(Field<TextBox>(dialog, "txtModel").Text, Is.EqualTo(ev.Model));
                Assert.That(Field<NumericUpDown>(dialog, "numYear").Value, Is.EqualTo(ev.Year));
                Assert.That(Field<CheckBox>(dialog, "_checkEv").Checked, Is.True);
                Assert.That(Field<NumericUpDown>(dialog, "_numericFuelCapacity").Value, Is.EqualTo(ev.BatteryCapacityKwh));
                Assert.That(Field<Label>(dialog, "_labelFuelType").Text, Is.EqualTo("kiloWatt-hours"));

                Field<TextBox>(dialog, "txtName").Text = "Daily Driver";
                Field<NumericUpDown>(dialog, "_numericFuelCapacity").Value = 88;
                Field<Button>(dialog, "btnSave").PerformClick();
            }
            finally
            {
                if (!dialog.IsDisposed)
                    dialog.Close();
            }

            Assert.That(vm.Vehicles, Has.Count.EqualTo(count));
            Assert.That(vm.Vehicles.Single(item => item.VehicleId == ev.Id).Name, Is.EqualTo("Daily Driver"));
            var name = grid.Rows.Cast<DataGridViewRow>()
                .Single(row => row.DataBoundItem is VehicleListItemViewModel item && item.VehicleId == ev.Id)
                .Cells[nameof(VehicleListItemViewModel.Name)].Value;
            Assert.That(name, Is.EqualTo("Daily Driver"));
            host.Close();
            var stored = vehicles.GetVehicle(ev.Id) as VehicleEv;
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Name, Is.EqualTo("Daily Driver"));
            Assert.That(stored.BatteryCapacityKwh, Is.EqualTo(88));
            Assert.That(stored.Vin, Is.EqualTo(ev.Vin));
            Assert.That(stored.ChargePortType, Is.EqualTo(ev.ChargePortType));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void EditDialog_ShowsIceFuelCapacity()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var ice = vehicles.GetAllVehicles().OfType<VehicleIce>().Single();
            ice.FuelTankCapacityGallons = 14.5m;
            ice.FuelType = "Diesel";
            vehicles.UpsertVehicle(ice);
            var vm = new VehicleListViewModel(vehicles, new InMemoryFinanceDataStore());

            using var dialog = new AddVehicleForm(vm, ice);
            dialog.Show();
            try
            {
                Assert.That(Field<CheckBox>(dialog, "_checkEv").Checked, Is.False);
                Assert.That(Field<NumericUpDown>(dialog, "_numericFuelCapacity").Value, Is.EqualTo(14.5m));
                Assert.That(Field<Label>(dialog, "_labelFuelType").Text, Is.EqualTo("gallons"));

                Field<TextBox>(dialog, "txtModel").Text = "Escape";
                Field<Button>(dialog, "btnSave").PerformClick();
            }
            finally
            {
                if (!dialog.IsDisposed)
                    dialog.Close();
            }

            var stored = vehicles.GetVehicle(ice.Id) as VehicleIce;
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Model, Is.EqualTo("Escape"));
            Assert.That(stored.FuelTankCapacityGallons, Is.EqualTo(14.5m));
            Assert.That(stored.FuelType, Is.EqualTo("Diesel"));
            Assert.That(stored.Vin, Is.EqualTo(ice.Vin));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void VehicleGrid_EditMenuOpensOnTheVehicleList()
        {
            using var form = new VehicleListDashboardForm();
            form.Show();
            try
            {
                var grid = All(form).OfType<DataGridView>().Single(candidate =>
                    candidate.ContextMenuStrip?.Items.OfType<ToolStripMenuItem>().Any(item => item.Text == "Edit") == true);
                Assert.That(grid.Dock, Is.EqualTo(DockStyle.Fill));
                Assert.That(grid.ContextMenuStrip!.Items[0], Is.InstanceOf<ToolStripMenuItem>());
                Assert.That(grid.ContextMenuStrip.Items[0].Text, Is.EqualTo("Edit"));
                Assert.That(All(form).OfType<DataGridView>().Count(candidate => candidate.ContextMenuStrip is not null),
                    Is.EqualTo(1));
            }
            finally
            {
                form.Close();
            }
        }

        private static T Field<T>(Control root, string name) where T : Control =>
            All(root).OfType<T>().Single(control => control.Name == name);

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
