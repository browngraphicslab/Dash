using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Dash.Views.Document_Menu.Toolbar
{
    interface ICommandBarBased
    {
        CommandBar GetCommandBar();
    }
}
