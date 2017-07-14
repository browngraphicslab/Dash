using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;

namespace Dash
{
    public abstract class ReferenceFieldModelController : FieldModelController
    {
        protected ReferenceFieldModelController(ReferenceFieldModel model) : base(model)
        {
            // bcz: TODO check DocContextList - maybe this should come from the constructor?
            //var fmc = ContentController.DereferenceToRootFieldModel(this);//TODO Uncomment this
            //var fmc = ContentController.GetController<DocumentController>(ReferenceFieldModel.DocId).GetDereferencedField(ReferenceFieldModel.FieldKey, DocContextList);
            var fmc = DereferenceToRoot(null);
            if (fmc != null)
                fmc.FieldModelUpdated += sender => FireFieldModelUpdated();
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

        public override TypeInfo TypeInfo => TypeInfo.Reference;

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceFieldModel ReferenceFieldModel => FieldModel as ReferenceFieldModel;

        public abstract DocumentController GetDocumentController(Context context);

        public abstract ReferenceFieldModelController Resolve(Context context);

        public sealed override FieldModelController Dereference(Context context)
        {
            if (context != null)
            {
                FieldModelController controller;
                if (context.TryDereferenceToRoot(this, out controller))
                {
                    return controller;
                }
            }
            return GetDocumentController(context).GetField(FieldKey, context);
        }

        public sealed override FieldModelController DereferenceToRoot(Context context)
        {
            FieldModelController reference = this;
            while (reference is ReferenceFieldModelController)
            {
                reference = reference.Dereference(context);
            }
            if (reference == null)
            {
                return null;
            }
            if (reference.InputReference != null)
            {
                return reference.InputReference.DereferenceToRoot(context);
            }
            return reference;
        }

        public sealed override T DereferenceToRoot<T>(Context context)
        {
            return DereferenceToRoot(context) as T;
        }

        public override ReferenceFieldModelController InputReference
        {
            get { return _inputReference; }
            set
            {
                if (SetProperty(ref FieldModel.InputReference, value.ReferenceFieldModel))
                {
                    _inputReference = value;
                }
            }
        }

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        public override FieldModelController GetDefaultController()
        {
            throw new System.NotImplementedException();
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = $"Reference to a field: {ReferenceFieldModel.FieldKey.Name}";
        }
    }
}
