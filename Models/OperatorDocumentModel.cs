using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class OperatorDocumentModel : DocumentModel
    {
        public static Key OperatorKey = new Key("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A");


        /// <summary>
        /// Dictionary that maps Field input name to the ReferenceFieldModel that it is set to
        /// </summary>
        protected Dictionary<Key, ReferenceFieldModel> InputReferences { get; set; } = new Dictionary<Key, ReferenceFieldModel>();

        protected OperatorFieldModel OperatorField { get; set; }

        public OperatorDocumentModel(OperatorFieldModel operatorField)
        {
            Fields[OperatorKey] = operatorField;
            OperatorField = operatorField;
        }

        public void AddInputReference(Key fieldKey, ReferenceFieldModel reference)
        {
            // remove the output reference of previous input reference document FieldModel 
            //if (InputReferences.ContainsKey(fieldName))
            //{
            //    FieldModel fm = DocumentController.GetFieldFromDocument(InputReferences[fieldName]);
            //    fm.RemoveOutputReference(this);
            //}
            InputReferences[fieldKey] = reference;
            DocumentController docController = App.Instance.Container.GetRequiredService<DocumentController>();
            docController.GetDocumentAsync(reference.DocId).DocumentFieldUpdated += OperatorDocumentModel_DocumentFieldUpdated;
        }

        private void OperatorDocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            if (!InputReferences.ContainsValue(fieldReference))
            {
                return;
            }
            var results = OperatorField.Execute(InputReferences);
            foreach (var fieldModel in results)
            {
                Fields[fieldModel.Key] = fieldModel.Value;
            }
        }
    }
}
