using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms
{
    internal static class FinanceDashboardDrillDown
    {
        public static void ShowMonth(IWin32Window owner, FinanceDashboardSnapshot snapshot, DateTime month)
        {
            var rows = snapshot.Activity.Where(row => row.Month == month).OrderBy(row => row.Date).ToList();
            var point = snapshot.MonthlyTrend.FirstOrDefault(p => p.Month == month);
            var spending = rows.Sum(row => row.Spending);
            var income = rows.Sum(row => row.Income);
            var setAside = rows.Sum(row => row.SetAside);
            ShowRows(
                owner,
                month.ToString("MMMM yyyy"),
                rows,
                $"Spending {spending:C2}    Income {income:C2}    Set aside {setAside:C2}" +
                (point is null ? "" : $"    Chart {point.Spending:C2} / {point.Income:C2}"));
        }

        public static void ShowSlice(IWin32Window owner, FinanceDashboardSnapshot snapshot, FinanceDashboardCategorySlice slice)
        {
            if (slice.IsOther)
            {
                ShowOther(owner, snapshot, slice);
                return;
            }

            var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ShowCategory(owner, snapshot, slice, month);
        }

        private static void ShowOther(
            IWin32Window owner,
            FinanceDashboardSnapshot snapshot,
            FinanceDashboardCategorySlice slice)
        {
            using var form = new Form
            {
                Text = "Other categories",
                StartPosition = FormStartPosition.CenterParent,
                Width = 480,
                Height = 360
            };
            var grid = Grid();
            grid.Columns.Add(Text("Name", "Category"));
            grid.Columns.Add(Money("Amount"));
            grid.DataSource = slice.Folded.Select(item => new OtherRow(item.Name, item.Amount, item)).ToList();
            grid.CellDoubleClick += (_, _) =>
            {
                if (grid.CurrentRow?.DataBoundItem is OtherRow row)
                    ShowCategory(form, snapshot, row.Slice, new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
            };
            var header = Header($"Other {slice.Amount:C2}. Open a category to see its transactions.");
            form.Controls.Add(grid);
            form.Controls.Add(header);
            form.ShowDialog(owner);
        }

        private static void ShowCategory(
            IWin32Window owner,
            FinanceDashboardSnapshot snapshot,
            FinanceDashboardCategorySlice slice,
            DateTime month)
        {
            var rows = snapshot.Activity
                .Where(row => row.Month == month && row.CategoryId == slice.CategoryId && row.Effect == "Spending")
                .OrderBy(row => row.Date)
                .ToList();
            ShowRows(
                owner,
                slice.Name,
                rows,
                $"Spending {rows.Sum(row => row.Spending):C2}    Slice {slice.Amount:C2}");
        }

        private static void ShowRows(
            IWin32Window owner,
            string title,
            List<FinanceDashboardActivityRow> rows,
            string summary)
        {
            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                Width = 900,
                Height = 480
            };
            var grid = Grid();
            grid.Columns.Add(DateColumn());
            grid.Columns.Add(Text(nameof(FinanceDashboardActivityRow.Description), "Description"));
            grid.Columns.Add(Text(nameof(FinanceDashboardActivityRow.Account), "Account"));
            grid.Columns.Add(Text(nameof(FinanceDashboardActivityRow.Category), "Category"));
            grid.Columns.Add(Text(nameof(FinanceDashboardActivityRow.Role), "Role"));
            grid.Columns.Add(Money(nameof(FinanceDashboardActivityRow.Amount)));
            grid.Columns.Add(Text(nameof(FinanceDashboardActivityRow.Effect), "Effect"));
            grid.DataSource = rows;
            form.Controls.Add(grid);
            form.Controls.Add(Header(summary));
            form.ShowDialog(owner);
        }

        private static DataGridView Grid() => new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        private static Label Header(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(8, 8, 8, 0)
        };

        private static DataGridViewTextBoxColumn Text(string property, string header) => new()
        {
            DataPropertyName = property,
            HeaderText = header,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };

        private static DataGridViewTextBoxColumn Money(string property) => new()
        {
            DataPropertyName = property,
            HeaderText = property == "Amount" ? "Amount" : property,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        };

        private static DataGridViewTextBoxColumn DateColumn() => new()
        {
            DataPropertyName = nameof(FinanceDashboardActivityRow.Date),
            HeaderText = "Date",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "d" },
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        };

        private sealed class OtherRow(string name, decimal amount, FinanceDashboardCategorySlice slice)
        {
            public string Name { get; } = name;
            public decimal Amount { get; } = amount;
            public FinanceDashboardCategorySlice Slice { get; } = slice;
        }
    }
}
