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
        public ReferenceFieldModelController(FieldReference reference) : base(new ReferenceFieldModel(reference), false)
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);
            var docController = reference.GetDocumentController(null);
            docController.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public ReferenceFieldModelController(string documentId, KeyController fieldKey) : this(
            new DocumentFieldReference(documentId, fieldKey))
        { }

        public ReferenceFieldModelController(FieldReference documentReference, KeyController fieldKey) : this(
            new DocumentPointerFieldReference(documentReference, fieldKey))
        {
        }

        private ReferenceFieldModelController(ReferenceFieldModel referenceFieldModel) : base(referenceFieldModel, true)
        {

        }

        public static ReferenceFieldModelController CreateFromServer(ReferenceFieldModel referenceFieldModel)
        {
            return ContentController.GetController<ReferenceFieldModelController>(referenceFieldModel.Id) ??
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
                RESTClient.Instance.Fields.UpdateField(FieldModel, dto =>
                {

                }, exception =>
                {

                });
            }
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
                Converter = new BoundReferenceToStringConverter(context),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
        }

        public override FieldModelController Copy()
        {
            return new ReferenceFieldModelController(FieldReference);
        }
    }
}
