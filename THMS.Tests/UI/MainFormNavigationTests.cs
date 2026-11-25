using THMS.UI;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class MainFormNavigationTests
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Dashboards_PutNetWorthAboveFinance()
        {
            using var form = new MainForm();
            form.LoadModules();

            var navigation = All(form).OfType<FlowLayoutPanel>().Single(panel => panel.Name == "navigationPanel");
            var buttons = navigation.Controls.OfType<Button>().Select(button => button.Text).ToArray();

            Assert.That(buttons.Take(2).ToArray(), Is.EqualTo(new[] { "Net Worth", "Finance" }));
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
