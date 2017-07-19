using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash.ViewModels
{
    public class CompositeLayoutViewModel
    {

        public ObservableCollection<DocumentController> LayoutDocs = new ObservableCollection<DocumentController>();
        public ObservableCollection<FrameworkElement> UiElements = new ObservableCollection<FrameworkElement>();
    }
}
