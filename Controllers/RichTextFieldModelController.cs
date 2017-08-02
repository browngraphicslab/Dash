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

namespace Dash
{
    public class RichTextFieldModelController: FieldModelController
    {
        public RichTextFieldModelController(): base(new RichTextFieldModel()) { }
        public RichTextFieldModelController(string data):base(new RichTextFieldModel(data)) { }
        /// <summary>
        /// The <see cref="RichTextFieldModel"/> associated with this <see cref="RichTextFieldModelController"/>
        /// </summary>
        public RichTextFieldModel RichTextFieldModel => FieldModel as RichTextFieldModel;

        public string RichTextData
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

        public ITextSelection SelectedText { get; set; }
        protected override void UpdateValue(FieldModelController fieldModel)
        {
            var richTextFieldModelController = fieldModel as RichTextFieldModelController;
            if (richTextFieldModelController != null) RichTextData = richTextFieldModelController.RichTextData;
        }

        public override TypeInfo TypeInfo => TypeInfo.Text;

        public override FrameworkElement GetTableCellView()
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
            return new RichTextFieldModelController("Default Value");
        }

        public override string ToString()
        {
            return RichTextData;
        }

        public override FieldModelController Copy()
        {
            return new RichTextFieldModelController(RichTextData);
        }
    }
}
