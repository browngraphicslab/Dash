using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class RichTextController : FieldModelController<RichTextModel>
    {
        public RichTextController() : base(new RichTextModel())
        {
        }

        public RichTextController(RichTextModel.RTD data) : base(new RichTextModel(data))
        {
        }

        public RichTextController(RichTextModel richTextFieldModel) : base(richTextFieldModel)
        {

        }

        /// <summary>
        /// The <see cref="RichTextFieldModel"/> associated with this <see cref="RichTextController"/>
        /// </summary>
        public RichTextModel RichTextFieldModel => Model as RichTextModel;

        public RichTextModel.RTD Data
        {
            get => RichTextFieldModel.Data;
            set
            {
                if (RichTextFieldModel.Data != value)
                {
                    RichTextModel.RTD data = RichTextFieldModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    RichTextFieldModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override object GetValue()
        {
            return Data;
        }
        public override bool TrySetValue(object value)
        {
            if (value is RichTextModel.RTD rtd)
            {
                Data = rtd;
                return true;
            }
            return false;
        }
        public ITextSelection SelectedText { get; set; }

        public override TypeInfo TypeInfo => TypeInfo.RichText;

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            return StringSearchModel.False;
            var richEditBox = new RichEditBox();
            richEditBox.Document.SetText(TextSetOptions.FormatRtf, RichTextFieldModel.Data.RtfFormatString);
            richEditBox.Document.GetText(TextGetOptions.UseObjectText, out string readableText);
            readableText = readableText.Replace("\r", "\n");
            return matcher.Matches(readableText);
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "RichTextController";
        }

        public StringSearchModel SearchForStringInRichText(string searchString)
        {
            int maxStringSize = 125;
            int textDecrementForContext = 8;

            var lowerData = Data.RtfFormatString.ToLower();
            var index = lowerData.IndexOf(searchString);
            if (index >= 0)
            {
                index = Math.Max(0, index - textDecrementForContext);
                var substring = Data.RtfFormatString.Substring(index, Math.Min(maxStringSize, Data.RtfFormatString.Length - index));
                return new StringSearchModel(substring);
            }
            return StringSearchModel.False;
        }
        
        public override FieldControllerBase GetDefaultController()
        {
            return new RichTextController(new RichTextModel.RTD("Default Value"));
        }

        public override string ToString()
        {
            return "RichTextController";
        }

        public override FieldControllerBase Copy()
        {
            return new RichTextController(new RichTextModel.RTD(Data.RtfFormatString));
        }
    }
}
