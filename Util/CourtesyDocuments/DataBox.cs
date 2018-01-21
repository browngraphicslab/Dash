using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class DataBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("9150B3F5-5E3C-4135-83E7-83845D73BB34", "Data Box");
        public static readonly string PrototypeId = "C1C83475-ADEB-4919-9465-46189F50AD9F";

        public DataBox(FieldControllerBase refToData, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToData);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            var fieldControllerBase = (refToData as ReferenceController)?.GetDocumentController(null);
            if (fieldControllerBase != null)
            {
                Document.SetField(KeyStore.DocumentContextKey, fieldControllerBase, true);
            }
        }

        public override FrameworkElement makeView(DocumentController documentController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            return MakeView(documentController, context, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            var data = documentController.GetDereferencedField<FieldControllerBase>(KeyStore.DataKey, context);
            if (data is TextController)
            {
                return TextingBox.MakeView(documentController, context);
            }
            else if (data is ImageController)
            {
                return ImageBox.MakeView(documentController, context);
            }
            else if (data is ListController<DocumentController>)
            {
                return CollectionBox.MakeView(documentController, context, documentController.GetDataDocument(null));
            }
            return new Grid();
        }

        protected override DocumentController GetLayoutPrototype()
        {
            return ContentController<FieldModel>.GetController<DocumentController>(PrototypeId) ??
                   InstantiatePrototypeLayout();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var defaultText = new TextController("Default Data");
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), defaultText);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }
    }
}
