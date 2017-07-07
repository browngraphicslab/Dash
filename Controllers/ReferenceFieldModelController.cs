using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System.Collections.Generic;

namespace Dash
{
    public class ReferenceFieldModelController : FieldModelController
    {
        public ReferenceFieldModelController(string docID, Key key) : base(new ReferenceFieldModel(docID, key))
        {
            var fmc = ContentController.GetController<FieldModelController>(ContentController.DereferenceToRootFieldModel(ReferenceFieldModel).Id);

            if (fmc != null)
                fmc.FieldModelUpdatedEvent += Fmc_FieldModelUpdatedEvent;
        }

        public List<DocumentController> DocContextList = null;

        public string DocId
        {
            get { return ReferenceFieldModel.DocId; }
            set
            {
                if (SetProperty(ref ReferenceFieldModel.DocId, value))
                {
                    
                }
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

        private void Fmc_FieldModelUpdatedEvent(FieldModelController sender)
        {
            FireFieldModelUpdated();
        }

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceFieldModel ReferenceFieldModel => FieldModel as ReferenceFieldModel;

        public override FrameworkElement GetTableCellView()
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            textBlock.Text = $"Reference to a field: {ReferenceFieldModel.FieldKey.Name}";
        }
    }
}
