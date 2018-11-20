using System;
using DashShared;

namespace Dash
{
    public class TextController : FieldModelController<TextModel>
    {
        private string _lowerData = "";
        public TextController() : this("")
        {
        }

        public TextController(string data) : base(new TextModel(data))
        {
        }

        public TextController(TextModel textFieldModel) : base(textFieldModel)
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
        public override bool TrySetValue(object value)
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
                    string data = TextFieldModel.Data;
                    var newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    _lowerData = value.ToLower();
                    TextFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
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

        public override StringSearchModel SearchForString(string searchString, Search.SearchOptions options)
        {
            return options.Matches(Data);

            int maxStringSize = 125;
            int textDecrementForContext = 8;

            _lowerData = String.IsNullOrEmpty(_lowerData) ? ((Model as TextModel)?.Data?.ToLower() ?? "") : _lowerData;

            if (searchString == null)
                return new StringSearchModel("");

            if (Data != null)
            {
                if (options.Regex != null)
                {
                    if (options.Regex.IsMatch(Data))
                    {

                    }
                }
            }
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc = null)
        {
            return "\"" + Data + "\"";
        }

        public override FieldControllerBase Copy()
        {
            return new TextController(Data);
        }
    }
}
