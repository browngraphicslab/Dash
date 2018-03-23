 using System;
 using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
 using DashShared.Models;

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
                    _lowerData = value.ToLower();
                    TextFieldModel.Data = value;
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

        public override StringSearchModel SearchForString(string searchString)
        {
            int maxStringSize = 125;
            int textDecrementForContext = 8;

            _lowerData = String.IsNullOrEmpty(_lowerData) ? ((Model as TextModel)?.Data?.ToLower() ?? "") : _lowerData;

            if (Data != null)
            {
                var index = _lowerData.IndexOf(searchString);
                if (index >= 0)
                {
                    index = Math.Max(0, index - textDecrementForContext);
                    var substring = Data.Substring(index, Math.Min(maxStringSize, Data.Length - index));
                    return new StringSearchModel(substring, true);
                }
            }
            return StringSearchModel.False;
        }

        public override FieldModelController<TextModel> Copy()
        {
            return new TextController(Data);
        }
    }
}