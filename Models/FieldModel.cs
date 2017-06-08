using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class FieldModel
    {
        public string Key { get; set; }

        public abstract UIElement MakeView(TemplateModel template);
    }
}
