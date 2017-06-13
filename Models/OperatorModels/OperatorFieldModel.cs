using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class OperatorFieldModel : FieldModel
    {
        /// <summary>
        /// Dictionary that maps Field input name to the ReferenceFieldModel that it is set to
        /// </summary>
        protected Dictionary<string, ReferenceFieldModel> InputReferences { get; set; } = new Dictionary<string, ReferenceFieldModel>();

        public void AddInputReference(string fieldName, ReferenceFieldModel reference)
        {
            // remove the output reference of previous input reference document FieldModel 
            //if (InputReferences.ContainsKey(fieldName))
            //{
            //    FieldModel fm = DocumentController.GetFieldFromDocument(InputReferences[fieldName]);
            //    fm.RemoveOutputReference(this);
            //}
            InputReferences[fieldName] = reference;
            //DocumentModel doc = DocumentController.GetDocumentWithID(reference.DocId);
            //doc.Fields[reference.FieldKey].Updated += Updated;
        }

        public override UIElement MakeView(TemplateModel template)
        {
            throw new NotImplementedException();
        }

        public abstract Dictionary<String, FieldModel> Execute(/*Dictionary<String, FieldModel> args*/);
    }
}
