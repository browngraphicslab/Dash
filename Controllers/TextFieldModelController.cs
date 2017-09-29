 using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
 using DashShared.Models;

namespace Dash
{
    public class TextFieldModelController : FieldModelController<TextFieldModel>
    {
        public TextFieldModelController(string data) : base(new TextFieldModel(data))
        {
        }

        public TextFieldModelController(TextFieldModel textFieldModel) : base(textFieldModel)
        {
        }

        public static TextFieldModelController CreateFromServer(TextFieldModel textFieldModel)
        {
            return ContentController<FieldModel>.GetController<TextFieldModelController>(textFieldModel.Id) ??
                    new TextFieldModelController(textFieldModel);
        }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public TextFieldModel TextFieldModel => Model as TextFieldModel;

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
                if (TextFieldModel.Data != value)
                {
                    TextFieldModel.Data = value;
                    OnFieldModelUpdated(null);
                    // Update the server
                    RESTClient.Instance.Fields.UpdateField(Model, dto =>
                    {

                    }, exception =>
                    {

                    });
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        public override FieldControllerBase GetDefaultController()
        {
            return new TextFieldModelController("Default Value");
        }

        public override string ToString()
        {
            return Data;
        }

        public override FieldModelController<TextFieldModel> Copy()
        {
            return new TextFieldModelController(Data);
        }
    }
}