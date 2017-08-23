 using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

namespace Dash
{
    public class TextFieldModelController : FieldModelController
    {
        public TextFieldModelController(string data) : base(new TextFieldModel(data))
        {
            Data = data; 
        }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public TextFieldModel TextFieldModel => FieldModel as TextFieldModel;

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
        {
            if (value is string)
            {
                Data = value as string;
                return true;
            }
            return false;
        }
        public string Data
        {
            get { return TextFieldModel.Data; }
            set
            {
                if (SetProperty(ref TextFieldModel.Data, value))
                {
                    OnFieldModelUpdated(null);
                    // update local
                    // update server
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Data = (fieldModel as TextFieldModelController).Data;
        }
        
        public override FieldModelController GetDefaultController()
        {
            return new TextFieldModelController("Default Value");
        }

        public override string ToString()
        {
            return Data;
        }

        public override FieldModelController Copy()
        {
            return new TextFieldModelController(Data);
        }
    }
}