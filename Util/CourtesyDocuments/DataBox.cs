using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using DashShared;
using System;
using Windows.UI.Xaml.Data;
using Dash.Converters;

namespace Dash
{
    public class DataBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("9150B3F5-5E3C-4135-83E7-83845D73BB34", "Data Box");
        public static readonly string PrototypeId = "C1C83475-ADEB-4919-9465-46189F50AD9F";


        public static TypeInfo Type { get; private set; }
        public DataBox(FieldControllerBase refToData, double x = 0, double y = 0, double w = double.NaN, double h = double.NaN)

        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToData);
            SetupDocument(DocumentType, PrototypeId, "Data Box Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
            var data = documentController.GetDereferencedField<FieldControllerBase>(KeyStore.DataKey, context);
			
	        //add field binding for content of content presenter
	        ContentPresenter contentPresenter = new ContentPresenter();
			BindContent(contentPresenter, documentController, context);

	        return contentPresenter;

        }

		protected static void BindContent(ContentPresenter presenter, DocumentController docController, Context context)
		{
			var contentBinding = new FieldBinding<TextController>()
			{
				Key = KeyStore.DataKey,
				Document = docController,
				Converter = new DataFieldToMakeViewConverter(docController, context),
				Mode = BindingMode.TwoWay,
				Context = context,
				FallbackValue = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Transparent)
			};
			presenter.AddFieldBinding(ContentPresenter.ContentProperty, contentBinding);
		}

	}
}
