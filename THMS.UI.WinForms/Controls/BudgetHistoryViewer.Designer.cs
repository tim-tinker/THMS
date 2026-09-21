namespace THMS.UI.WinForms.Controls
{
    partial class BudgetHistoryViewer
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            gridHistory = new DataGridView();
            pnlButtons = new FlowLayoutPanel();
            btnClose = new ThmsButton();
            ((System.ComponentModel.ISupportInitialize)gridHistory).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            gridHistory.AllowUserToAddRows = false;
            gridHistory.AllowUserToDeleteRows = false;
            gridHistory.AutoGenerateColumns = true;
            gridHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridHistory.Dock = DockStyle.Fill;
            gridHistory.Name = "gridHistory";
            gridHistory.ReadOnly = true;
            gridHistory.RowHeadersVisible = false;
            gridHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            pnlButtons.AutoSize = true;
            pnlButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlButtons.Controls.Add(btnClose);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.FlowDirection = FlowDirection.RightToLeft;
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Padding = new Padding(12, 12, 12, 16);
            pnlButtons.WrapContents = false;
            btnClose.AutoSize = true;
            btnClose.Name = "btnClose";
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(900, 360);
            Controls.Add(gridHistory);
            Controls.Add(pnlButtons);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BudgetHistoryViewer";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Budget History";
            ((System.ComponentModel.ISupportInitialize)gridHistory).EndInit();
            pnlButtons.ResumeLayout(false);
            pnlButtons.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView gridHistory;
        private FlowLayoutPanel pnlButtons;
        private ThmsButton btnClose;
    }
}
