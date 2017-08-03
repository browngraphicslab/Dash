using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;
using System.Diagnostics;

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
            docController.AddFieldUpdatedListener(FieldKey, DocFieldUpdated);
        }

        public ReferenceFieldModelController(string documentId, Key fieldKey) : this(
            new DocumentFieldReference(documentId, fieldKey))
        { }

        public ReferenceFieldModelController(FieldReference documentReference, Key fieldKey) : this(
            new DocumentPointerFieldReference(documentReference, fieldKey))
        {
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
            set { ReferenceFieldModel.Reference = value; }
        }

        public Key FieldKey => FieldReference.FieldKey;

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

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        public override FieldModelController GetDefaultController()
        {
            throw new NotImplementedException();
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = $"Reference to a field: {FieldKey.Name}";
        }

        public override FieldModelController Copy()
        {
            return new ReferenceFieldModelController(FieldReference);
        }
    }
}
