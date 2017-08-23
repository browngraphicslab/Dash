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

namespace Dash
{
    public class RichTextFieldModelController: FieldModelController
    {
        public RichTextFieldModelController(): base(new RichTextFieldModel()) { }
        public RichTextFieldModelController(RichTextFieldModel.RTD data):base(new RichTextFieldModel(data)) { }
        /// <summary>
        /// The <see cref="RichTextFieldModel"/> associated with this <see cref="RichTextFieldModelController"/>
        /// </summary>
        public RichTextFieldModel RichTextFieldModel => FieldModel as RichTextFieldModel;

        public RichTextFieldModel.RTD Data
        {
            get { return RichTextFieldModel.Data; }
            set
            {
                if (SetProperty(ref RichTextFieldModel.Data, value))
                {
                    OnFieldModelUpdated(null);
                    // update local
                    // update server
                }

            }
        }
        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
        {
            if (value is RichTextFieldModel.RTD)
            {
                Data = value as RichTextFieldModel.RTD;
                return true;
            }
            return false;
        }
        public ITextSelection SelectedText { get; set; }
        protected override void UpdateValue(FieldModelController fieldModel)
        {
            var richTextFieldModelController = fieldModel as RichTextFieldModelController;
            if (richTextFieldModelController != null) Data = richTextFieldModelController.Data;
        }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        public override IEnumerable<DocumentController> GetReferences()
        {
            var links = Data.ReadableString.Split(new string[] { "HYPERLINK" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var link in links)
            {
                var split = link.Split('\"');
                if (split.Count() > 1)
                {
                    var doc = ContentController.GetController<DocumentController>(split[1]);
                    if (doc != null)
                        yield return doc;
                }
            }
        }

        public override FrameworkElement GetTableCellView(Context context)
        {
            var richTextView = new RichTextView(this, null, null)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            return richTextView;
        }

        public override FieldModelController GetDefaultController()
        {
            return new RichTextFieldModelController(new RichTextFieldModel.RTD("Default Value"));
        }

        public override string ToString()
        {
            return Data.ReadableString;
        }

        public override FieldModelController Copy()
        {
            return new RichTextFieldModelController(Data);
        }
    }
}
