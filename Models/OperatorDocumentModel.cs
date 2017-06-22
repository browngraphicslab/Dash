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
        public static Key OperatorKey = new Key("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static DocumentType OperatorType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");

        /// <summary>
        /// Dictionary that maps Field input name to the ReferenceFieldModel that it is set to
        /// </summary>

        protected Dictionary<Key, ReferenceFieldModel> InputReferences { get; set; } = new Dictionary<Key, ReferenceFieldModel>();

        public OperatorFieldModel OperatorField
        {
            get { return Field(OperatorKey) as OperatorFieldModel; }
            set
            {
                value.DocumentID = Id;
                SetField(OperatorKey, value);
            }
        }

        public OperatorDocumentModel(OperatorFieldModel operatorField, string documentID)
        {
            // Fields = new Dictionary<Key, FieldModel>();
            Id = documentID;
            SetField(OperatorKey, operatorField);
            OperatorField = operatorField;
            DocumentType = OperatorType;
        }

        public override void AddInputReference(Key fieldKey, ReferenceFieldModel reference)
        {
            DocumentEndpoint docEndpoint = App.Instance.Container.GetRequiredService<DocumentEndpoint>();

            //TODO Remove existing output references and add new output reference
            //if (InputReferences.ContainsKey(fieldKey))
            //{
            //    FieldModel fm = docEndpoint.GetFieldInDocument(InputReferences[fieldKey]);
            //    fm.RemoveOutputReference(new ReferenceFieldModel {DocId = Id, Key = fieldKey});
            //}
            InputReferences[fieldKey] = reference;
            docEndpoint.GetFieldInDocument(reference).FieldUpdated += OperatorDocumentModel_FieldUpdated;
            Execute();
        }

        private void OperatorDocumentModel_FieldUpdated(FieldModel model)
        {
            Execute();
        }

        private void Execute()
        {
            Dictionary<Key, FieldModel> results;
            try
            {
                results = OperatorField.Execute(InputReferences);
            }
            catch (KeyNotFoundException e)
            {
                return;
            }
            foreach (var fieldModel in results)
            {
                SetField(fieldModel.Key, fieldModel.Value);
            }
        }
    }
}
