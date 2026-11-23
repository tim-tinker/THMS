using THMS.Domain.Transportation;
using THMS.UI.WinForms;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class EvChargeSessionFormTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void CalculatedCheckbox_FillsBatteryKwhFromStartAndEndSoc()
        {
            var ev = new VehicleEv
            {
                Id = Guid.NewGuid(),
                Name = "Fit",
                Make = "Ford",
                Model = "Mach-E",
                Year = 2023,
                ChargeEnergy = new SocEnergyCoefficients(0.8m, 0, 0)
            };
            var session = new CommercialEvChargeSession
            {
                Id = Guid.NewGuid(),
                VehicleId = ev.Id,
                VehicleName = ev.Name,
                StartTime = DateTime.Today,
                EndTime = DateTime.Today.AddHours(1),
                StartSoc = 20,
                EndSoc = 60,
                KwhAdded = 0
            };

            using var form = new EvChargeSessionForm(ev, session);
            form.Show();
            try
            {
                var kwh = Field<NumericUpDown>(form, "_numBatteryKwhAdded");
                var box = Field<CheckBox>(form, "_checkKwhCalculated");
                Assert.That(kwh.Value, Is.EqualTo(0m));

                box.Checked = true;
                Application.DoEvents();

                Assert.That(box.Checked, Is.True);
                Assert.That(kwh.Value, Is.EqualTo(32m));

                kwh.Value = 10;
                Assert.That(box.Checked, Is.False);
                Assert.That(kwh.Value, Is.EqualTo(10m));
            }
            finally
            {
                if (!form.IsDisposed)
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
