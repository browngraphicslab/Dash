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
         
        public TextTemplateModel(double left, double top, FontWeight weight, TextWrapping wrap = TextWrapping.NoWrap, Visibility visibility = Visibility.Visible)
            : base(left, top, 0, 0, visibility)
        {
            FontWeight = weight;
            TextWrapping = wrap;
        }

        public override FieldViewModel CreateViewModel(FieldModel field)
        {
            return new TextViewModel(field, this);
        }
    }
}
