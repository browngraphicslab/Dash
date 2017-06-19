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
        public override FrameworkElement MakeView(TemplateModel template)
        {
            var textTemplate = template as TextTemplateModel;
            Debug.Assert(textTemplate != null);

            // bind to editable text if the textTemplate is editable
            var textBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Data")
            };

            var tb = textTemplate.Editable ? (FrameworkElement)new TextBox() : new TextBlock();

            // if we are editable
            if (tb is TextBox)
            {
                tb.SetBinding(TextBox.TextProperty, textBinding);
                (tb as TextBox).TextChanged += ((s,e) => Data = (s as TextBox).Text);
                (tb as TextBox).FontWeight   = textTemplate.FontWeight;
                (tb as TextBox).TextWrapping = textTemplate.TextWrapping;
            }
            // if we are not editable
            else if (tb is TextBlock)
            {
                tb.SetBinding(TextBlock.TextProperty, textBinding);
                (tb as TextBlock).FontWeight   = textTemplate.FontWeight;
                (tb as TextBlock).TextWrapping = textTemplate.TextWrapping;
            }

            // move the text when its left property changes
            var leftBinding = new Binding
            {
                Source = textTemplate,
                Path = new PropertyPath("Left"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(Canvas.LeftProperty, leftBinding);

            // move the text when its top property changes
            var topBinding = new Binding
            {
                Source = textTemplate,
                Path = new PropertyPath("Top"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(Canvas.TopProperty, topBinding);

            // resize the text when its width property changes
            var widthBinding = new Binding
            {
                Source = textTemplate,
                Path = new PropertyPath("Width"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(TextBlock.WidthProperty, widthBinding);

            // resize the text when its height property changes
            var heightBinding = new Binding
            {
                Source = textTemplate,
                Path = new PropertyPath("Height"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(TextBlock.HeightProperty, heightBinding);
            tb.Visibility = textTemplate.Visibility;

            return tb;
        }
    }
}
