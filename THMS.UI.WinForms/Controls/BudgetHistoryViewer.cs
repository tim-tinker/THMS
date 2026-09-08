using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class BudgetHistoryViewer : Form
    {
        public BudgetHistoryViewer()
        {
            InitializeComponent();
        }

        public BudgetHistoryViewer(Guid ruleId)
            : this(new BudgetOrchestrator(), ruleId)
        {
        }

        public BudgetHistoryViewer(BudgetOrchestrator orchestrator, Guid ruleId)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            var rows = orchestrator.GetHistory(ruleId)
                .Select(h => new HistoryRow(h))
                .ToList();
            gridHistory.DataSource = rows;
        }

        private void OnClose(object? sender, EventArgs e)
        {
            Close();
        }

        private sealed class HistoryRow
        {
            public HistoryRow(ExpenseBudgetHistory history)
            {
                Period = $"{history.PeriodStart:d} – {history.PeriodEnd:d}";
                Starting = history.StartingBalance;
                BudgetAmount = history.BudgetAmount;
                Actual = history.ActualExpenses;
                Remaining = history.Remaining;
                Ending = history.EndingBalance;
                Recommended = history.RecommendedAmount;
                Status = history.IsClosed ? "Closed" : "Open";
            }

            public string Period { get; }
            public decimal Starting { get; }
            public decimal BudgetAmount { get; }
            public decimal Actual { get; }
            public decimal Remaining { get; }
            public decimal Ending { get; }
            public decimal Recommended { get; }
            public string Status { get; }
        }
    }
}
