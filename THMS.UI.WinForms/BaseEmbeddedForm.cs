namespace THMS.UI.WinForms
{
    public class BaseEmbeddedForm : Form
    {
        /// <summary>
        /// Call from MainForm before adding this form to a host panel.
        /// Kept out of the constructor so the Designer can open derived forms.
        /// </summary>
        public void ConfigureAsEmbeddedForm()
        {
            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            DoubleBuffered = true;
        }
    }
}
