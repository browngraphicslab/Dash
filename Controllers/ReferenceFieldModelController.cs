using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using DashShared.Models;

namespace Dash
{
    public class ReferenceFieldModelController : FieldModelController<ReferenceFieldModel>
    {
        public ReferenceFieldModelController(FieldReference reference) : base(new ReferenceFieldModel(reference))
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);
            var docController = reference.GetDocumentController(null);
            docController.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public ReferenceFieldModelController(string documentId, KeyControllerBase fieldKey) : this(
            new DocumentFieldReference(documentId, fieldKey))
        { }

        public ReferenceFieldModelController(FieldReference documentReference, KeyControllerBase fieldKey) : this(
            new DocumentPointerFieldReference(documentReference, fieldKey))
        {
        }

        private ReferenceFieldModelController(ReferenceFieldModel referenceFieldModel) : base(referenceFieldModel)
        {

        }

        public static ReferenceFieldModelController CreateFromServer(ReferenceFieldModel referenceFieldModel)
        {
            return ContentController<FieldModel>.GetController<ReferenceFieldModelController>(referenceFieldModel.Id) ??
                    new ReferenceFieldModelController(referenceFieldModel);
        }

        private void DocFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            OnFieldModelUpdated(new FieldUpdatedEventArgs(TypeInfo.Reference, args.Action), args.Context);
        }

        public override void Dispose()
        {
            var docController = FieldReference.GetDocumentController(null);
            docController.RemoveFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public FieldReference FieldReference
        {
            get { return ReferenceFieldModel.Reference; }
            set
            {
                ReferenceFieldModel.Reference = value;
                // Update the server
                RESTClient.Instance.Fields.UpdateField(Model, dto =>
                {

                }, exception =>
                {

                });
            }
        }

        public KeyControllerBase FieldKey => FieldReference.FieldKey;

        public DocumentController GetDocumentController(Context context)
        {
            return FieldReference.GetDocumentController(context);
        }

        public override IEnumerable<DocumentController> GetReferences()
        {
            yield return GetDocumentController(null);
        }

        public override FieldControllerBase Dereference(Context context)
        {
            return FieldReference.Dereference(context);
        }

        public override FieldControllerBase DereferenceToRoot(Context context)
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
        public ReferenceFieldModel ReferenceFieldModel => Model as ReferenceFieldModel;

        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewOfScrollableText((tb) => BindTextOrSetOnce(tb, context));
        }

        public override FieldControllerBase GetDefaultController()
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
                Converter = new BoundReferenceToStringConverter(context),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
        }

        public override FieldModelController<ReferenceFieldModel> Copy()
        {
            return new ReferenceFieldModelController(FieldReference);
        }
        public override object GetValue(Context context)
        {
            var refDoc = FieldReference.GetDocumentController(context);
            var opField = refDoc.GetDereferencedField(OperatorDocumentModel.OperatorKey, context) as OperatorFieldModelController;
            if (opField != null)
            {
                var str = "=" + (opField.Model as OperatorFieldModel).Type + "(";
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
            var field = doc.GetDereferencedField<FieldControllerBase>(FieldKey, refValue.Item1);
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
