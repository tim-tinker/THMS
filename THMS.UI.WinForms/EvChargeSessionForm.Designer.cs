namespace THMS.UI.WinForms
{
    partial class EvChargeSessionForm
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
            _numOdometer = new NumericUpDown();
            label2 = new Label();
            _dateStart = new DateTimePicker();
            label3 = new Label();
            _numStartSoc = new NumericUpDown();
            _timeStart = new DateTimePicker();
            label4 = new Label();
            label5 = new Label();
            _dateEnd = new DateTimePicker();
            label6 = new Label();
            _numEndSoc = new NumericUpDown();
            _timeEnd = new DateTimePicker();
            label7 = new Label();
            label8 = new Label();
            _numKwhAdded = new NumericUpDown();
            label9 = new Label();
            _checkHomeCharger = new CheckBox();
            _numSessionCost = new NumericUpDown();
            label10 = new Label();
            label20 = new Label();
            label21 = new Label();
            _btnSave = new Button();
            _btnCancel = new Button();
            _numLastOdometer = new NumericUpDown();
            _numLastSoc = new NumericUpDown();
            _numBatteryKwhAdded = new NumericUpDown();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)_numOdometer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numStartSoc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numEndSoc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numKwhAdded).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numSessionCost).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numLastOdometer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numLastSoc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numBatteryKwhAdded).BeginInit();
            SuspendLayout();
            // 
            // _numOdometer
            // 
            _numOdometer.Location = new Point(226, 95);
            _numOdometer.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            _numOdometer.Name = "_numOdometer";
            _numOdometer.Size = new Size(183, 35);
            _numOdometer.TabIndex = 2;
            _numOdometer.ValueChanged += OnValueChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 97);
            label2.Name = "label2";
            label2.Size = new Size(107, 30);
            label2.TabIndex = 3;
            label2.Text = "Odometer";
            // 
            // _dateStart
            // 
            _dateStart.Format = DateTimePickerFormat.Short;
            _dateStart.Location = new Point(226, 136);
            _dateStart.Name = "_dateStart";
            _dateStart.Size = new Size(183, 35);
            _dateStart.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 138);
            label3.Name = "label3";
            label3.Size = new Size(105, 30);
            label3.TabIndex = 5;
            label3.Text = "Start Date";
            // 
            // _numStartSoc
            // 
            _numStartSoc.Location = new Point(226, 301);
            _numStartSoc.Name = "_numStartSoc";
            _numStartSoc.Size = new Size(183, 35);
            _numStartSoc.TabIndex = 7;
            _numStartSoc.ValueChanged += OnValueChanged;
            // 
            // _timeStart
            // 
            _timeStart.Format = DateTimePickerFormat.Time;
            _timeStart.Location = new Point(226, 178);
            _timeStart.Name = "_timeStart";
            _timeStart.ShowUpDown = true;
            _timeStart.Size = new Size(183, 35);
            _timeStart.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 180);
            label4.Name = "label4";
            label4.Size = new Size(106, 30);
            label4.TabIndex = 8;
            label4.Text = "Start Time";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 304);
            label5.Name = "label5";
            label5.Size = new Size(136, 30);
            label5.TabIndex = 9;
            label5.Text = "Start SOC (%)";
            // 
            // _dateEnd
            // 
            _dateEnd.Format = DateTimePickerFormat.Short;
            _dateEnd.Location = new Point(226, 219);
            _dateEnd.Name = "_dateEnd";
            _dateEnd.Size = new Size(183, 35);
            _dateEnd.TabIndex = 5;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 221);
            label6.Name = "label6";
            label6.Size = new Size(98, 30);
            label6.TabIndex = 5;
            label6.Text = "End Date";
            // 
            // _numEndSoc
            // 
            _numEndSoc.Location = new Point(226, 342);
            _numEndSoc.Name = "_numEndSoc";
            _numEndSoc.Size = new Size(183, 35);
            _numEndSoc.TabIndex = 8;
            _numEndSoc.ValueChanged += OnValueChanged;
            // 
            // _timeEnd
            // 
            _timeEnd.Format = DateTimePickerFormat.Time;
            _timeEnd.Location = new Point(226, 260);
            _timeEnd.Name = "_timeEnd";
            _timeEnd.ShowUpDown = true;
            _timeEnd.Size = new Size(183, 35);
            _timeEnd.TabIndex = 6;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 262);
            label7.Name = "label7";
            label7.Size = new Size(99, 30);
            label7.TabIndex = 8;
            label7.Text = "End Time";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(12, 344);
            label8.Name = "label8";
            label8.Size = new Size(129, 30);
            label8.TabIndex = 9;
            label8.Text = "End SOC (%)";
            // 
            // _numKwhAdded
            // 
            _numKwhAdded.DecimalPlaces = 3;
            _numKwhAdded.Location = new Point(226, 465);
            _numKwhAdded.Name = "_numKwhAdded";
            _numKwhAdded.Size = new Size(183, 35);
            _numKwhAdded.TabIndex = 12;
            _numKwhAdded.ValueChanged += OnValueChanged;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(12, 467);
            label9.Name = "label9";
            label9.Size = new Size(122, 30);
            label9.TabIndex = 11;
            label9.Text = "kWh Added";
            // 
            // _checkHomeCharger
            // 
            _checkHomeCharger.AutoSize = true;
            _checkHomeCharger.Location = new Point(12, 424);
            _checkHomeCharger.Name = "_checkHomeCharger";
            _checkHomeCharger.Size = new Size(174, 34);
            _checkHomeCharger.TabIndex = 10;
            _checkHomeCharger.Text = "Home Charger";
            _checkHomeCharger.UseVisualStyleBackColor = true;
            _checkHomeCharger.CheckedChanged += OnCheckedChangedHomeCharger;
            // 
            // _numSessionCost
            // 
            _numSessionCost.DecimalPlaces = 2;
            _numSessionCost.Location = new Point(226, 506);
            _numSessionCost.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            _numSessionCost.Name = "_numSessionCost";
            _numSessionCost.Size = new Size(183, 35);
            _numSessionCost.TabIndex = 13;
            _numSessionCost.ValueChanged += OnValueChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(12, 508);
            label10.Name = "label10";
            label10.Size = new Size(83, 30);
            label10.TabIndex = 14;
            label10.Text = "Cost ($)";
            // 
            // label20
            // 
            label20.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label20.AutoSize = true;
            label20.Location = new Point(12, 15);
            label20.Name = "label20";
            label20.Size = new Size(150, 30);
            label20.TabIndex = 36;
            label20.Text = "Last Odometer";
            // 
            // label21
            // 
            label21.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label21.AutoSize = true;
            label21.Location = new Point(12, 56);
            label21.Name = "label21";
            label21.Size = new Size(131, 30);
            label21.TabIndex = 38;
            label21.Text = "Last SOC (%)";
            // 
            // _btnSave
            // 
            _btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnSave.Location = new Point(725, 539);
            _btnSave.Name = "_btnSave";
            _btnSave.Size = new Size(131, 40);
            _btnSave.TabIndex = 41;
            _btnSave.Text = "Save";
            _btnSave.UseVisualStyleBackColor = true;
            _btnSave.Click += OnClickSave;
            // 
            // _btnCancel
            // 
            _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnCancel.Location = new Point(588, 539);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(131, 40);
            _btnCancel.TabIndex = 42;
            _btnCancel.Text = "Cancel";
            _btnCancel.UseVisualStyleBackColor = true;
            _btnCancel.Click += OnClickCancel;
            // 
            // _numLastOdometer
            // 
            _numLastOdometer.Location = new Point(226, 13);
            _numLastOdometer.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            _numLastOdometer.Name = "_numLastOdometer";
            _numLastOdometer.Size = new Size(183, 35);
            _numLastOdometer.TabIndex = 0;
            _numLastOdometer.ValueChanged += OnValueChanged;
            // 
            // _numLastSoc
            // 
            _numLastSoc.Location = new Point(226, 54);
            _numLastSoc.Name = "_numLastSoc";
            _numLastSoc.Size = new Size(183, 35);
            _numLastSoc.TabIndex = 1;
            _numLastSoc.ValueChanged += OnValueChanged;
            // 
            // _numBatteryKwhAdded
            // 
            _numBatteryKwhAdded.Location = new Point(226, 383);
            _numBatteryKwhAdded.Name = "_numBatteryKwhAdded";
            _numBatteryKwhAdded.Size = new Size(183, 35);
            _numBatteryKwhAdded.TabIndex = 9;
            _numBatteryKwhAdded.ValueChanged += OnValueChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 385);
            label1.Name = "label1";
            label1.Size = new Size(193, 30);
            label1.TabIndex = 44;
            label1.Text = "Battery kWh Added";
            // 
            // EvChargeSessionForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _btnCancel;
            ClientSize = new Size(868, 591);
            Controls.Add(label1);
            Controls.Add(_numBatteryKwhAdded);
            Controls.Add(_numLastSoc);
            Controls.Add(_numLastOdometer);
            Controls.Add(_btnCancel);
            Controls.Add(_btnSave);
            Controls.Add(label21);
            Controls.Add(label20);
            Controls.Add(label10);
            Controls.Add(_numSessionCost);
            Controls.Add(_checkHomeCharger);
            Controls.Add(label9);
            Controls.Add(_numKwhAdded);
            Controls.Add(label8);
            Controls.Add(label5);
            Controls.Add(label7);
            Controls.Add(_timeEnd);
            Controls.Add(label4);
            Controls.Add(_numEndSoc);
            Controls.Add(_timeStart);
            Controls.Add(label6);
            Controls.Add(_numStartSoc);
            Controls.Add(_dateEnd);
            Controls.Add(label3);
            Controls.Add(_dateStart);
            Controls.Add(label2);
            Controls.Add(_numOdometer);
            Name = "EvChargeSessionForm";
            Text = "EV Charge Session";
            Load += OnLoadForm;
            ((System.ComponentModel.ISupportInitialize)_numOdometer).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numStartSoc).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numEndSoc).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numKwhAdded).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numSessionCost).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numLastOdometer).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numLastSoc).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numBatteryKwhAdded).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private NumericUpDown _numOdometer;
        private Label label2;
        private DateTimePicker _dateStart;
        private Label label3;
        private NumericUpDown _numStartSoc;
        private DateTimePicker _timeStart;
        private Label label4;
        private Label label5;
        private DateTimePicker _dateEnd;
        private Label label6;
        private NumericUpDown _numEndSoc;
        private DateTimePicker _timeEnd;
        private Label label7;
        private Label label8;
        private NumericUpDown _numKwhAdded;
        private Label label9;
        private CheckBox _checkHomeCharger;
        private NumericUpDown _numSessionCost;
        private Label label10;
        private Label label20;
        private Label label21;
        private Button _btnSave;
        private Button _btnCancel;
        private NumericUpDown _numLastOdometer;
        private NumericUpDown _numLastSoc;
        private NumericUpDown _numBatteryKwhAdded;
        private Label label1;
    }
}