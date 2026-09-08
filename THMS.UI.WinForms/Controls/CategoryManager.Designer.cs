namespace THMS.UI.WinForms.Controls
{
    partial class CategoryManager
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
            splitMain = new SplitContainer();
            treeCategories = new TreeView();
            txtSearch = new TextBox();
            pnlEditor = new TableLayoutPanel();
            lblName = new Label();
            txtName = new TextBox();
            lblParent = new Label();
            cmbParent = new ComboBox();
            chkActive = new CheckBox();
            lblUsage = new Label();
            flowEditorButtons = new FlowLayoutPanel();
            btnSave = new Button();
            btnDeactivate = new Button();
            btnMergeSelected = new Button();
            pnlBottom = new TableLayoutPanel();
            flowBottomLeft = new FlowLayoutPanel();
            btnAdd = new Button();
            btnMerge = new Button();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            pnlEditor.SuspendLayout();
            flowEditorButtons.SuspendLayout();
            pnlBottom.SuspendLayout();
            flowBottomLeft.SuspendLayout();
            SuspendLayout();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Name = "splitMain";
            splitMain.SplitterWidth = 7;
            splitMain.Panel1.Controls.Add(treeCategories);
            splitMain.Panel1.Controls.Add(txtSearch);
            splitMain.Panel2.Padding = new Padding(8);
            splitMain.Panel2.Controls.Add(pnlEditor);
            txtSearch.Dock = DockStyle.Top;
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search categories";
            txtSearch.TextChanged += OnSearchChanged;
            treeCategories.Dock = DockStyle.Fill;
            treeCategories.HideSelection = false;
            treeCategories.Name = "treeCategories";
            treeCategories.AfterSelect += OnTreeSelected;
            pnlEditor.ColumnCount = 1;
            pnlEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlEditor.Dock = DockStyle.Fill;
            pnlEditor.Name = "pnlEditor";
            pnlEditor.Padding = new Padding(4);
            pnlEditor.RowCount = 7;
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.Controls.Add(lblName, 0, 0);
            pnlEditor.Controls.Add(txtName, 0, 1);
            pnlEditor.Controls.Add(lblParent, 0, 2);
            pnlEditor.Controls.Add(cmbParent, 0, 3);
            pnlEditor.Controls.Add(chkActive, 0, 4);
            pnlEditor.Controls.Add(lblUsage, 0, 5);
            pnlEditor.Controls.Add(flowEditorButtons, 0, 6);
            lblName.AutoSize = true;
            lblName.Name = "lblName";
            lblName.Text = "Name:";
            txtName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtName.Name = "txtName";
            txtName.TextChanged += OnEditorChanged;
            lblParent.AutoSize = true;
            lblParent.Margin = new Padding(3, 12, 3, 0);
            lblParent.Name = "lblParent";
            lblParent.Text = "Parent category:";
            cmbParent.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmbParent.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbParent.Name = "cmbParent";
            cmbParent.SelectedIndexChanged += OnEditorChanged;
            chkActive.AutoSize = true;
            chkActive.Margin = new Padding(3, 12, 3, 3);
            chkActive.Name = "chkActive";
            chkActive.Text = "Active";
            chkActive.CheckedChanged += OnEditorChanged;
            lblUsage.AutoSize = false;
            lblUsage.Dock = DockStyle.Fill;
            lblUsage.Name = "lblUsage";
            lblUsage.Text = "Usage: 0";
            flowEditorButtons.AutoSize = true;
            flowEditorButtons.Dock = DockStyle.Fill;
            flowEditorButtons.Name = "flowEditorButtons";
            flowEditorButtons.WrapContents = true;
            flowEditorButtons.Controls.Add(btnSave);
            flowEditorButtons.Controls.Add(btnDeactivate);
            flowEditorButtons.Controls.Add(btnMergeSelected);
            btnSave.AutoSize = true;
            btnSave.MinimumSize = new Size(90, 32);
            btnSave.Name = "btnSave";
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnDeactivate.AutoSize = true;
            btnDeactivate.MinimumSize = new Size(90, 32);
            btnDeactivate.Name = "btnDeactivate";
            btnDeactivate.Text = "Deactivate";
            btnDeactivate.Click += OnDeactivate;
            btnMergeSelected.AutoSize = true;
            btnMergeSelected.MinimumSize = new Size(90, 32);
            btnMergeSelected.Name = "btnMergeSelected";
            btnMergeSelected.Text = "Merge…";
            btnMergeSelected.Click += OnMerge;
            pnlBottom.ColumnCount = 2;
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new Padding(8, 8, 8, 8);
            pnlBottom.RowCount = 1;
            pnlBottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlBottom.Controls.Add(flowBottomLeft, 0, 0);
            pnlBottom.Controls.Add(btnClose, 1, 0);
            flowBottomLeft.AutoSize = true;
            flowBottomLeft.Dock = DockStyle.Fill;
            flowBottomLeft.Name = "flowBottomLeft";
            flowBottomLeft.WrapContents = true;
            flowBottomLeft.Controls.Add(btnAdd);
            flowBottomLeft.Controls.Add(btnMerge);
            btnAdd.AutoSize = true;
            btnAdd.MinimumSize = new Size(120, 32);
            btnAdd.Name = "btnAdd";
            btnAdd.Text = "Add Category";
            btnAdd.Click += OnAdd;
            btnMerge.AutoSize = true;
            btnMerge.MinimumSize = new Size(140, 32);
            btnMerge.Name = "btnMerge";
            btnMerge.Text = "Merge Categories";
            btnMerge.Click += OnMerge;
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.AutoSize = true;
            btnClose.MinimumSize = new Size(90, 32);
            btnClose.Name = "btnClose";
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(780, 480);
            Controls.Add(splitMain);
            Controls.Add(pnlBottom);
            MinimumSize = new Size(560, 400);
            Name = "CategoryManager";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Category Manager";
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            pnlEditor.ResumeLayout(false);
            pnlEditor.PerformLayout();
            flowEditorButtons.ResumeLayout(false);
            flowEditorButtons.PerformLayout();
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            flowBottomLeft.ResumeLayout(false);
            flowBottomLeft.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitMain;
        private TextBox txtSearch;
        private TreeView treeCategories;
        private TableLayoutPanel pnlEditor;
        private Label lblName;
        private TextBox txtName;
        private Label lblParent;
        private ComboBox cmbParent;
        private CheckBox chkActive;
        private Label lblUsage;
        private FlowLayoutPanel flowEditorButtons;
        private Button btnSave;
        private Button btnDeactivate;
        private Button btnMergeSelected;
        private TableLayoutPanel pnlBottom;
        private FlowLayoutPanel flowBottomLeft;
        private Button btnAdd;
        private Button btnMerge;
        private Button btnClose;
    }
}
