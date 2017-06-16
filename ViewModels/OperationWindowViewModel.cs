using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Dash.ViewModels
{
    class OperationWindowViewModel
    {
        public ObservableCollection<Button> Buttons { get; set; } = new ObservableCollection<Button>();

        public OperationWindowViewModel()
        {
            for (int i = 0; i < 10; ++i)
            {
                Button b = new Button {Content = "Test"};
                Buttons.Add(b);
            }
        }
    }
}
