using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class NumberFieldModel : FieldModel
    {
        public NumberFieldModel() { }

        public NumberFieldModel(double data)
        {
            Data = data;
        }

        private double _data;
        public double Data
        {
            get { return _data; }
            set { SetProperty(ref _data, value); }
        }

        /// <summary>
        /// Update the value of the FieldModel and send update event for the field
        /// </summary>
        /// <param name="fieldReference"></param>
        protected override void UpdateValue(ReferenceFieldModel fieldReference)
        {
            DocumentEndpoint cont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            NumberFieldModel fm = cont.GetFieldInDocument(fieldReference) as NumberFieldModel;
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
            TextTemplateModel textTemplate = template as TextTemplateModel;
            // TODO commented out for debugging 
            //Debug.Assert(textTemplate != null);

            TextBlock tb = new TextBlock();
            //Create binding for the text block
            Binding binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Data")
            };
            tb.SetBinding(TextBlock.TextProperty, binding);
            if (textTemplate != null)                   //TODO remove this check
            {
                Canvas.SetTop(tb, textTemplate.Top);
                Canvas.SetLeft(tb, textTemplate.Left);
                tb.FontWeight = textTemplate.FontWeight;
                tb.TextWrapping = textTemplate.TextWrapping;
                tb.Visibility = textTemplate.Visibility;
            }
            return tb;
        }
    }
}
