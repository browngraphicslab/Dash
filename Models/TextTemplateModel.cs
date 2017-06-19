using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Dash;
using Windows.UI.Text;

namespace Dash
{
    public class TextTemplateModel: TemplateModel
    {
        public FontWeight FontWeight { get; set;  }

        public TextWrapping TextWrapping { get; set; }

        public bool Editable { get; set;  }
         
        public TextTemplateModel(double left, double top, FontWeight weight, TextWrapping wrap = TextWrapping.NoWrap, Visibility visibility = Visibility.Visible, bool editable= false)
            : base(left, top, 200, 50, visibility)
        {
            FontWeight = weight;
            TextWrapping = wrap;
            Editable = editable;
        }
    }
}
