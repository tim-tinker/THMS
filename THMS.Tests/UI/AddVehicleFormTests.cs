using THMS.UI.WinForms;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class AddVehicleFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void VehicleGrid_HasNoVehicleEditor()
        {
            using var form = new VehicleListDashboardForm();
            form.Show();
            try
            {
                var grids = All(form).OfType<DataGridView>().ToList();
                Assert.That(grids, Is.Not.Empty);
                Assert.That(grids.All(grid => grid.ContextMenuStrip is null), Is.True);
                Assert.That(All(form).OfType<Button>().Any(button => button.Text.Contains("Add")), Is.False);
                Assert.That(All(form).OfType<Button>().Any(button => button.Text == "Edit"), Is.False);
            }
            finally
            {
                form.Close();
            }
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
