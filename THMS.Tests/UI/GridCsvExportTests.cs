using System.Globalization;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridCsvExportTests
    {
        [Test]
        public void Build_WritesVisibleCellsAsSpreadsheetText()
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            try
            {
                using var grid = new DataGridView { AllowUserToAddRows = false };
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name" });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Secret", HeaderText = "Secret", Visible = false });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Note, extra" });
                var amount = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount" };
                amount.DefaultCellStyle.Format = "C2";
                grid.Columns.Add(amount);
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "When", HeaderText = "When" });
                grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Paid", HeaderText = "Paid" });

                grid.Rows.Add("Alpha, \"beta\"", "hidden", "hello, world", 12.5m, new DateTime(2026, 1, 2), true);
                grid.Rows.Add("line\nbreak", "hidden", "plain", 0m, new DateTime(2026, 3, 4, 15, 30, 0), false);

                var csv = GridCsvExport.Build(grid);

                Assert.That(csv, Is.EqualTo(
                    "Name,\"Note, extra\",Amount,When,Paid\r\n" +
                    "\"Alpha, \"\"beta\"\"\",\"hello, world\",12.50,1/2/2026,true\r\n" +
                    "\"line\nbreak\",plain,0.00,3/4/2026 3:30 PM,false\r\n"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        [Test]
        public void Build_WritesHeaderWhenTheGridHasNoRows()
        {
            using var grid = new DataGridView { AllowUserToAddRows = false };
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Account" });

            Assert.That(GridCsvExport.Build(grid), Is.EqualTo("Account\r\n"));
        }
    }
}
