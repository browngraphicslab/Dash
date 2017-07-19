using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dash
{
    public abstract class ReferenceFieldModelController : FieldModelController
    {
        protected ReferenceFieldModelController(ReferenceFieldModel model) : base(model)
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);
            var docController = GetDocumentController(null);
            if (docController != null)
            {
                docController.AddFieldUpdatedListener(FieldKey, delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    Debug.Assert(args.Reference.FieldKey.Equals(FieldKey));
                    OnFieldModelUpdated(args.Context);
                });
            }
        }

        public Key FieldKey
        {
            get { return ReferenceFieldModel.FieldKey; }
            set
            {
                if (SetProperty(ref ReferenceFieldModel.FieldKey, value))
                {

                }
            }
        }

        public string GetDocumentId(Context context = null)
        {
            return GetDocumentController(context)?.GetId();
        }

        public override TypeInfo TypeInfo => TypeInfo.Reference;

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceFieldModel ReferenceFieldModel => FieldModel as ReferenceFieldModel;

        public abstract DocumentController GetDocumentController(Context context);

        /// <summary>
        /// Resolve this reference field model to the lowest delegate in the given context
        /// </summary>
        /// <param name="context">Context to look for delegates in</param>
        /// <returns>A new FieldModelController that points to the same field in the lowest delegate of the pointed to document</returns>
        public abstract ReferenceFieldModelController Resolve(Context context);

        public sealed override FieldModelController Dereference(Context context)
        {
            FieldModelController controller;
            if (context != null)
            {
                if (context.TryDereferenceToRoot(this, out controller))
                {
                    return controller;
                }
            }
            DocumentController doc = GetDocumentController(context);
            context = context ?? new Context();
            doc.Execute(context, false);
            if (context.TryDereferenceToRoot(this, out controller))
            {
                return controller;
            }
            var fmc = GetDocumentController(context)?.GetField(FieldKey);
            return fmc;
        }

        public sealed override FieldModelController DereferenceToRoot(Context context)
        {
            FieldModelController reference = this;
            while (reference is ReferenceFieldModelController)
            {
                reference = reference.Dereference(context);
            }
            //if (reference == null)
            //{
            //    return null;
            //}
            //if (reference.InputReference != null)
            //{
            //    return reference.InputReference.DereferenceToRoot(context);
            //}
            return reference;
        }

        public sealed override T DereferenceToRoot<T>(Context context)
        {
            return DereferenceToRoot(context) as T;
        }

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
            textBlock.Text = $"Reference to a field: {ReferenceFieldModel.FieldKey.Name}";
        }
    }
}
