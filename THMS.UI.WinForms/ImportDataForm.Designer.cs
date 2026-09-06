namespace THMS.UI.WinForms
{
    partial class ImportDataForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _textAccountFile = new TextBox();
            _btnAccountFile = new Button();
            label1 = new Label();
            _listTransactionFiles = new ListBox();
            label2 = new Label();
            _btnTransactionFiles = new Button();
            _btnCancel = new Button();
            _btnImport = new Button();
            _openAccountFile = new OpenFileDialog();
            _openTransactionFiles = new OpenFileDialog();
            SuspendLayout();
            // 
            // _textAccountFile
            // 
            _textAccountFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textAccountFile.Location = new Point(265, 12);
            _textAccountFile.Name = "_textAccountFile";
            _textAccountFile.Size = new Size(895, 35);
            _textAccountFile.TabIndex = 0;
            // 
            // _btnAccountFile
            // 
            _btnAccountFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnAccountFile.Location = new Point(1166, 12);
            _btnAccountFile.Name = "_btnAccountFile";
            _btnAccountFile.Size = new Size(55, 40);
            _btnAccountFile.TabIndex = 1;
            _btnAccountFile.Text = "...";
            _btnAccountFile.UseVisualStyleBackColor = true;
            _btnAccountFile.Click += OnClickAccountFile;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(210, 30);
            label1.TabIndex = 2;
            label1.Text = "Account Spreadsheet";
            // 
            // _listTransactionFiles
            // 
            _listTransactionFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _listTransactionFiles.FormattingEnabled = true;
            _listTransactionFiles.Location = new Point(265, 53);
            _listTransactionFiles.Name = "_listTransactionFiles";
            _listTransactionFiles.Size = new Size(895, 364);
            _listTransactionFiles.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 63);
            label2.Name = "label2";
            label2.Size = new Size(247, 30);
            label2.TabIndex = 4;
            label2.Text = "Transaction Spreadsheets";
            // 
            // _btnTransactionFiles
            // 
            _btnTransactionFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnTransactionFiles.Location = new Point(1166, 58);
            _btnTransactionFiles.Name = "_btnTransactionFiles";
            _btnTransactionFiles.Size = new Size(55, 40);
            _btnTransactionFiles.TabIndex = 1;
            _btnTransactionFiles.Text = "...";
            _btnTransactionFiles.UseVisualStyleBackColor = true;
            _btnTransactionFiles.Click += OnClickOpenTransactionFiles;
            // 
            // _btnCancel
            // 
            _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnCancel.Location = new Point(1090, 445);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(131, 40);
            _btnCancel.TabIndex = 5;
            _btnCancel.Text = "Cancel";
            _btnCancel.UseVisualStyleBackColor = true;
            _btnCancel.Click += OnClickCancel;
            // 
            // _btnImport
            // 
            _btnImport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnImport.Location = new Point(953, 445);
            _btnImport.Name = "_btnImport";
            _btnImport.Size = new Size(131, 40);
            _btnImport.TabIndex = 6;
            _btnImport.Text = "Import";
            _btnImport.UseVisualStyleBackColor = true;
            _btnImport.Click += OnClickImport;
            // 
            // _openAccountFile
            // 
            _openAccountFile.Filter = "Spreadsheet (*.xlsx)|*.xlsx";
            _openAccountFile.Title = "Select Account Spreadsheet";
            // 
            // _openTransactionFiles
            // 
            _openTransactionFiles.Filter = "Spreadsheet (*.xlsx)|*.xlsx";
            _openTransactionFiles.Multiselect = true;
            _openTransactionFiles.Title = "Select Transaction Spreadsheets";
            // 
            // ImportDataForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1233, 497);
            Controls.Add(_btnImport);
            Controls.Add(_btnCancel);
            Controls.Add(label2);
            Controls.Add(_listTransactionFiles);
            Controls.Add(label1);
            Controls.Add(_btnTransactionFiles);
            Controls.Add(_btnAccountFile);
            Controls.Add(_textAccountFile);
            Name = "ImportDataForm";
            Text = "Import Historical Transaction Data";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox _textAccountFile;
        private Button _btnAccountFile;
        private Label label1;
        private ListBox _listTransactionFiles;
        private Label label2;
        private Button _btnTransactionFiles;
        private Button _btnCancel;
        private Button _btnImport;
        private OpenFileDialog _openAccountFile;
        private OpenFileDialog _openTransactionFiles;
    }
}