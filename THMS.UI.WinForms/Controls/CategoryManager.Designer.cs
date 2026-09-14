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
            manager = new CategoryManagerControl();
            SuspendLayout();
            manager.Dock = DockStyle.Fill;
            manager.Name = "manager";
            AcceptButton = manager.SaveButton;
            CancelButton = manager.CloseButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(780, 480);
            Controls.Add(manager);
            MinimumSize = new Size(560, 400);
            Name = "CategoryManager";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Category Manager";
            ResumeLayout(false);
        }

        #endregion

        private CategoryManagerControl manager;
    }
}
