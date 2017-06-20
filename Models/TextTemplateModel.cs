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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using Dash.Models;
using System.Diagnostics;

namespace Dash
{
    public class TextTemplateModel: TemplateModel
    {
        public FontWeight FontWeight { get; set;  }

        public TextWrapping TextWrapping { get; set; }

        public bool Editable { get; set; }

        public string DefaultText { get; set; }

        public TextTemplateModel(double left, double top, FontWeight weight, TextWrapping wrap = TextWrapping.NoWrap, Visibility visibility = Visibility.Visible, bool editable= false, string defaultText=null)
            : base(left, top, 0, 0, visibility)
        {
            FontWeight = weight;
            TextWrapping = wrap;
            Editable = editable;
            DefaultText = defaultText;
        }
        
        /// <summary>
         /// Creates TextBlock using layout information from template and Data 
         /// </summary>
        public override UIElement MakeView(FieldModel fieldModel)
        {
            if (fieldModel == null && DefaultText == null)
                return null;
            if (fieldModel is ImageFieldModel)
                return new ImageTemplateModel(Left,Top,Width,Height,Visibility).MakeView(fieldModel);
            var textFieldModel = fieldModel as TextFieldModel;

            Binding binding = new Binding
            {
                Source = fieldModel,
                Path = new PropertyPath("Data")
            };

            var tb = Editable && textFieldModel != null ? (FrameworkElement)new TextBox() : new TextBlock();
            if (tb is TextBox)
            {
                tb.SetBinding(TextBox.TextProperty, binding);
                (tb as TextBox).TextChanged += ((s, e) => textFieldModel.Data = (s as TextBox).Text);
                (tb as TextBox).FontWeight = FontWeight;
                (tb as TextBox).TextWrapping = TextWrapping;
            }
            else
            {
                if (textFieldModel == null && !(fieldModel is NumberFieldModel))
                    (tb as TextBlock).Text = DefaultText;
                else tb.SetBinding(TextBlock.TextProperty, binding);
                (tb as TextBlock).FontWeight = FontWeight;
                (tb as TextBlock).TextWrapping = TextWrapping;
            }

            Canvas.SetTop(tb, Top);
            Canvas.SetLeft(tb, Left);
            tb.Visibility = Visibility;

            return tb;
        }
    }
}
