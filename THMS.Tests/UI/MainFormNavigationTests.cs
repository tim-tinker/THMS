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

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Planning_FollowsDataManagement_AndOpensCalculatorsFromTheRail()
        {
            using var form = new MainForm();
            form.LoadModules();

            var navigation = All(form).OfType<FlowLayoutPanel>().Single(panel => panel.Name == "navigationPanel");
            var texts = navigation.Controls.Cast<Control>().Select(control => control.Text).ToList();
            var planning = texts.IndexOf("Planning");

            Assert.That(texts.IndexOf("Data Management"), Is.LessThan(planning));
            Assert.That(texts[planning - 1], Is.EqualTo("Income"));
            Assert.That(texts[planning - 2], Is.EqualTo("Assets"));
            Assert.That(texts[planning + 1], Is.EqualTo("401(k)"));
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
