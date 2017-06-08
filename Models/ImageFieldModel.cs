using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    class ImageFieldModel : FieldModel
    {
        public Image Data { get; set; }
        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }
    }
}
