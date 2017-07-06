using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class ReferenceFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="ReferenceFieldModelController"/> associated with the passed in <see cref="ReferenceFieldModel" />
        /// </summary>
        /// <param name="referenceFieldModel">The model which this controller will be operating over</param>
        public ReferenceFieldModelController(ReferenceFieldModel referenceFieldModel) : base(referenceFieldModel)
        {
            ReferenceFieldModel = referenceFieldModel;
            var fmc = (ContentController.GetController(referenceFieldModel.DocId) as DocumentController).GetField(referenceFieldModel.FieldKey);
            fmc.FieldModelUpdatedEvent += Fmc_FieldModelUpdatedEvent;
        }

        private void Fmc_FieldModelUpdatedEvent(FieldModelController sender)
        {
            FireFieldModelUpdated();
        }

        /// <summary>
        ///     The <see cref="ReferenceFieldModel" /> associated with this <see cref="ReferenceFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public ReferenceFieldModel ReferenceFieldModel { get; }

        public override FrameworkElement GetTableCellView()
        {
            var textBlockText = $"Reference to a field: {ReferenceFieldModel.FieldKey.Name}";
            return GetTableCellViewOfScrollableText(textBlockText);
        }
    }
}
