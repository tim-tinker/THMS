namespace THMS.UI.WinForms.Controls
{
    partial class CategoryManagerControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                treeCategories.ImageList = null;
                _statusImages?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

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
            btnSave = new ThmsButton();
            btnDeactivate = new ThmsButton();
            btnMergeSelected = new ThmsButton();
            pnlBottom = new FlowLayoutPanel();
            btnAdd = new ThmsButton();
            btnImport = new ThmsButton();
            btnMerge = new ThmsButton();
            btnClose = new ThmsButton();
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            pnlEditor.SuspendLayout();
            flowEditorButtons.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Margin = new Padding(0);
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Vertical;
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
            treeCategories.Margin = new Padding(0);
            treeCategories.Name = "treeCategories";
            treeCategories.AfterSelect += OnTreeSelected;
            pnlEditor.ColumnCount = 1;
            pnlEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlEditor.Dock = DockStyle.Top;
            pnlEditor.AutoSize = true;
            pnlEditor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlEditor.Name = "pnlEditor";
            pnlEditor.Padding = new Padding(4);
            pnlEditor.RowCount = 7;
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlEditor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
            lblUsage.AutoSize = true;
            lblUsage.Dock = DockStyle.Fill;
            lblUsage.Margin = new Padding(3, 12, 3, 3);
            lblUsage.Name = "lblUsage";
            lblUsage.Text = "Usage: 0";
            flowEditorButtons.AutoSize = true;
            flowEditorButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowEditorButtons.Dock = DockStyle.Fill;
            flowEditorButtons.Name = "flowEditorButtons";
            flowEditorButtons.WrapContents = true;
            flowEditorButtons.Controls.Add(btnSave);
            flowEditorButtons.Controls.Add(btnMergeSelected);
            flowEditorButtons.Controls.Add(btnDeactivate);
            btnSave.Name = "btnSave";
            btnSave.Text = "Save";
            btnSave.Click += OnSave;
            btnDeactivate.Destructive = true;
            btnDeactivate.Name = "btnDeactivate";
            btnDeactivate.Text = "Deactivate";
            btnDeactivate.Click += OnDeactivate;
            btnMergeSelected.Name = "btnMergeSelected";
            btnMergeSelected.Text = "Merge…";
            btnMergeSelected.Click += OnMerge;
            pnlBottom.AutoSize = true;
            pnlBottom.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new Padding(12, 12, 12, 16);
            pnlBottom.WrapContents = true;
            pnlBottom.Controls.Add(btnAdd);
            pnlBottom.Controls.Add(btnImport);
            pnlBottom.Controls.Add(btnMerge);
            pnlBottom.Controls.Add(btnClose);
            btnAdd.Name = "btnAdd";
            btnAdd.Text = "Add Category";
            btnAdd.Click += OnAdd;
            btnImport.Name = "btnImport";
            btnImport.Text = "Import";
            btnImport.Click += OnImport;
            btnMerge.Name = "btnMerge";
            btnMerge.Text = "Merge Categories";
            btnMerge.Click += OnMerge;
            btnClose.Name = "btnClose";
            btnClose.Text = "Close";
            btnClose.Click += OnClose;
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(8, 0, 8, 0);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Visible = false;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = false;
            Controls.Add(splitMain);
            Controls.Add(lblStatus);
            Controls.Add(pnlBottom);
            Dock = DockStyle.Fill;
            Name = "CategoryManagerControl";
            Size = new Size(780, 480);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            splitMain.Panel2.ResumeLayout(false);
            splitMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            pnlEditor.ResumeLayout(false);
            pnlEditor.PerformLayout();
            flowEditorButtons.ResumeLayout(false);
            flowEditorButtons.PerformLayout();
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
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
        private ThmsButton btnSave;
        private ThmsButton btnDeactivate;
        private ThmsButton btnMergeSelected;
        private FlowLayoutPanel pnlBottom;
        private ThmsButton btnAdd;
        private ThmsButton btnImport;
        private ThmsButton btnMerge;
        private ThmsButton btnClose;
        private Label lblStatus;
    }
}
