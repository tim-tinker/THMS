namespace THMS.UI.WinForms
{
    partial class VehicleSelectionForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView gridVehicles;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            gridVehicles = new DataGridView();
            btnOk = new Button();
            btnCancel = new Button();
            NameColumn = new DataGridViewTextBoxColumn();
            MakeColumn = new DataGridViewTextBoxColumn();
            ModelColumn = new DataGridViewTextBoxColumn();
            YearColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)gridVehicles).BeginInit();
            SuspendLayout();
            // 
            // gridVehicles
            // 
            gridVehicles.AllowUserToAddRows = false;
            gridVehicles.AllowUserToDeleteRows = false;
            gridVehicles.AllowUserToResizeRows = false;
            gridVehicles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridVehicles.Columns.AddRange(new DataGridViewColumn[] { NameColumn, MakeColumn, ModelColumn, YearColumn });
            gridVehicles.Dock = DockStyle.Top;
            gridVehicles.Location = new Point(0, 0);
            gridVehicles.Margin = new Padding(4);
            gridVehicles.MultiSelect = false;
            gridVehicles.Name = "gridVehicles";
            gridVehicles.RowHeadersVisible = false;
            gridVehicles.RowHeadersWidth = 72;
            gridVehicles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridVehicles.Size = new Size(600, 350);
            gridVehicles.TabIndex = 0;
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.Location = new Point(360, 370);
            btnOk.Margin = new Padding(4);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(100, 40);
            btnOk.TabIndex = 1;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += OnClickOk;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(470, 370);
            btnCancel.Margin = new Padding(4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 40);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += OnClickCancel;
            // 
            // NameColumn
            // 
            NameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            NameColumn.DataPropertyName = "Name";
            NameColumn.HeaderText = "Name";
            NameColumn.MinimumWidth = 9;
            NameColumn.Name = "NameColumn";
            // 
            // MakeColumn
            // 
            MakeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            MakeColumn.DataPropertyName = "Make";
            MakeColumn.HeaderText = "Make";
            MakeColumn.MinimumWidth = 9;
            MakeColumn.Name = "MakeColumn";
            // 
            // ModelColumn
            // 
            ModelColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            ModelColumn.DataPropertyName = "Model";
            ModelColumn.HeaderText = "Model";
            ModelColumn.MinimumWidth = 9;
            ModelColumn.Name = "ModelColumn";
            // 
            // YearColumn
            // 
            YearColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            YearColumn.DataPropertyName = "Year";
            YearColumn.HeaderText = "Year";
            YearColumn.MinimumWidth = 9;
            YearColumn.Name = "YearColumn";
            // 
            // VehicleSelectionForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 430);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(gridVehicles);
            Margin = new Padding(4);
            Name = "VehicleSelectionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Select Vehicle";
            ((System.ComponentModel.ISupportInitialize)gridVehicles).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridViewTextBoxColumn NameColumn;
        private DataGridViewTextBoxColumn MakeColumn;
        private DataGridViewTextBoxColumn ModelColumn;
        private DataGridViewTextBoxColumn YearColumn;
    }
}
