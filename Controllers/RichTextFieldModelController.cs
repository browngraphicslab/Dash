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
    public class RichTextFieldModelController: FieldModelController<RichTextFieldModel>
    {
        public RichTextFieldModelController(): base(new RichTextFieldModel()) { }
        public RichTextFieldModelController(RichTextFieldModel.RTD data):base(new RichTextFieldModel(data)) { }

        public RichTextFieldModelController(RichTextFieldModel richTextFieldModel) : base(richTextFieldModel)
        {

        }

        public override void Init()
        {
            
        }

        /// <summary>
        /// The <see cref="RichTextFieldModel"/> associated with this <see cref="RichTextFieldModelController"/>
        /// </summary>
        public RichTextFieldModel RichTextFieldModel => Model as RichTextFieldModel;

        public RichTextFieldModel.RTD Data
        {
            get { return RichTextFieldModel.Data; }
            set
            {
                if (RichTextFieldModel.Data != value)
                {
                    RichTextFieldModel.Data = value;
                    OnFieldModelUpdated(null);
                    // Update the server
                    UpdateOnServer();
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

        public override TypeInfo TypeInfo => TypeInfo.RichTextField;

        public override IEnumerable<DocumentController> GetReferences()
        {
            var links = Data.ReadableString.Split(new string[] { "HYPERLINK" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var link in links)
            {
                var split = link.Split('\"');
                if (split.Count() > 1)
                {
                    var doc = ContentController<DocumentModel>.GetController<DocumentController>(split[1]);
                    if (doc != null)
                        yield return doc;
                }
            }
        }

        public override FrameworkElement GetTableCellView(Context context)
        {
            var richTextView = new RichTextView()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TargetRTFController = this
            };

            return richTextView;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new RichTextFieldModelController(new RichTextFieldModel.RTD("Default Value"));
        }

        public override string ToString()
        {
            return Data.ReadableString;
        }

        public override FieldModelController<RichTextFieldModel> Copy()
        {
            return new RichTextFieldModelController(Data);
        }
    }
}
