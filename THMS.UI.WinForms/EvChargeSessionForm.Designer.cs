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
            label1 = new Label();
            _textVehicle = new TextBox();
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
            _btnLoadCircuitData = new Button();
            label11 = new Label();
            label12 = new Label();
            label13 = new Label();
            label14 = new Label();
            label15 = new Label();
            label16 = new Label();
            label17 = new Label();
            label18 = new Label();
            label19 = new Label();
            _textSocAdded = new TextBox();
            _textSocUsed = new TextBox();
            _textMilesUsed = new TextBox();
            _textKwhUsed = new TextBox();
            _textCostPerMile = new TextBox();
            _textWhPerMile = new TextBox();
            _textMpge = new TextBox();
            _textGridKwh = new TextBox();
            _textSolarKwh = new TextBox();
            textPreviousOdometer = new TextBox();
            label20 = new Label();
            textBox1 = new TextBox();
            label21 = new Label();
            _textBatteryKwh = new TextBox();
            label22 = new Label();
            _btnSave = new Button();
            ((System.ComponentModel.ISupportInitialize)_numOdometer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numStartSoc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numEndSoc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numKwhAdded).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numSessionCost).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(79, 30);
            label1.TabIndex = 0;
            label1.Text = "Vehicle";
            // 
            // _textVehicle
            // 
            _textVehicle.Location = new Point(192, 12);
            _textVehicle.Name = "_textVehicle";
            _textVehicle.Size = new Size(170, 35);
            _textVehicle.TabIndex = 1;
            // 
            // _numOdometer
            // 
            _numOdometer.Location = new Point(192, 52);
            _numOdometer.Name = "_numOdometer";
            _numOdometer.Size = new Size(170, 35);
            _numOdometer.TabIndex = 2;
            _numOdometer.ValueChanged += OnValueChangedOdometer;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 55);
            label2.Name = "label2";
            label2.Size = new Size(107, 30);
            label2.TabIndex = 3;
            label2.Text = "Odometer";
            // 
            // _dateStart
            // 
            _dateStart.Format = DateTimePickerFormat.Short;
            _dateStart.Location = new Point(192, 93);
            _dateStart.Name = "_dateStart";
            _dateStart.Size = new Size(170, 35);
            _dateStart.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 98);
            label3.Name = "label3";
            label3.Size = new Size(105, 30);
            label3.TabIndex = 5;
            label3.Text = "Start Date";
            // 
            // _numStartSoc
            // 
            _numStartSoc.Location = new Point(192, 176);
            _numStartSoc.Name = "_numStartSoc";
            _numStartSoc.Size = new Size(170, 35);
            _numStartSoc.TabIndex = 6;
            _numStartSoc.ValueChanged += OnValueChangedStartSoc;
            // 
            // _timeStart
            // 
            _timeStart.Format = DateTimePickerFormat.Time;
            _timeStart.Location = new Point(192, 135);
            _timeStart.Name = "_timeStart";
            _timeStart.ShowUpDown = true;
            _timeStart.Size = new Size(170, 35);
            _timeStart.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 139);
            label4.Name = "label4";
            label4.Size = new Size(106, 30);
            label4.TabIndex = 8;
            label4.Text = "Start Time";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 178);
            label5.Name = "label5";
            label5.Size = new Size(101, 30);
            label5.TabIndex = 9;
            label5.Text = "Start SOC";
            // 
            // _dateEnd
            // 
            _dateEnd.Format = DateTimePickerFormat.Short;
            _dateEnd.Location = new Point(192, 217);
            _dateEnd.Name = "_dateEnd";
            _dateEnd.Size = new Size(170, 35);
            _dateEnd.TabIndex = 4;
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
            _numEndSoc.Location = new Point(192, 299);
            _numEndSoc.Name = "_numEndSoc";
            _numEndSoc.Size = new Size(170, 35);
            _numEndSoc.TabIndex = 6;
            _numEndSoc.ValueChanged += OnValueChangedEndSoc;
            // 
            // _timeEnd
            // 
            _timeEnd.Format = DateTimePickerFormat.Time;
            _timeEnd.Location = new Point(192, 258);
            _timeEnd.Name = "_timeEnd";
            _timeEnd.ShowUpDown = true;
            _timeEnd.Size = new Size(170, 35);
            _timeEnd.TabIndex = 7;
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
            label8.Location = new Point(12, 301);
            label8.Name = "label8";
            label8.Size = new Size(94, 30);
            label8.TabIndex = 9;
            label8.Text = "End SOC";
            // 
            // _numKwhAdded
            // 
            _numKwhAdded.DecimalPlaces = 3;
            _numKwhAdded.Location = new Point(192, 386);
            _numKwhAdded.Name = "_numKwhAdded";
            _numKwhAdded.Size = new Size(170, 35);
            _numKwhAdded.TabIndex = 10;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(12, 388);
            label9.Name = "label9";
            label9.Size = new Size(122, 30);
            label9.TabIndex = 11;
            label9.Text = "kWh Added";
            // 
            // _checkHomeCharger
            // 
            _checkHomeCharger.AutoSize = true;
            _checkHomeCharger.Location = new Point(12, 344);
            _checkHomeCharger.Name = "_checkHomeCharger";
            _checkHomeCharger.Size = new Size(174, 34);
            _checkHomeCharger.TabIndex = 12;
            _checkHomeCharger.Text = "Home Charger";
            _checkHomeCharger.UseVisualStyleBackColor = true;
            _checkHomeCharger.CheckedChanged += OnCheckedChangedHomeCharger;
            // 
            // _numSessionCost
            // 
            _numSessionCost.DecimalPlaces = 2;
            _numSessionCost.Location = new Point(192, 427);
            _numSessionCost.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            _numSessionCost.Name = "_numSessionCost";
            _numSessionCost.Size = new Size(170, 35);
            _numSessionCost.TabIndex = 13;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(12, 429);
            label10.Name = "label10";
            label10.Size = new Size(83, 30);
            label10.TabIndex = 14;
            label10.Text = "Cost ($)";
            // 
            // _btnLoadCircuitData
            // 
            _btnLoadCircuitData.Location = new Point(192, 340);
            _btnLoadCircuitData.Name = "_btnLoadCircuitData";
            _btnLoadCircuitData.Size = new Size(194, 40);
            _btnLoadCircuitData.TabIndex = 15;
            _btnLoadCircuitData.Text = "Load Circuit Data";
            _btnLoadCircuitData.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(441, 390);
            label11.Name = "label11";
            label11.Size = new Size(99, 30);
            label11.TabIndex = 17;
            label11.Text = "Grid kWh";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(441, 138);
            label12.Name = "label12";
            label12.Size = new Size(105, 30);
            label12.TabIndex = 18;
            label12.Text = "SOC Used";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(441, 303);
            label13.Name = "label13";
            label13.Size = new Size(120, 30);
            label13.TabIndex = 19;
            label13.Text = "SOC Added";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(441, 97);
            label14.Name = "label14";
            label14.Size = new Size(114, 30);
            label14.TabIndex = 20;
            label14.Text = "Miles Used";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(441, 180);
            label15.Name = "label15";
            label15.Size = new Size(107, 30);
            label15.TabIndex = 21;
            label15.Text = "kWh Used";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(12, 472);
            label16.Name = "label16";
            label16.Size = new Size(72, 30);
            label16.TabIndex = 22;
            label16.Text = "$/Mile";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(441, 221);
            label17.Name = "label17";
            label17.Size = new Size(93, 30);
            label17.TabIndex = 23;
            label17.Text = "Wh/Mile";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(441, 262);
            label18.Name = "label18";
            label18.Size = new Size(69, 30);
            label18.TabIndex = 24;
            label18.Text = "MPGe";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(441, 431);
            label19.Name = "label19";
            label19.Size = new Size(107, 30);
            label19.TabIndex = 25;
            label19.Text = "Solar kWh";
            // 
            // _textSocAdded
            // 
            _textSocAdded.Location = new Point(597, 301);
            _textSocAdded.Name = "_textSocAdded";
            _textSocAdded.Size = new Size(170, 35);
            _textSocAdded.TabIndex = 26;
            // 
            // _textSocUsed
            // 
            _textSocUsed.Location = new Point(597, 136);
            _textSocUsed.Name = "_textSocUsed";
            _textSocUsed.Size = new Size(170, 35);
            _textSocUsed.TabIndex = 27;
            // 
            // _textMilesUsed
            // 
            _textMilesUsed.Location = new Point(597, 95);
            _textMilesUsed.Name = "_textMilesUsed";
            _textMilesUsed.Size = new Size(170, 35);
            _textMilesUsed.TabIndex = 28;
            // 
            // _textKwhUsed
            // 
            _textKwhUsed.Location = new Point(597, 178);
            _textKwhUsed.Name = "_textKwhUsed";
            _textKwhUsed.Size = new Size(170, 35);
            _textKwhUsed.TabIndex = 29;
            // 
            // _textCostPerMile
            // 
            _textCostPerMile.Location = new Point(192, 470);
            _textCostPerMile.Name = "_textCostPerMile";
            _textCostPerMile.Size = new Size(170, 35);
            _textCostPerMile.TabIndex = 30;
            // 
            // _textWhPerMile
            // 
            _textWhPerMile.Location = new Point(597, 219);
            _textWhPerMile.Name = "_textWhPerMile";
            _textWhPerMile.Size = new Size(170, 35);
            _textWhPerMile.TabIndex = 31;
            // 
            // _textMpge
            // 
            _textMpge.Location = new Point(597, 260);
            _textMpge.Name = "_textMpge";
            _textMpge.Size = new Size(170, 35);
            _textMpge.TabIndex = 32;
            // 
            // _textGridKwh
            // 
            _textGridKwh.Location = new Point(597, 388);
            _textGridKwh.Name = "_textGridKwh";
            _textGridKwh.Size = new Size(170, 35);
            _textGridKwh.TabIndex = 33;
            // 
            // _textSolarKwh
            // 
            _textSolarKwh.Location = new Point(597, 429);
            _textSolarKwh.Name = "_textSolarKwh";
            _textSolarKwh.Size = new Size(170, 35);
            _textSolarKwh.TabIndex = 34;
            // 
            // textPreviousOdometer
            // 
            textPreviousOdometer.Location = new Point(597, 10);
            textPreviousOdometer.Name = "textPreviousOdometer";
            textPreviousOdometer.Size = new Size(170, 35);
            textPreviousOdometer.TabIndex = 35;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(441, 13);
            label20.Name = "label20";
            label20.Size = new Size(150, 30);
            label20.TabIndex = 36;
            label20.Text = "Last Odometer";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(597, 51);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(175, 35);
            textBox1.TabIndex = 37;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(441, 54);
            label21.Name = "label21";
            label21.Size = new Size(96, 30);
            label21.TabIndex = 38;
            label21.Text = "Last SOC";
            // 
            // _textBatteryKwh
            // 
            _textBatteryKwh.Location = new Point(597, 472);
            _textBatteryKwh.Name = "_textBatteryKwh";
            _textBatteryKwh.Size = new Size(170, 35);
            _textBatteryKwh.TabIndex = 39;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(441, 474);
            label22.Name = "label22";
            label22.Size = new Size(126, 30);
            label22.TabIndex = 40;
            label22.Text = "Battery kWh";
            // 
            // _btnSave
            // 
            _btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnSave.Location = new Point(651, 555);
            _btnSave.Name = "_btnSave";
            _btnSave.Size = new Size(131, 40);
            _btnSave.TabIndex = 41;
            _btnSave.Text = "Save";
            _btnSave.UseVisualStyleBackColor = true;
            _btnSave.Click += OnClickSave;
            // 
            // EvChargeSessionForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(794, 607);
            Controls.Add(_btnSave);
            Controls.Add(label22);
            Controls.Add(_textBatteryKwh);
            Controls.Add(label21);
            Controls.Add(textBox1);
            Controls.Add(label20);
            Controls.Add(textPreviousOdometer);
            Controls.Add(_textSolarKwh);
            Controls.Add(_textGridKwh);
            Controls.Add(_textMpge);
            Controls.Add(_textWhPerMile);
            Controls.Add(_textCostPerMile);
            Controls.Add(_textKwhUsed);
            Controls.Add(_textMilesUsed);
            Controls.Add(_textSocUsed);
            Controls.Add(_textSocAdded);
            Controls.Add(label19);
            Controls.Add(label18);
            Controls.Add(label17);
            Controls.Add(label16);
            Controls.Add(label15);
            Controls.Add(label14);
            Controls.Add(label13);
            Controls.Add(label12);
            Controls.Add(label11);
            Controls.Add(_btnLoadCircuitData);
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
            Controls.Add(_textVehicle);
            Controls.Add(label1);
            Name = "EvChargeSessionForm";
            Text = "EV Charge Session";
            ((System.ComponentModel.ISupportInitialize)_numOdometer).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numStartSoc).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numEndSoc).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numKwhAdded).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numSessionCost).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox _textVehicle;
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
        private Button _btnLoadCircuitData;
        private Label label11;
        private Label label12;
        private Label label13;
        private Label label14;
        private Label label15;
        private Label label16;
        private Label label17;
        private Label label18;
        private Label label19;
        private TextBox _textSocAdded;
        private TextBox _textSocUsed;
        private TextBox _textMilesUsed;
        private TextBox _textKwhUsed;
        private TextBox _textCostPerMile;
        private TextBox _textWhPerMile;
        private TextBox _textMpge;
        private TextBox _textGridKwh;
        private TextBox _textSolarKwh;
        private TextBox textPreviousOdometer;
        private Label label20;
        private TextBox textBox1;
        private Label label21;
        private TextBox _textBatteryKwh;
        private Label label22;
        private Button _btnSave;
    }
}