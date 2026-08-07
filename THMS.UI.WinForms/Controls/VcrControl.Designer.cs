using static System.Windows.Forms.DataFormats;

namespace THMS.UI.WinForms.Controls
{
    partial class VcrControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            _btnBack = new Button();
            _datePicker = new DateTimePicker();
            _btnForward = new Button();
            layout.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            layout.Controls.Add(_btnBack, 0, 0);
            layout.Controls.Add(_datePicker, 1, 0);
            layout.Controls.Add(_btnForward, 2, 0);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(4, 4);
            layout.Name = "layout";
            layout.RowCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.Size = new Size(142, 32);
            layout.TabIndex = 0;
            // 
            // _btnBack
            // 
            _btnBack.Dock = DockStyle.Fill;
            _btnBack.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _btnBack.Location = new Point(3, 3);
            _btnBack.Name = "_btnBack";
            _btnBack.Size = new Size(34, 26);
            _btnBack.TabIndex = 0;
            _btnBack.Text = "◀";
            _btnBack.Click += OnClickBack;
            // 
            // _datePicker
            // 
            _datePicker.Dock = DockStyle.Fill;
            _datePicker.Format = DateTimePickerFormat.Short;
            _datePicker.Location = new Point(43, 3);
            _datePicker.Name = "_datePicker";
            _datePicker.Size = new Size(56, 35);
            _datePicker.TabIndex = 1;
            _datePicker.ValueChanged += OnValueChangedDate;
            // 
            // _btnForward
            // 
            _btnForward.Dock = DockStyle.Fill;
            _btnForward.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _btnForward.Location = new Point(105, 3);
            _btnForward.Name = "_btnForward";
            _btnForward.Size = new Size(34, 26);
            _btnForward.TabIndex = 2;
            _btnForward.Text = "▶";
            _btnForward.Click += OnClickForward;
            // 
            // VcrControl
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(layout);
            Name = "VcrControl";
            Padding = new Padding(4);
            Size = new Size(150, 40);
            layout.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layout;
        private Button _btnBack;
        private Button _btnForward;
        private DateTimePicker _datePicker;
    }
}
