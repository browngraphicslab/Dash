using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using DashShared;
using System;

namespace Dash
{
    public class DataBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("9150B3F5-5E3C-4135-83E7-83845D73BB34", "Data Box");
        public static readonly string PrototypeId = "C1C83475-ADEB-4919-9465-46189F50AD9F";
        public static TypeInfo Type { get; private set; }
        public DataBox(FieldControllerBase refToData, double x = 0, double y = 0, double w = 100, double h = double.NaN)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToData);
            SetupDocument(DocumentType, PrototypeId, "Data Box Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
            var data = documentController.GetDereferencedField<FieldControllerBase>(KeyStore.DataKey, context);

            if (data is TextController txt && txt.Data.StartsWith("=="))
            {
                try
                {
                    data = DSL.InterpretUserInput(txt.Data)?.DereferenceToRoot(null);
                }
                catch (Exception) { }
            }
            //if (data is ListController<DocumentController> documentList)
            //{
            //    data = new TextController(new ObjectToStringConverter().ConvertDataToXaml(documentList, null));
            //}

            if (data is ImageController img)
            {
                if (img.Data.LocalPath.EndsWith(".pdf"))
                    return PdfBox.MakeView(documentController, context);
               
                return ImageBox.MakeView(documentController, context);
            }
			if (data is VideoController)
			{
				return VideoBox.MakeView(documentController, context);
			}
            else if (data is AudioController)
			{
                return AudioBox.MakeView(documentController, context);
            }
            else if (data is ListController<DocumentController> docList)
			{
                return CollectionBox.MakeView(documentController, context);
            }
            else if (data is DocumentController dc)
			{
                // hack to check if the dc is a view document
                FrameworkElement view = null;
                if (KeyStore.TypeRenderer.ContainsKey(dc.DocumentType))
                {
                    view =  dc.MakeViewUI(context);
                }
                else
                {
                    view = dc.GetKeyValueAlias().MakeViewUI(context);
                }
                //bcz: this is odd -- the DocumentViewModel is bound to the DataBox, so we have to transfer the
                //   "container-like" bindings from the contained data view to the DataBox
                SetupBindings(view, documentController, context);
                return view;
            }
            else if (data is TextController || data is NumberController || data is DateTimeController)
			{
                return TextingBox.MakeView(documentController, context);
            }
            else if (data is RichTextController)
			{
                return RichTextBox.MakeView(documentController, context);
            }
            return new Grid();
        }
    }
}
