namespace Dash
{
    public class TextFieldModelController : FieldModelController
    {
        public TextFieldModelController(string data) : base(new TextFieldModel(data)) { }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public TextFieldModel TextFieldModel => FieldModel as TextFieldModel;

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

        public override string ToString()
        {
            return Data;
        }
    }
}