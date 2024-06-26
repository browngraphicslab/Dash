﻿using Windows.Foundation;
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
        public DataBox(DocumentController docReference, KeyController refKey, Point position, double w = double.NaN, double h = double.NaN):this(
            new DocumentReferenceController(docReference, refKey), position.X, position.Y, w, h)
        {
        }

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

        public static FrameworkElement MakeView(DocumentController documentController)
        {
            //add field binding for content of content presenter
            var contentPresenter = new ContentPresenter();
            BindContent(contentPresenter, documentController, KeyStore.DataKey);
            return contentPresenter;
        }

		public static void BindContent(ContentPresenter presenter, DocumentController docController, KeyController key)
		{
			var converter = new DataFieldToMakeViewConverter(docController);

			var contentBinding = new FieldBinding<FieldControllerBase, TextController>()
			{
				Key = key,
				Document = docController,
				Converter = converter,
				Mode = BindingMode.OneWay,
                ValueType = BindingValueType.Field,
                Tag = "BindContent is DataBox"
			};
			presenter.AddFieldBinding(ContentPresenter.ContentProperty, contentBinding);
		}

	}
}
