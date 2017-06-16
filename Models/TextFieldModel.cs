using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Dash
{
    /// <summary>
    /// Field model for holding text data
    /// </summary>
    class TextFieldModel : FieldModel
    {
        public TextFieldModel() { }

        public TextFieldModel(string data)
        {
            Data = data;
        }

        private string _data;
        public string Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }

        /// <summary>
        /// Creates TextBlock using layout information from template and Data 
        /// </summary>
        public override UIElement MakeView(TemplateModel template)
        {
            TextTemplateModel textTemplate = template as TextTemplateModel;
            Debug.Assert(textTemplate != null);
        //    TextViewModel vm = new TextViewModel(this, template);
            TextBlock tb = new TextBlock();
            Binding binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Data")
            };
            tb.SetBinding(TextBlock.TextProperty, binding);
            Canvas.SetTop(tb, textTemplate.Top);
            Canvas.SetLeft(tb, textTemplate.Left);
            tb.FontWeight = textTemplate.FontWeight;
            tb.TextWrapping = textTemplate.TextWrapping;
            tb.Visibility = textTemplate.Visibility;
            return tb;
        }
    }
}
