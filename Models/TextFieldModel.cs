using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    class TextFieldModel : FieldModel
    {
        public TextFieldModel()
        {
            
        }

        public TextFieldModel(string data)
        {
            Data = data;
        }

        public string Data { get; set; }

        public override UIElement MakeView(TemplateModel template)
        {
            TextTemplateModel textTemplate = template as TextTemplateModel;
            Debug.Assert(textTemplate != null);
        //    TextViewModel vm = new TextViewModel(this, template);
            TextBlock tb = new TextBlock
            {
                Text = Data
            };
            Canvas.SetTop(tb, textTemplate.Top);
            Canvas.SetLeft(tb, textTemplate.Left);
            tb.FontWeight = textTemplate.FontWeight;
            tb.TextWrapping = textTemplate.TextWrapping;
            tb.Visibility = textTemplate.Visibility;
            return tb;
        }
    }
}
