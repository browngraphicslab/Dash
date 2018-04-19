using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class DishReplViewModel : ViewModelBase
    {
        public ObservableCollection<ReplLineViewModel> Items { get; set; } =
            new ObservableCollection<ReplLineViewModel>();
    }
}
