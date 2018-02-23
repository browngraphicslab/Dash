using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Dash.Views;
using DashShared;
using DashShared.Models;

namespace Dash
{
    class InkBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ACDF5539-656B-44B5-AC0A-BA6E1875A4C2", "Ink Box");

        private static string PrototypeId = "29BD18A0-8236-4305-B063-B77BA4192C59";

        public InkBox(FieldControllerBase refToInk, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToInk);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            //Document.SetField(InkDataKey, new InkFieldModelController(), true);
            SetLayoutForDocument(Document, Document, true, true);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, DocumentController dataDocument)
        {

            var fmController = docController.GetDereferencedField(KeyStore.DataKey, context) as InkController;
            if (fmController != null)
            {
                var inkCanvas = new InkCanvasControl(fmController);
                SetupBindings(inkCanvas, docController, context);

                return inkCanvas;
            }
            return new Grid();
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var inkFieldModelController = new InkController();
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), inkFieldModelController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }

        private static ReferenceController GetInkReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }
    }
}
