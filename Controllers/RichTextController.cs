using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class RichTextController: FieldModelController<RichTextModel>
    {
        public RichTextController(): base(new RichTextModel()) { }
        public RichTextController(RichTextModel.RTD data):base(new RichTextModel(data)) { }

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
                if (RichTextFieldModel.Data == value) return;
                RichTextFieldModel.Data = value;
                OnFieldModelUpdated(null);
            }
        }
        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
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
            var links = Data.ReadableString.Split(new string[] { "HYPERLINK" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var link in links)
            {
                var split = link.Split('\"');
                if (split.Count() > 1)
                {
                    var doc = ContentController<FieldModel>.GetController<DocumentController>(split[1]);
                    if (doc != null)
                        yield return doc;
                }
            }
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            int maxStringSize = 125;
            int textDecrementForContext = 15;

            var lowerData = Data.ReadableString.ToLower();
            if (lowerData.Contains(searchString))
            {
                var index = lowerData.IndexOf(searchString);
                index = Math.Max(0, index - textDecrementForContext);
                var substring = Data.ReadableString.Substring(index, Math.Min(maxStringSize, Data.ReadableString.Length - index));
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
            return Data.ReadableString;
        }

        public override FieldModelController<RichTextModel> Copy()
        {
            return new RichTextController(Data);
        }
    }
}
