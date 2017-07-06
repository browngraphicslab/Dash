using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class TextFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="TextFieldModelController"/> associated with the passed in <see cref="TextFieldModel" />
        /// </summary>
        /// <param name="textFieldModel">The model which this controller will be operating over</param>
        public TextFieldModelController(TextFieldModel textFieldModel) : base(textFieldModel)
        {
            TextFieldModel = textFieldModel;
        }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public TextFieldModel TextFieldModel { get; }

        public string Data
        {
            get { return TextFieldModel.Data; }
            set
            {
                if (SetProperty(ref TextFieldModel.Data, value))
                {
                    // update local
                    // update server
                }
                FireFieldModelUpdated();
            }
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
                Data = (fieldModel as TextFieldModelController).Data;
            
        }

        public override FrameworkElement GetTableCellView()
        {
            var textBlockText = TextFieldModel.Data;
            return GetTableCellViewOfScrollableText(textBlockText);
        }

        public override string ToString()
        {
            return Data;
        }
    }
}