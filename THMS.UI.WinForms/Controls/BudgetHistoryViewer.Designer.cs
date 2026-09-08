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
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)gridHistory).BeginInit();
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
            btnClose.Dock = DockStyle.Bottom;
            btnClose.Height = 40;
            btnClose.Name = "btnClose";
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(900, 360);
            Controls.Add(gridHistory);
            Controls.Add(btnClose);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "BudgetHistoryViewer";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Budget History";
            ((System.ComponentModel.ISupportInitialize)gridHistory).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView gridHistory;
        private Button btnClose;
    }
}
