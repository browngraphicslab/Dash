using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /// <summary>
    /// Field model for holding text data
    /// </summary>
    class TextFieldModel : FieldModel
    {
        public TextFieldModel() { }

        /// <summary>
        /// Create a new text field model with the passed in string as data
        /// </summary>
        /// <param name="data"></param>
        public TextFieldModel(string data)
        {
            Data = data;
        }

        private string _data;

        /// <summary>
        /// The text which is the field model contains
        /// </summary>
        public string Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }

        protected override void UpdateValue(ReferenceFieldModel fieldReference)
        {
            var documentEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            TextFieldModel fm = documentEndpoint.GetFieldInDocument(fieldReference) as TextFieldModel;
            if (fm != null)
            {
                Data = fm.Data;
            }
        }

        /// <summary>
        /// Creates TextBlock using layout information from template and Data 
        /// </summary>
        public override UIElement MakeView(TemplateModel template)
        {
            var textTemplate = template as TextTemplateModel;
            Debug.Assert(textTemplate != null);

            var binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Data")
            };

            var tb = textTemplate.Editable ? (FrameworkElement)new TextBox() : new TextBlock();
            if (tb is TextBox)
            {
                tb.SetBinding(TextBox.TextProperty, binding);
                (tb as TextBox).TextChanged += ((s,e) => Data = (s as TextBox).Text);
                (tb as TextBox).FontWeight   = textTemplate.FontWeight;
                (tb as TextBox).TextWrapping = textTemplate.TextWrapping;
            } else
            {
                tb.SetBinding(TextBlock.TextProperty, binding);
                (tb as TextBlock).FontWeight   = textTemplate.FontWeight;
                (tb as TextBlock).TextWrapping = textTemplate.TextWrapping;
            }

            Canvas.SetTop(tb, textTemplate.Top);
            Canvas.SetLeft(tb, textTemplate.Left);
            tb.Visibility = textTemplate.Visibility;

            return tb;
        }
    }
}
