using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash.Models
{
    public class LayoutModelFieldModel : FieldModel
    {
        private LayoutModel _data;
        public LayoutModelFieldModel(LayoutModel layout)
        {
            _data = layout;
        }

        public LayoutModel Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }
    }
}
