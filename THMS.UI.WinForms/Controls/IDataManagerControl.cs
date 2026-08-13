using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.UI.WinForms.Controls
{
    public interface IDataManagerControl
    {
        Control GetControl();
        void SetGridDataSource(string period);
    }
}
