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
using Windows.Foundation;
using Windows.UI.Xaml.Media;

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
        protected override List<UIElement> MakeView(FieldModel fieldModel)
        {
            if (fieldModel == null && DefaultText == null)
                return null;

            Binding binding = new Binding
            {
                Source = fieldModel,
                Path = new PropertyPath("Data")
            };

            var tb = Editable && fieldModel is TextFieldModel ? (FrameworkElement)new TextBox() : new TextBlock();
            if (tb is TextBox)
            {
                tb.SetBinding(TextBox.TextProperty, binding);
                (tb as TextBox).TextChanged += ((s, e) => (fieldModel as TextFieldModel).Data = (s as TextBox).Text);
                (tb as TextBox).FontWeight = FontWeight;
                (tb as TextBox).TextWrapping = TextWrapping;
            }
            else
            {
                if (fieldModel == null)
                    (tb as TextBlock).Text = DefaultText;
                else tb.SetBinding(TextBlock.TextProperty, binding);
                (tb as TextBlock).FontWeight = FontWeight;
                (tb as TextBlock).TextWrapping = TextWrapping;
            }

            var txf = new TranslateTransform();
            txf.Y = Top;
            txf.X = Left;
            tb.RenderTransform = txf;
            tb.Visibility = Visibility;

            return new List<UIElement>(new UIElement[] { tb });
        }
    }
}
