using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml.Data;
using Dash.Converters;

namespace Dash
{
    public class ReferenceFieldModelController : FieldModelController
    {
        public ReferenceFieldModelController(FieldReference reference) : base(new ReferenceFieldModel(reference))
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);
            var docController = reference.GetDocumentController(null);
            docController?.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public void ChangeFieldDoc(string docId)
        {
            var docController = ReferenceFieldModel.Reference.GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
            if (ReferenceFieldModel.Reference is DocumentFieldReference)
                (ReferenceFieldModel.Reference as DocumentFieldReference).DocumentId = docId;
            var docController2 = ReferenceFieldModel.Reference.GetDocumentController(null);
            docController2.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public ReferenceFieldModelController(string documentId, KeyController fieldKey) : this(
            new DocumentFieldReference(documentId, fieldKey))
        { }

        public ReferenceFieldModelController(FieldReference documentReference, KeyController fieldKey) : this(
            new DocumentPointerFieldReference(documentReference, fieldKey))
        {
        }

        private void DocFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            OnFieldModelUpdated(args.FieldArgs, args.Context);
        }

        public override void Dispose()
        {
            var docController = FieldReference.GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public FieldReference FieldReference
        {
            get { return ReferenceFieldModel.Reference; }
            set { ReferenceFieldModel.Reference = value; }
        }

        public KeyController FieldKey => FieldReference.FieldKey;

        public DocumentController GetDocumentController(Context context)
        {
            return FieldReference.GetDocumentController(context);
        }

        public override IEnumerable<DocumentController> GetReferences()
        {
            yield return GetDocumentController(null);
        }

        public override FieldModelController Dereference(Context context)
        {
            return FieldReference.Dereference(context);
        }

        public override FieldModelController DereferenceToRoot(Context context)
        {
            return FieldReference.DereferenceToRoot(context);
        }

        public string GetDocumentId(Context context)
        {
            return FieldReference.GetDocumentId(context);
        }

        public override TypeInfo TypeInfo => TypeInfo.Reference;

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceFieldModel ReferenceFieldModel => FieldModel as ReferenceFieldModel;

        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewOfScrollableText((tb) => BindTextOrSetOnce(tb, context));
        }

        public override FieldModelController GetDefaultController()
        {
            throw new NotImplementedException();
        }
        
        public DocumentCollectionFieldModelController DocumentCollectionFieldModelController => DereferenceToRoot<DocumentCollectionFieldModelController>(null);
        public DocumentFieldModelController DocumentFieldModelController => DereferenceToRoot<DocumentFieldModelController>(null);

        private void BindTextOrSetOnce(TextBlock textBlock, Context context)
        {
            Binding textBinding = new Binding
            {
                Source = this,
                Converter = new ObjectToStringConverter(context),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
        }

        public override FieldModelController Copy()
        {
            return new ReferenceFieldModelController(FieldReference.Copy());
        }
        public override object GetValue(Context context)
        {
            var refDoc = FieldReference.GetDocumentController(context);
            var opField = refDoc.GetDereferencedField(KeyStore.OperatorKey, context) as OperatorFieldModelController;
            if (opField != null)
            {
                var str = "=" + (opField.FieldModel as OperatorFieldModel).Type + "(";
                foreach (var input in opField.Inputs)
                    str += refDoc.GetField(input.Key)?.GetValue(context)?.ToString().TrimStart('=') + ",";
                str = str.TrimEnd(',') + ")";
                return str;
            }
            return "=" + new DocumentControllerToStringConverter().ConvertDataToXaml(refDoc).Trim('<', '>') + "." + FieldKey.Name;
        }
        public override bool SetValue(object value)
        {
            var refValue = (Tuple<Context,object>)value;
            var doc = GetDocumentController(refValue.Item1);
            var field = doc.GetDereferencedField<FieldModelController>(FieldKey, refValue.Item1);
            if (refValue.Item2 is string)
                return doc.ParseDocField(FieldKey, refValue.Item2 as string, field);
            else if (refValue.Item2 is RichTextFieldModel.RTD)
                return doc.SetField(FieldKey, new RichTextFieldModelController(refValue.Item2 as RichTextFieldModel.RTD), false);
            else
                ;
            return false;
        }
    }
}
