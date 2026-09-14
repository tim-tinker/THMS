using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class EvChargeEnergyCellHintsTests
    {
        private static readonly EvChargeEnergyColumn[] EnergyColumns =
        [
            EvChargeEnergyColumn.Drawn,
            EvChargeEnergyColumn.Solar,
            EvChargeEnergyColumn.Battery,
            EvChargeEnergyColumn.Grid,
            EvChargeEnergyColumn.Cost
        ];

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Home_WithoutCircuit_UsesNoCircuitTooltipOnEnergyColumns(bool hasSolar, bool hasContract)
        {
            var session = Home(hasCircuit: false, hasSolar, hasContract);
            foreach (var column in EnergyColumns)
            {
                var hint = EvChargeEnergyCellHints.For(session, column);
                Assert.That(hint.Kind, Is.EqualTo(EvChargeEnergyCellKind.Unavailable));
                Assert.That(hint.ToolTip, Is.EqualTo(EvChargeEnergyCellHints.NoCircuitToolTip));
            }
        }

        [Test]
        public void Home_CircuitWithoutSolarWithoutContract_UsesEstimatedSourcesAndUnknownCost()
        {
            var session = Home(hasCircuit: true, hasSolar: false, hasContract: false);

            AssertNormal(session, EvChargeEnergyColumn.Drawn);
            AssertHint(session, EvChargeEnergyColumn.Solar, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
            AssertHint(session, EvChargeEnergyColumn.Battery, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
            AssertHint(session, EvChargeEnergyColumn.Grid, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
            AssertHint(session, EvChargeEnergyColumn.Cost, EvChargeEnergyCellKind.Unavailable, EvChargeEnergyCellHints.NoContractToolTip);
        }

        [Test]
        public void Home_CircuitWithoutSolarWithContract_UsesEstimatedSourcesAndCost()
        {
            var session = Home(hasCircuit: true, hasSolar: false, hasContract: true);

            AssertNormal(session, EvChargeEnergyColumn.Drawn);
            AssertHint(session, EvChargeEnergyColumn.Solar, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
            AssertHint(session, EvChargeEnergyColumn.Battery, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
            AssertHint(session, EvChargeEnergyColumn.Grid, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
            AssertHint(session, EvChargeEnergyColumn.Cost, EvChargeEnergyCellKind.Estimated, EvChargeEnergyCellHints.NoSolarToolTip);
        }

        [Test]
        public void Home_CircuitAndSolarWithoutContract_OnlyCostIsUnknown()
        {
            var session = Home(hasCircuit: true, hasSolar: true, hasContract: false);

            AssertNormal(session, EvChargeEnergyColumn.Drawn);
            AssertNormal(session, EvChargeEnergyColumn.Solar);
            AssertNormal(session, EvChargeEnergyColumn.Battery);
            AssertNormal(session, EvChargeEnergyColumn.Grid);
            AssertHint(session, EvChargeEnergyColumn.Cost, EvChargeEnergyCellKind.Unavailable, EvChargeEnergyCellHints.NoContractToolTip);
        }

        [Test]
        public void Home_AllDataPresent_HasNoTooltips()
        {
            var session = Home(hasCircuit: true, hasSolar: true, hasContract: true);
            foreach (var column in EnergyColumns)
                AssertNormal(session, column);
        }

        [Test]
        public void Commercial_MarksSourceSplitNotApplicable()
        {
            var session = new CommercialEvChargeSession();

            AssertNormal(session, EvChargeEnergyColumn.Drawn);
            AssertHint(session, EvChargeEnergyColumn.Solar, EvChargeEnergyCellKind.NotApplicable, EvChargeEnergyCellHints.CommercialToolTip);
            AssertHint(session, EvChargeEnergyColumn.Battery, EvChargeEnergyCellKind.NotApplicable, EvChargeEnergyCellHints.CommercialToolTip);
            AssertHint(session, EvChargeEnergyColumn.Grid, EvChargeEnergyCellKind.NotApplicable, EvChargeEnergyCellHints.CommercialToolTip);
            AssertNormal(session, EvChargeEnergyColumn.Cost);
        }

        private static HomeEvChargeSession Home(bool hasCircuit, bool hasSolar, bool hasContract) =>
            new()
            {
                HasCircuitData = hasCircuit,
                HasSolarData = hasSolar,
                HasElectricContract = hasContract
            };

        private static void AssertNormal(BaseEvChargeSession session, EvChargeEnergyColumn column)
        {
            var hint = EvChargeEnergyCellHints.For(session, column);
            Assert.That(hint.Kind, Is.EqualTo(EvChargeEnergyCellKind.Normal));
            Assert.That(hint.ToolTip, Is.Null);
        }

        private static void AssertHint(
            BaseEvChargeSession session,
            EvChargeEnergyColumn column,
            EvChargeEnergyCellKind kind,
            string toolTip)
        {
            var hint = EvChargeEnergyCellHints.For(session, column);
            Assert.That(hint.Kind, Is.EqualTo(kind));
            Assert.That(hint.ToolTip, Is.EqualTo(toolTip));
        }
    }
}
