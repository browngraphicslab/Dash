using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Dash.Converters;

namespace Dash
{
    public sealed class DataBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("9150B3F5-5E3C-4135-83E7-83845D73BB34", "Data Box");
        public static readonly string PrototypeId = "C1C83475-ADEB-4919-9465-46189F50AD9F";


        public static TypeInfo Type { get; private set; }
        public DataBox(FieldControllerBase refToData, double x = 0, double y = 0, double w = double.NaN, double h = double.NaN)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToData);
            SetupDocument(DocumentType, PrototypeId, "Data Box Prototype Layout", fields);
            var doc = (refToData as ReferenceController)?.GetDocumentController(null);
            if (doc != null)
            {
                Document.SetField(KeyStore.DocumentContextKey, doc, true);
            }

            Document.SetField(KeyStore.TitleKey, new PointerReferenceController(new DocumentReferenceController(Document, KeyStore.DocumentContextKey),
                KeyStore.TitleKey), true);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
	        //add field binding for content of content presenter
	        ContentPresenter contentPresenter = new ContentPresenter();
			BindContent(contentPresenter, documentController, context);

	        return contentPresenter;

        }

		public static void BindContent(ContentPresenter presenter, DocumentController docController, Context context)
		{
			var converter = new DataFieldToMakeViewConverter(docController, context);

			var contentBinding = new FieldBinding<FieldControllerBase>()
			{
				Key = KeyStore.DataKey,
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
