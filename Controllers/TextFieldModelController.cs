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
                    OnDataUpdated();
                    // update local
                    // update server
                }
            }
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Data = (fieldModel as TextFieldModelController).Data;
        }
    }
}