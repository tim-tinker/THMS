namespace THMS.UI.WinForms
{
    partial class ElectricContractDataEntryForm
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
            _dateStart = new DateTimePicker();
            label1 = new Label();
            _textName = new TextBox();
            label2 = new Label();
            _dateEnd = new DateTimePicker();
            label3 = new Label();
            _numBaseEnergyCharge = new NumericUpDown();
            label4 = new Label();
            _numEnergyRate = new NumericUpDown();
            label5 = new Label();
            _numSolarCreditRate = new NumericUpDown();
            label6 = new Label();
            _numDeliveryBaseCharge = new NumericUpDown();
            label7 = new Label();
            _numDeliveryRate = new NumericUpDown();
            label8 = new Label();
            _btnCancel = new Button();
            _btnSave = new Button();
            ((System.ComponentModel.ISupportInitialize)_numBaseEnergyCharge).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numEnergyRate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numSolarCreditRate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numDeliveryBaseCharge).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numDeliveryRate).BeginInit();
            SuspendLayout();
            // 
            // _dateStart
            // 
            _dateStart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateStart.Format = DateTimePickerFormat.Short;
            _dateStart.Location = new Point(226, 53);
            _dateStart.Name = "_dateStart";
            _dateStart.Size = new Size(212, 35);
            _dateStart.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 58);
            label1.Name = "label1";
            label1.Size = new Size(142, 30);
            label1.TabIndex = 1;
            label1.Text = "Effective Date";
            // 
            // _textName
            // 
            _textName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textName.Location = new Point(226, 12);
            _textName.Name = "_textName";
            _textName.Size = new Size(212, 35);
            _textName.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 15);
            label2.Name = "label2";
            label2.Size = new Size(154, 30);
            label2.TabIndex = 3;
            label2.Text = "Contract Name";
            // 
            // _dateEnd
            // 
            _dateEnd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateEnd.Format = DateTimePickerFormat.Short;
            _dateEnd.Location = new Point(226, 94);
            _dateEnd.Name = "_dateEnd";
            _dateEnd.Size = new Size(212, 35);
            _dateEnd.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 98);
            label3.Name = "label3";
            label3.Size = new Size(155, 30);
            label3.TabIndex = 5;
            label3.Text = "Expiration Date";
            // 
            // _numBaseEnergyCharge
            // 
            _numBaseEnergyCharge.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numBaseEnergyCharge.DecimalPlaces = 2;
            _numBaseEnergyCharge.Location = new Point(226, 135);
            _numBaseEnergyCharge.Name = "_numBaseEnergyCharge";
            _numBaseEnergyCharge.Size = new Size(212, 35);
            _numBaseEnergyCharge.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 137);
            label4.Name = "label4";
            label4.Size = new Size(197, 30);
            label4.TabIndex = 7;
            label4.Text = "Base Energy Charge";
            // 
            // _numEnergyRate
            // 
            _numEnergyRate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numEnergyRate.DecimalPlaces = 6;
            _numEnergyRate.Location = new Point(226, 176);
            _numEnergyRate.Name = "_numEnergyRate";
            _numEnergyRate.Size = new Size(212, 35);
            _numEnergyRate.TabIndex = 8;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 178);
            label5.Name = "label5";
            label5.Size = new Size(124, 30);
            label5.TabIndex = 9;
            label5.Text = "Energy Rate";
            // 
            // _numSolarCreditRate
            // 
            _numSolarCreditRate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numSolarCreditRate.DecimalPlaces = 6;
            _numSolarCreditRate.Location = new Point(226, 217);
            _numSolarCreditRate.Name = "_numSolarCreditRate";
            _numSolarCreditRate.Size = new Size(212, 35);
            _numSolarCreditRate.TabIndex = 10;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 219);
            label6.Name = "label6";
            label6.Size = new Size(168, 30);
            label6.TabIndex = 11;
            label6.Text = "Solar Credit Rate";
            // 
            // _numDeliveryBaseCharge
            // 
            _numDeliveryBaseCharge.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numDeliveryBaseCharge.DecimalPlaces = 2;
            _numDeliveryBaseCharge.Location = new Point(226, 258);
            _numDeliveryBaseCharge.Name = "_numDeliveryBaseCharge";
            _numDeliveryBaseCharge.Size = new Size(212, 35);
            _numDeliveryBaseCharge.TabIndex = 12;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 260);
            label7.Name = "label7";
            label7.Size = new Size(208, 30);
            label7.TabIndex = 13;
            label7.Text = "Delivery Base Charge";
            // 
            // _numDeliveryRate
            // 
            _numDeliveryRate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _numDeliveryRate.DecimalPlaces = 6;
            _numDeliveryRate.Location = new Point(226, 299);
            _numDeliveryRate.Name = "_numDeliveryRate";
            _numDeliveryRate.Size = new Size(212, 35);
            _numDeliveryRate.TabIndex = 14;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(12, 301);
            label8.Name = "label8";
            label8.Size = new Size(135, 30);
            label8.TabIndex = 15;
            label8.Text = "Delivery Rate";
            // 
            // _btnCancel
            // 
            _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnCancel.Location = new Point(307, 370);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(131, 40);
            _btnCancel.TabIndex = 16;
            _btnCancel.Text = "Cancel";
            _btnCancel.UseVisualStyleBackColor = true;
            _btnCancel.Click += OnClickCancel;
            // 
            // _btnSave
            // 
            _btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnSave.Location = new Point(170, 370);
            _btnSave.Name = "_btnSave";
            _btnSave.Size = new Size(131, 40);
            _btnSave.TabIndex = 16;
            _btnSave.Text = "Save";
            _btnSave.UseVisualStyleBackColor = true;
            _btnSave.Click += OnClickSave;
            // 
            // ElectricContractDataEntryForm
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(450, 422);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            Controls.Add(label8);
            Controls.Add(_numDeliveryRate);
            Controls.Add(label7);
            Controls.Add(_numDeliveryBaseCharge);
            Controls.Add(label6);
            Controls.Add(_numSolarCreditRate);
            Controls.Add(label5);
            Controls.Add(_numEnergyRate);
            Controls.Add(label4);
            Controls.Add(_numBaseEnergyCharge);
            Controls.Add(label3);
            Controls.Add(_dateEnd);
            Controls.Add(label2);
            Controls.Add(_textName);
            Controls.Add(label1);
            Controls.Add(_dateStart);
            Name = "ElectricContractDataEntryForm";
            Text = "Electric Contract Data";
            ((System.ComponentModel.ISupportInitialize)_numBaseEnergyCharge).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numEnergyRate).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numSolarCreditRate).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numDeliveryBaseCharge).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numDeliveryRate).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DateTimePicker _dateStart;
        private Label label1;
        private TextBox _textName;
        private Label label2;
        private DateTimePicker _dateEnd;
        private Label label3;
        private NumericUpDown _numBaseEnergyCharge;
        private Label label4;
        private NumericUpDown _numEnergyRate;
        private Label label5;
        private NumericUpDown _numSolarCreditRate;
        private Label label6;
        private NumericUpDown _numDeliveryBaseCharge;
        private Label label7;
        private NumericUpDown _numDeliveryRate;
        private Label label8;
        private Button _btnCancel;
        private Button _btnSave;
    }
}