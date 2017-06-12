using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    class NumberFieldModel : FieldModel
    {
        public NumberFieldModel() { }

        public NumberFieldModel(double data)
        {
            Data = data;
        }

        public double Data { get; set; }

        /// <summary>
        /// Creates TextBlock using layout information from template and Data 
        /// </summary>
        public override UIElement MakeView(TemplateModel template)
        {
            TextTemplateModel textTemplate = template as TextTemplateModel;
            Debug.Assert(textTemplate != null);
            //    TextViewModel vm = new TextViewModel(this, template);
            TextBlock tb = new TextBlock
            {
                Text = Data.ToString(CultureInfo.InvariantCulture)
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
