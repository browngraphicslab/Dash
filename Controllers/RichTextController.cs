using System;
using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class RichTextController: FieldModelController<RichTextModel>
    {
        public RichTextController() : base(new RichTextModel())
        {
            SaveOnServer();
        }

        public RichTextController(RichTextModel.RTD data) : base(new RichTextModel(data))
        {
            SaveOnServer();
        }

        public RichTextController(RichTextModel richTextFieldModel) : base(richTextFieldModel)
        {

        }

        public override void Init()
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
                    SetData(value);
                }
            }
        }

        /*
       * Sets the data property and gives UpdateOnServer an UndoCommand 
       */
        private void SetData(RichTextModel.RTD val, bool withUndo = true)
        {
            RichTextModel.RTD data = RichTextFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            RichTextFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }
        public override object GetValue(Context context)
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

        public override IEnumerable<DocumentController> GetReferences()
        {
            yield return null;
            //var links = Data.ReadableString.Split(new string[] { "HYPERLINK" }, StringSplitOptions.RemoveEmptyEntries);
            //foreach (var link in links)
            //{
            //    var split = link.Split('\"');
            //    if (split.Count() > 1)
            //    {
            //        var doc = ContentController<FieldModel>.GetController<DocumentController>(split[1]);
            //        if (doc != null)
            //            yield return doc;
            //    }
            //}
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            var richEditBox = new RichEditBox();
            richEditBox.Document.SetText(TextSetOptions.FormatRtf, RichTextFieldModel.Data.RtfFormatString);
            richEditBox.Document.GetText(TextGetOptions.UseObjectText, out string readableText);
            readableText = readableText.Replace("\r", "\n");
            return readableText.Contains(searchString) ? new StringSearchModel(readableText) : StringSearchModel.False;
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

        // bcz: just want the basic behavior of converting the field into a string.. no need to override.
        //public override FrameworkElement GetTableCellView(Context context)
        //{
        //    var richTextView = new RichTextView()
        //    {
        //        HorizontalAlignment = HorizontalAlignment.Stretch,
        //        VerticalAlignment = VerticalAlignment.Stretch,
        //        TargetRTFController = this
        //    };

        //    return richTextView;
        //}

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
