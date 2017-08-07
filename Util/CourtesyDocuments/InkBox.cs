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

namespace Dash
{
    class InkBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ACDF5539-656B-44B5-AC0A-BA6E1875A4C2", "Ink Box");

        public static KeyController InkDataKey = new KeyController("1F6A3D2F-28D8-4365-ADA8-4C345C3AF8B6", "Ink Data Key");

        private static string PrototypeId = "29BD18A0-8236-4305-B063-B77BA4192C59";

        public InkBox(FieldModelController refToInk, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToInk);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            //Document.SetField(InkDataKey, new InkFieldModelController(), true);
            SetLayoutForDocument(Document, Document, true, true);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, DocumentController dataDocument, bool isInterfaceBuilderLayout = false)
        {

            var fmController = docController.GetDereferencedField(KeyStore.DataKey, context) as InkFieldModelController;
            if (fmController != null)
            {
                var inkCanvas = new InkCanvasControl(fmController);
                SetupBindings(inkCanvas, docController, context);
                
                if (isInterfaceBuilderLayout)
                {
                    var selectableContainer = new SelectableContainer(inkCanvas, docController, dataDocument);
                    //SetupBindings(selectableContainer, docController, context);
                    return selectableContainer;
                }
                return inkCanvas;
            }
            return new Grid();
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var inkFieldModelController = new InkFieldModelController();
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), inkFieldModelController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);
            return prototypeDocument;
        }

        private static ReferenceFieldModelController GetInkReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
        }
    }
}
