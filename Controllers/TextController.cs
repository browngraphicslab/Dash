﻿ using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
 using DashShared.Models;

namespace Dash
{
    public class TextController : FieldModelController<TextModel>
    {
        public TextController(string data) : base(new TextModel(data))
        {
        }

        public TextController(TextModel textFieldModel) : base(textFieldModel)
        {
        }

        public override void Init()
        {

        }

        /// <summary>
        ///     The <see cref="TextFieldModel" /> associated with this <see cref="TextController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public TextModel TextFieldModel => Model as TextModel;

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
                    UpdateOnServer();
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        public override FieldControllerBase GetDefaultController()
        {
            return new TextController("Default Value");
        }

        public override string ToString()
        {
            return Data;
        }

        public override FieldModelController<TextModel> Copy()
        {
            return new TextController(Data);
        }
    }
}