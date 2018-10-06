using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using DashShared;

namespace Dash
{
    class TableBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("45D372AB-C25A-4EC4-87DF-D63257CFFFE0", "Table Box");
        public static readonly string PrototypeId = "FB270B9A-FFFA-41B8-8A60-94D86EC87B2Fs";

        public TableBox(FieldControllerBase refToData, double x = 0, double y = 0, double w = double.NaN,
            double h = double.NaN)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToData);
            SetupDocument(DocumentType, PrototypeId, "Table Box Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController, KeyController key, Context context)
        {
            //add field binding for content of content presenter
            ContentPresenter contentPresenter = new ContentPresenter();
            BindContent(contentPresenter, documentController, key, context);

            return contentPresenter;

        }

        public static void BindContent(ContentPresenter presenter, DocumentController docController, KeyController key, Context context)
        {
            var converter = new TableFieldToMakeViewConverter(docController, key, context);

            var contentBinding = new FieldBinding<FieldControllerBase>()
            {
                Key = key,
                Document = docController,
                Converter = converter,
                Mode = BindingMode.TwoWay,
                Context = context,
                ValueType = BindingValueType.Field
            };
            presenter.AddFieldBinding(ContentPresenter.ContentProperty, contentBinding);
        }
    }
}
