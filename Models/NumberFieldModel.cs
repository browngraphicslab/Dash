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
            DocumentEndpoint cont = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
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
        
    }
}
