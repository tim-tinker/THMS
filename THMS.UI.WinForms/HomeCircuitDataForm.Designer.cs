namespace THMS.UI.WinForms
{
    partial class HomeCircuitDataForm
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
            _btnClose = new Button();
            _btnLoad = new Button();
            _groupSummary = new GroupBox();
            label4 = new Label();
            _textBatteryKwh = new TextBox();
            label3 = new Label();
            _textSolarKwh = new TextBox();
            label2 = new Label();
            _textGridKwh = new TextBox();
            _textTotalKwh = new TextBox();
            label1 = new Label();
            _splitContainer = new SplitContainer();
            _gridCircuitData = new DataGridView();
            TimestampColumn = new DataGridViewTextBoxColumn();
            KwhColumn = new DataGridViewTextBoxColumn();
            GridKwhColumn = new DataGridViewTextBoxColumn();
            SolarKwhColumn = new DataGridViewTextBoxColumn();
            BatteryKwhColumn = new DataGridViewTextBoxColumn();
            _groupSummary.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_gridCircuitData).BeginInit();
            SuspendLayout();
            // 
            // _btnClose
            // 
            _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnClose.Location = new Point(663, 848);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(131, 40);
            _btnClose.TabIndex = 0;
            _btnClose.Text = "Close";
            _btnClose.UseVisualStyleBackColor = true;
            _btnClose.Click += OnClickClose;
            // 
            // _btnLoad
            // 
            _btnLoad.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnLoad.Location = new Point(507, 848);
            _btnLoad.Name = "_btnLoad";
            _btnLoad.Size = new Size(150, 40);
            _btnLoad.TabIndex = 1;
            _btnLoad.Text = "Reload Data";
            _btnLoad.UseVisualStyleBackColor = true;
            _btnLoad.Click += OnClickLoad;
            // 
            // _groupSummary
            // 
            _groupSummary.Controls.Add(label4);
            _groupSummary.Controls.Add(_textBatteryKwh);
            _groupSummary.Controls.Add(label3);
            _groupSummary.Controls.Add(_textSolarKwh);
            _groupSummary.Controls.Add(label2);
            _groupSummary.Controls.Add(_textGridKwh);
            _groupSummary.Controls.Add(_textTotalKwh);
            _groupSummary.Controls.Add(label1);
            _groupSummary.Location = new Point(12, 12);
            _groupSummary.Name = "_groupSummary";
            _groupSummary.Size = new Size(733, 127);
            _groupSummary.TabIndex = 2;
            _groupSummary.TabStop = false;
            _groupSummary.Text = "Summary";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(416, 78);
            label4.Name = "label4";
            label4.Size = new Size(126, 30);
            label4.TabIndex = 3;
            label4.Text = "Battery kWh";
            // 
            // _textBatteryKwh
            // 
            _textBatteryKwh.Location = new Point(548, 75);
            _textBatteryKwh.Name = "_textBatteryKwh";
            _textBatteryKwh.ReadOnly = true;
            _textBatteryKwh.Size = new Size(175, 35);
            _textBatteryKwh.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(435, 37);
            label3.Name = "label3";
            label3.Size = new Size(107, 30);
            label3.TabIndex = 3;
            label3.Text = "Solar kWh";
            // 
            // _textSolarKwh
            // 
            _textSolarKwh.Location = new Point(548, 34);
            _textSolarKwh.Name = "_textSolarKwh";
            _textSolarKwh.ReadOnly = true;
            _textSolarKwh.Size = new Size(175, 35);
            _textSolarKwh.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 78);
            label2.Name = "label2";
            label2.Size = new Size(99, 30);
            label2.TabIndex = 3;
            label2.Text = "Grid kWh";
            // 
            // _textGridKwh
            // 
            _textGridKwh.Location = new Point(117, 75);
            _textGridKwh.Name = "_textGridKwh";
            _textGridKwh.ReadOnly = true;
            _textGridKwh.Size = new Size(175, 35);
            _textGridKwh.TabIndex = 4;
            // 
            // _textTotalKwh
            // 
            _textTotalKwh.Location = new Point(117, 34);
            _textTotalKwh.Name = "_textTotalKwh";
            _textTotalKwh.ReadOnly = true;
            _textTotalKwh.Size = new Size(175, 35);
            _textTotalKwh.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 37);
            label1.Name = "label1";
            label1.Size = new Size(105, 30);
            label1.TabIndex = 0;
            label1.Text = "Total kWh";
            // 
            // _splitContainer
            // 
            _splitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _splitContainer.Location = new Point(12, 145);
            _splitContainer.Name = "_splitContainer";
            _splitContainer.Orientation = Orientation.Horizontal;
            // 
            // _splitContainer.Panel1
            // 
            _splitContainer.Panel1.Controls.Add(_gridCircuitData);
            _splitContainer.Size = new Size(782, 697);
            _splitContainer.SplitterDistance = 441;
            _splitContainer.TabIndex = 3;
            // 
            // _gridCircuitData
            // 
            _gridCircuitData.AllowUserToAddRows = false;
            _gridCircuitData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _gridCircuitData.Columns.AddRange(new DataGridViewColumn[] { TimestampColumn, KwhColumn, GridKwhColumn, SolarKwhColumn, BatteryKwhColumn });
            _gridCircuitData.Dock = DockStyle.Fill;
            _gridCircuitData.Location = new Point(0, 0);
            _gridCircuitData.Name = "_gridCircuitData";
            _gridCircuitData.RowHeadersVisible = false;
            _gridCircuitData.RowHeadersWidth = 72;
            _gridCircuitData.Size = new Size(782, 441);
            _gridCircuitData.TabIndex = 0;
            // 
            // TimestampColumn
            // 
            TimestampColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            TimestampColumn.DataPropertyName = "Timestamp";
            TimestampColumn.HeaderText = "Timestamp";
            TimestampColumn.MinimumWidth = 9;
            TimestampColumn.Name = "TimestampColumn";
            // 
            // KwhColumn
            // 
            KwhColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            KwhColumn.DataPropertyName = "Kwh";
            KwhColumn.HeaderText = "kWh";
            KwhColumn.MinimumWidth = 9;
            KwhColumn.Name = "KwhColumn";
            KwhColumn.Width = 96;
            // 
            // GridKwhColumn
            // 
            GridKwhColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            GridKwhColumn.DataPropertyName = "GridKwh";
            GridKwhColumn.HeaderText = "Grid kWh";
            GridKwhColumn.MinimumWidth = 9;
            GridKwhColumn.Name = "GridKwhColumn";
            GridKwhColumn.Width = 140;
            // 
            // SolarKwhColumn
            // 
            SolarKwhColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            SolarKwhColumn.DataPropertyName = "SolarKwh";
            SolarKwhColumn.HeaderText = "Solar kWh";
            SolarKwhColumn.MinimumWidth = 9;
            SolarKwhColumn.Name = "SolarKwhColumn";
            SolarKwhColumn.Width = 148;
            // 
            // BatteryKwhColumn
            // 
            BatteryKwhColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            BatteryKwhColumn.DataPropertyName = "BatteryKwh";
            BatteryKwhColumn.HeaderText = "Battery kWh";
            BatteryKwhColumn.MinimumWidth = 9;
            BatteryKwhColumn.Name = "BatteryKwhColumn";
            BatteryKwhColumn.Width = 167;
            // 
            // HomeCircuitDataForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(806, 900);
            Controls.Add(_splitContainer);
            Controls.Add(_groupSummary);
            Controls.Add(_btnLoad);
            Controls.Add(_btnClose);
            Name = "HomeCircuitDataForm";
            Text = "Home Circuit data (date/time)";
            Load += OnLoadForm;
            _groupSummary.ResumeLayout(false);
            _groupSummary.PerformLayout();
            _splitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_gridCircuitData).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button _btnClose;
        private Button _btnLoad;
        private GroupBox _groupSummary;
        private TextBox _textSolarKwh;
        private Label label2;
        private TextBox _textGridKwh;
        private TextBox _textTotalKwh;
        private Label label1;
        private Label label4;
        private TextBox _textBatteryKwh;
        private Label label3;
        private SplitContainer _splitContainer;
        private DataGridView _gridCircuitData;
        private DataGridViewTextBoxColumn TimestampColumn;
        private DataGridViewTextBoxColumn KwhColumn;
        private DataGridViewTextBoxColumn GridKwhColumn;
        private DataGridViewTextBoxColumn SolarKwhColumn;
        private DataGridViewTextBoxColumn BatteryKwhColumn;
    }
}