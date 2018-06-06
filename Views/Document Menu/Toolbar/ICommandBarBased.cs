using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Dash.Views.Document_Menu.Toolbar
{
    public interface ICommandBarBased
    {
        void CommandBarOpen(bool status);
        ComboBox GetComboBox();
    }
}
