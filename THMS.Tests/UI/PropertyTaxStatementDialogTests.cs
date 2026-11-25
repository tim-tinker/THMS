using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
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
                Assert.That(address.ReadOnly, Is.True);
                Assert.That(legal.ReadOnly, Is.True);
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
            var orchestrator = Orchestrator();
            var homestead = new PropertyAsset
            {
                Name = "Casa Tinker",
                Category = "Real estate",
                ParcelAddress = "6219 Cresting Knolls Cir, Richmond, TX 77407"
            };
            orchestrator.SaveAsset(homestead, null, null);
            using (var dialog = new PropertyValuationDialog(orchestrator, homestead, null))
            {
                dialog.Show();
                Assert.That(dialog.Text, Is.EqualTo("Add Real Estate Valuation"));
                var propertyHeading = Label(dialog, "Real Estate Property");
                var valuesHeading = Label(dialog, "Appraised values");
                Assert.That(propertyHeading.ClientSize.Height, Is.GreaterThanOrEqualTo(TextRenderer.MeasureText(propertyHeading.Text, propertyHeading.Font).Height));
                Assert.That(valuesHeading.ClientSize.Height, Is.GreaterThanOrEqualTo(TextRenderer.MeasureText(valuesHeading.Text, valuesHeading.Font).Height));
                Editor(dialog, "Land market").Value = 84500m;
                Editor(dialog, "Improvement market").Value = 542600m;
                var total = Editor(dialog, "Total market");
                var assessed = Editor(dialog, "Assessed");
                Assert.That(total.ReadOnly, Is.True);
                Assert.That(assessed.ReadOnly, Is.True);
                Assert.That(Editor(dialog, "Land market").ReadOnly, Is.False);
                Assert.That(total.Value, Is.EqualTo(627100m));
                Assert.That(assessed.Value, Is.EqualTo(627100m));
                AssertFullyInside(dialog, assessed);
                Assert.That(TextBox(dialog, homestead.Name).ReadOnly, Is.True);
                Assert.That(TextBox(dialog, homestead.ParcelAddress).ReadOnly, Is.True);

                Editor(dialog, "Less homestead").Value = 27000m;
                Assert.That(assessed.Value, Is.EqualTo(600100m));
                dialog.Close();
            }

            var equal = homestead;
            equal.AssessmentMethod = AssessmentMethod.EqualMarketValue;
            using (var dialog = new PropertyValuationDialog(orchestrator, equal, null))
            {
                dialog.Show();
                Editor(dialog, "Land market").Value = 84500m;
                Editor(dialog, "Improvement market").Value = 542600m;
                Assert.That(Label(dialog, "Less homestead").Visible, Is.False);
                Assert.That(Editor(dialog, "Assessed").Value, Is.EqualTo(627100m));
                Assert.That(Editor(dialog, "Assessed").ReadOnly, Is.True);
                dialog.Close();
            }

            homestead.AssessmentMethod = AssessmentMethod.AssessmentRatio;
            using (var dialog = new PropertyValuationDialog(orchestrator, homestead, null))
            {
                dialog.Show();
                Editor(dialog, "Land market").Value = 84500m;
                Editor(dialog, "Improvement market").Value = 542600m;
                var ratio = Editor(dialog, "Assessment ratio %");
                Assert.That(ratio.Visible, Is.True);
                ratio.Value = 40m;
                Assert.That(Editor(dialog, "Assessed").Value, Is.EqualTo(250840m));
                Assert.That(Editor(dialog, "Total market").ReadOnly, Is.True);
                Assert.That(Editor(dialog, "Assessed").ReadOnly, Is.True);
                dialog.Close();
            }

            homestead.AssessmentMethod = AssessmentMethod.Manual;
            using (var dialog = new PropertyValuationDialog(orchestrator, homestead, null))
            {
                dialog.Show();
                var total = Editor(dialog, "Total market");
                var assessed = Editor(dialog, "Assessed");
                Assert.That(total.ReadOnly, Is.False);
                Assert.That(assessed.ReadOnly, Is.False);
                Assert.That(Label(dialog, "Less homestead").Visible, Is.True);
                total.Value = 1000m;
                assessed.Value = 800m;
                Assert.That(total.Value, Is.EqualTo(1000m));
                Assert.That(assessed.Value, Is.EqualTo(800m));
                dialog.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void NewValuation_StartsFromTheLatestAppraisal()
        {
            var orchestrator = Orchestrator();
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);
            orchestrator.SavePropertyAppraisal(property, new PropertyAppraisal
            {
                AsOf = new DateTime(2024, 5, 1),
                Source = "Old notice",
                LandMarketValue = 10000m,
                ImprovementMarketValue = 20000m
            });
            orchestrator.SavePropertyAppraisal(property, new PropertyAppraisal
            {
                AsOf = new DateTime(2026, 5, 1),
                Source = "Fort Bend CAD",
                LandMarketValue = 84500m,
                ImprovementMarketValue = 542600m,
                HomesteadCap = 27000m
            });

            using var dialog = new PropertyValuationDialog(orchestrator, property, null);
            dialog.Show();
            try
            {
                var anniversary = new DateTime(2027, 5, 1);
                var expected = anniversary < DateTime.Today ? anniversary : DateTime.Today;
                Assert.That(dialog.Text, Is.EqualTo("Add Real Estate Valuation"));
                Assert.That(All(dialog).OfType<DateTimePicker>().Single().Value.Date, Is.EqualTo(expected));
                Assert.That(
                    All(dialog).OfType<ComboBox>().Single(combo => combo.DropDownStyle == ComboBoxStyle.DropDown).Text,
                    Is.EqualTo("Fort Bend CAD"));
                Assert.That(Editor(dialog, "Land market").Value, Is.EqualTo(84500m));
                Assert.That(Editor(dialog, "Improvement market").Value, Is.EqualTo(542600m));
                Assert.That(Editor(dialog, "Less homestead").Value, Is.EqualTo(27000m));
                Assert.That(Editor(dialog, "Assessed").Value, Is.EqualTo(600100m));

                ((Button)dialog.AcceptButton!).PerformClick();
                var saved = orchestrator.GetPropertyAppraisals(property.Id);
                Assert.That(saved, Has.Count.EqualTo(3));
                Assert.That(saved.Single(item => item.AsOf == expected && item.LandMarketValue == 84500m).Source, Is.EqualTo("Fort Bend CAD"));
                Assert.That(saved.Single(item => item.AsOf == new DateTime(2026, 5, 1)).LandMarketValue, Is.EqualTo(84500m));
                Assert.That(saved.Single(item => item.AsOf == new DateTime(2024, 5, 1)).LandMarketValue, Is.EqualTo(10000m));
            }
            finally
            {
                dialog.Close();
            }

            var older = new PropertyAsset { Name = "Older house", Category = "Real estate" };
            orchestrator.SaveAsset(older, null, null);
            orchestrator.SavePropertyAppraisal(older, new PropertyAppraisal
            {
                AsOf = new DateTime(2020, 3, 15),
                LandMarketValue = 50000m,
                ImprovementMarketValue = 150000m
            });
            using var prior = new PropertyValuationDialog(orchestrator, older, null);
            prior.Show();
            try
            {
                Assert.That(All(prior).OfType<DateTimePicker>().Single().Value.Date, Is.EqualTo(new DateTime(2021, 3, 15)));
                Assert.That(Editor(prior, "Land market").Value, Is.EqualTo(50000m));
            }
            finally
            {
                prior.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void JurisdictionLines_CalculateTaxableValueAndTaxes()
        {
            var orchestrator = Orchestrator();
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);
            orchestrator.SavePropertyAppraisal(property, new PropertyAppraisal
            {
                AsOf = new DateTime(2026, 10, 26),
                LandMarketValue = 84500m,
                ImprovementMarketValue = 542600m
            });

            using var dialog = new PropertyTaxStatementDialog(orchestrator, null, null, property);
            dialog.Show();
            try
            {
                Assert.That(Editor(dialog, "Land market").ReadOnly, Is.True);
                Assert.That(Editor(dialog, "Assessed").Value, Is.EqualTo(627100m));
                Assert.That(Levy(dialog).Enabled, Is.False);

                var grid = All(dialog).OfType<DataGridView>().Single();
                Assert.That(grid.Rows.Count, Is.EqualTo(0));
                All(dialog).OfType<ThmsButton>().Single(button => button.Text == "Add").PerformClick();
                SetLine(grid, "ExemptionAmount", 145000m);
                SetLine(grid, "RatePerHundred", 1.139m);

                Assert.That(grid.Columns["TaxableValue"]!.ReadOnly, Is.True);
                Assert.That(grid.Columns["TaxAmount"]!.ReadOnly, Is.True);
                Assert.That(grid.Columns["ExemptionAmount"]!.ReadOnly, Is.False);
                Assert.That(grid.Columns["RatePerHundred"]!.HeaderText, Is.EqualTo("Rate per $100"));
                Assert.That(Line(grid, "TaxableValue"), Is.EqualTo(482100m));
                Assert.That(Cell(grid, "TaxableValue"), Is.EqualTo(482100m));
                Assert.That(Cell(grid, "TaxAmount"), Is.EqualTo(5491.12m));

                All(dialog).OfType<ThmsButton>().Single(button => button.Text == "Add").PerformClick();
                Application.DoEvents();
                Assert.That(grid.Rows.Count, Is.EqualTo(2));
                Assert.That(Cell(grid, "TaxableValue"), Is.EqualTo(482100m));
                Assert.That(Convert.ToDecimal(grid.Rows[1].Cells["TaxableValue"].Value), Is.EqualTo(627100m));
                Assert.That(Convert.ToDecimal(grid.Rows[1].Cells["TaxAmount"].Value), Is.EqualTo(0m));

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

        [Test]
        [Apartment(ApartmentState.STA)]
        public void NewStatement_StartsFromThePreviousYear()
        {
            var orchestrator = Orchestrator();
            var collector = orchestrator.EnsureTaxCollector("Fort Bend Tax Office");
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);
            orchestrator.SavePropertyAppraisal(property, new PropertyAppraisal
            {
                AsOf = new DateTime(DateTime.Today.Year, 5, 1),
                LandMarketValue = 84500m,
                ImprovementMarketValue = 542600m
            });
            var older = Statement(collector.Id, property.Id, DateTime.Today.Year - 2, "Old District", 1000m, 1m);
            var previous = Statement(collector.Id, property.Id, DateTime.Today.Year - 1, "Lamar CISD", 145000m, 1.139m);
            previous.Exemptions = "Homestead, Veteran";
            previous.Jurisdictions.Add(new PropertyTaxJurisdictionLine
            {
                Name = "Fort Bend County",
                ExemptionAmount = 5000m,
                TaxableValue = 575000m,
                RatePerHundred = 0.407m,
                TaxAmount = 2340.25m
            });
            orchestrator.SavePropertyTaxStatement(property, older);
            orchestrator.SavePropertyTaxStatement(property, previous);

            using var dialog = new PropertyTaxStatementDialog(orchestrator, null, null, property);
            dialog.Show();
            try
            {
                var grid = All(dialog).OfType<DataGridView>().Single();
                var collectorLabel = All(dialog).OfType<Label>().Single(label => label.Text == "Collector");
                var collectors = All(collectorLabel.Parent!).OfType<ComboBox>().Single();
                Assert.That(collectors.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
                Assert.That(collectors.Text, Is.Empty);
                Assert.That(collectors.SelectedIndex, Is.EqualTo(-1));
                Assert.That(grid.Rows.Count, Is.EqualTo(0));
                Assert.That(All(dialog).OfType<Label>().Any(label => label.Text == "Collector name"), Is.False);

                collectors.SelectedItem = collectors.Items.Cast<object>().OfType<Account>()
                    .Single(account => account.Name == "Fort Bend Tax Office");
                Assert.That(grid.Rows.Count, Is.EqualTo(2));
                Assert.That(grid.Rows[0].Cells["Name"].Value, Is.EqualTo("Lamar CISD"));
                Assert.That(grid.Rows[1].Cells["Name"].Value, Is.EqualTo("Fort Bend County"));
                Assert.That(Line(grid, "ExemptionAmount"), Is.EqualTo(145000m));
                Assert.That(Line(grid, "RatePerHundred"), Is.EqualTo(1.139m));
                Assert.That(Line(grid, "TaxableValue"), Is.EqualTo(482100m));
                Assert.That(Line(grid, "TaxAmount"), Is.EqualTo(5491.12m));
                Assert.That(Convert.ToDecimal(grid.Rows[1].Cells["ExemptionAmount"].Value), Is.EqualTo(5000m));
                Assert.That(Convert.ToDecimal(grid.Rows[1].Cells["TaxableValue"].Value), Is.EqualTo(622100m));
                Assert.That(Convert.ToDecimal(grid.Rows[1].Cells["TaxAmount"].Value), Is.EqualTo(2531.95m));

                var mud = orchestrator.EnsureTaxCollector("Fort Bend County M.U.D. #194");
                var mudBill = Statement(mud.Id, property.Id, DateTime.Today.Year - 1, "MUD Debt Service", 0m, 0.25m);
                orchestrator.SavePropertyTaxStatement(property, mudBill);
                collectors.Items.Add(mud);
                collectors.SelectedItem = mud;
                Assert.That(grid.Rows.Count, Is.EqualTo(1));
                Assert.That(grid.Rows[0].Cells["Name"].Value, Is.EqualTo("MUD Debt Service"));

                collectors.Text = "Harris County Tax Office";
                Assert.That(collectors.SelectedIndex, Is.EqualTo(-1));
                Assert.That(grid.Rows.Count, Is.EqualTo(0));

                collectors.Focus();
                collectors.Text = "Fort Bend Tax Office";
                dialog.SelectNextControl(collectors, true, true, true, true);
                Assert.That(collectors.SelectedItem, Is.InstanceOf<Account>());
                Assert.That(((Account)collectors.SelectedItem!).Name, Is.EqualTo("Fort Bend Tax Office"));
                Assert.That(grid.Rows.Count, Is.EqualTo(2));
            }
            finally
            {
                dialog.Close();
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void StatementFromAValuation_FillsThatAppraisalAndWaitsForTheCollector()
        {
            var orchestrator = Orchestrator();
            var collector = orchestrator.EnsureTaxCollector("Fort Bend Tax Office");
            var property = new PropertyAsset { Name = "Casa Tinker", Category = "Real estate" };
            orchestrator.SaveAsset(property, null, null);
            var latest = new PropertyAppraisal
            {
                AsOf = new DateTime(2026, 5, 1),
                Source = "Fort Bend CAD",
                LandMarketValue = 84500m,
                ImprovementMarketValue = 542600m
            };
            var selected = new PropertyAppraisal
            {
                AsOf = new DateTime(2025, 5, 1),
                Source = "County notice",
                LandMarketValue = 70000m,
                ImprovementMarketValue = 500000m,
                HomesteadCap = 12000m
            };
            orchestrator.SavePropertyAppraisal(property, latest);
            orchestrator.SavePropertyAppraisal(property, selected);
            var previous = Statement(collector.Id, property.Id, DateTime.Today.Year - 1, "Lamar CISD", 145000m, 1.139m);
            orchestrator.SavePropertyTaxStatement(property, previous);

            using var dialog = new PropertyTaxStatementDialog(orchestrator, null, null, property, selected);
            dialog.Show();
            try
            {
                var valuation = All(dialog).OfType<ComboBox>().Single(combo =>
                    combo.Items.Cast<object>().Any(item => item is string text && text == "(choose a valuation)"));
                Assert.That(((PropertyAppraisal)valuation.SelectedItem!).Id, Is.EqualTo(selected.Id));
                Assert.That(valuation.GetItemText(valuation.SelectedItem), Does.Contain("County notice"));
                Assert.That(Editor(dialog, "Land market").Value, Is.EqualTo(70000m));
                Assert.That(Editor(dialog, "Improvement market").Value, Is.EqualTo(500000m));
                Assert.That(Editor(dialog, "Less homestead").Value, Is.EqualTo(12000m));
                Assert.That(Editor(dialog, "Total market").Value, Is.EqualTo(570000m));
                Assert.That(Editor(dialog, "Assessed").Value, Is.EqualTo(558000m));

                var collectorLabel = All(dialog).OfType<Label>().Single(label => label.Text == "Collector");
                var collectors = All(collectorLabel.Parent!).OfType<ComboBox>().Single();
                Assert.That(collectors.Text, Is.Empty);
                Assert.That(collectors.SelectedIndex, Is.EqualTo(-1));
                var grid = All(dialog).OfType<DataGridView>().Single();
                Assert.That(grid.Rows.Count, Is.EqualTo(0));

                collectors.SelectedItem = collectors.Items.Cast<object>().OfType<Account>()
                    .Single(account => account.Name == "Fort Bend Tax Office");
                Assert.That(grid.Rows.Count, Is.EqualTo(1));
                Assert.That(grid.Rows[0].Cells["Name"].Value, Is.EqualTo("Lamar CISD"));
                Assert.That(Line(grid, "ExemptionAmount"), Is.EqualTo(145000m));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static PropertyTaxStatement Statement(
            Guid collectorId,
            Guid assetId,
            int year,
            string jurisdiction,
            decimal exemption,
            decimal rate)
        {
            var assessed = 580000m;
            var taxable = assessed - exemption;
            var tax = decimal.Round(taxable * rate / 100m, 2, MidpointRounding.AwayFromZero);
            return new PropertyTaxStatement
            {
                AssetId = assetId,
                AccountId = collectorId,
                TaxYear = year,
                StatementDate = new DateTime(year, 10, 26),
                DueDate = new DateTime(year + 1, 1, 31),
                LandMarketValue = 80000m,
                ImprovementMarketValue = 500000m,
                TotalMarketValue = assessed,
                AssessedValue = assessed,
                Exemptions = "Prior",
                Jurisdictions =
                [
                    new PropertyTaxJurisdictionLine
                    {
                        Name = jurisdiction,
                        ExemptionAmount = exemption,
                        TaxableValue = taxable,
                        RatePerHundred = rate,
                        TaxAmount = tax
                    }
                ]
            };
        }

        private static void AssertFullyInside(Form dialog, Control control)
        {
            var origin = dialog.PointToClient(control.PointToScreen(Point.Empty));
            var bounds = new Rectangle(origin, control.Size);
            Assert.That(Rectangle.Inflate(dialog.ClientRectangle, -4, -4).Contains(bounds), Is.True,
                $"{control.Text} is cropped by the dialog.");
        }

        private static AssetOrchestrator Orchestrator() => new(
            new InMemoryAssetDataStore(),
            new InMemoryAccountDataStore(),
            new InMemoryAccountStatementDataStore(),
            new InMemoryTransactionDataStore(),
            new InMemoryVehicleDataStore());

        private static TextBox TextBox(Control dialog, string text) =>
            All(dialog).OfType<TextBox>().Single(box => box.Text == text);

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
