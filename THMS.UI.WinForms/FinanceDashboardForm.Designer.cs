namespace THMS.UI.WinForms
{
    partial class FinanceDashboardForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView financeGrid;
        private System.Windows.Forms.Label lblTotalIncome;
        private System.Windows.Forms.Label lblTotalSpending;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.financeGrid = new System.Windows.Forms.DataGridView();
            this.lblTotalIncome = new System.Windows.Forms.Label();
            this.lblTotalSpending = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.financeGrid)).BeginInit();
            this.SuspendLayout();

            // financeGrid
            this.financeGrid.Location = new System.Drawing.Point(20, 20);
            this.financeGrid.Name = "financeGrid";
            this.financeGrid.Size = new System.Drawing.Size(760, 350);
            this.financeGrid.TabIndex = 0;

            // lblTotalIncome
            this.lblTotalIncome.Location = new System.Drawing.Point(20, 390);
            this.lblTotalIncome.Name = "lblTotalIncome";
            this.lblTotalIncome.Size = new System.Drawing.Size(300, 25);
            this.lblTotalIncome.Text = "Income:";

            // lblTotalSpending
            this.lblTotalSpending.Location = new System.Drawing.Point(20, 420);
            this.lblTotalSpending.Name = "lblTotalSpending";
            this.lblTotalSpending.Size = new System.Drawing.Size(300, 25);
            this.lblTotalSpending.Text = "Spending:";

            // FinanceDashboardForm
            this.ClientSize = new System.Drawing.Size(800, 460);
            this.Controls.Add(this.financeGrid);
            this.Controls.Add(this.lblTotalIncome);
            this.Controls.Add(this.lblTotalSpending);
            this.Name = "FinanceDashboardForm";
            this.Text = "Finance Dashboard";

            ((System.ComponentModel.ISupportInitialize)(this.financeGrid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
