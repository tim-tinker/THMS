using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
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
