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
        public RichTextFieldModelController(): base(new RichTextFieldModel(), false) { }
        public RichTextFieldModelController(RichTextFieldModel.RTD data):base(new RichTextFieldModel(data), false) { }

        private RichTextFieldModelController(RichTextFieldModel richTextFieldModel) : base(richTextFieldModel, true)
        {

        }

        public static RichTextFieldModelController CreateFromServer(RichTextFieldModel richTextFieldModel)
        {
            return ContentController.GetController<RichTextFieldModelController>(richTextFieldModel.Id) ??
                    new RichTextFieldModelController(richTextFieldModel);
        }

        /// <summary>
        /// The <see cref="RichTextFieldModel"/> associated with this <see cref="RichTextFieldModelController"/>
        /// </summary>
        public RichTextFieldModel RichTextFieldModel => FieldModel as RichTextFieldModel;

        public RichTextFieldModel.RTD RichTextData
        {
            get { return RichTextFieldModel.Data; }
            set
            {
                if (SetProperty(ref RichTextFieldModel.Data, value))
                {
                    OnFieldModelUpdated(null);
                    // Update the server
                    RESTClient.Instance.Fields.UpdateField(FieldModel, dto =>
                    {

                    }, exception =>
                    {

                    });
                }

            }
        }

        public ITextSelection SelectedText { get; set; }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        public override IEnumerable<DocumentController> GetReferences()
        {
            var links = RichTextData.ReadableString.Split(new string[] { "HYPERLINK" }, StringSplitOptions.RemoveEmptyEntries);
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
            return RichTextData.ReadableString;
        }

        public override FieldModelController Copy()
        {
            return new RichTextFieldModelController(RichTextData);
        }
    }
}
