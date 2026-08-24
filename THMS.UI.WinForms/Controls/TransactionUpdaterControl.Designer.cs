namespace THMS.UI.WinForms.Controls
{
    partial class TransactionUpdaterControl
    {
        private System.ComponentModel.IContainer components = null;

        private Panel pnlActions;
        private Button btnRunUpdate;
        private Button btnClear;
        private Label lblStatus;
        private TextBox txtSummary;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            pnlActions = new Panel();
            btnRunUpdate = new Button();
            btnClear = new Button();
            lblStatus = new Label();
            txtSummary = new TextBox();
            pnlActions.SuspendLayout();
            SuspendLayout();

            btnRunUpdate.Text = "Run Update";
            btnRunUpdate.Location = new Point(12, 12);
            btnRunUpdate.Size = new Size(120, 40);
            btnRunUpdate.Click += OnRunUpdate;

            btnClear.Text = "Clear";
            btnClear.Location = new Point(144, 12);
            btnClear.Size = new Size(120, 40);
            btnClear.Click += OnClearSummary;

            lblStatus.Text = "Ready.";
            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(276, 12);
            lblStatus.Size = new Size(400, 40);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            pnlActions.Controls.Add(btnRunUpdate);
            pnlActions.Controls.Add(btnClear);
            pnlActions.Controls.Add(lblStatus);
            pnlActions.Dock = DockStyle.Top;
            pnlActions.Height = 64;

            txtSummary.Multiline = true;
            txtSummary.ReadOnly = true;
            txtSummary.ScrollBars = ScrollBars.Vertical;
            txtSummary.Dock = DockStyle.Fill;

            Controls.Add(txtSummary);
            Controls.Add(pnlActions);
            Padding = new Padding(12);
            Size = new Size(800, 600);

            pnlActions.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
