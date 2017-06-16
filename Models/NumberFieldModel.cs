using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected override void UpdateValue(ReferenceFieldModel fieldReference)
        {
            DocumentController cont = App.Instance.Container.GetRequiredService<DocumentController>();
            NumberFieldModel fm = cont.GetFieldInDocument(fieldReference) as NumberFieldModel;
            if (fm != null)
            {
                Data = fm.Data;
            }

            //    DocumentModel doc = cont.GetDocumentAsync(fieldReference.DocId);
            //    if (doc != null)
            //    {
            //        if (doc.Fields.ContainsKey(fieldReference.FieldKey))
            //        {
            //            var fm = doc.Fields[fieldReference.FieldKey] as NumberFieldModel;
            //            Data = fm.Data;
            //        }
            //    }
            //}
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
            Binding binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Data")
            };
            tb.SetBinding(TextBlock.TextProperty, binding);
            if (textTemplate != null)//TODO remove this check
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
