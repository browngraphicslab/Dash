using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class InkDoc : CourtesyDocument
    {
        public static DocumentType InkDocType = new DocumentType("39358710-EA2C-4943-8476-B0AD91FD1379", "Ink");
        public static KeyController InkFieldKey = new KeyController("B71E2449-9A55-45F2-AF5F-C5B302328FEA", "_InkField");
        static DocumentController _prototypeTwoImages = CreatePrototypeInk();
        static DocumentController _prototypeLayout = CreatePrototypeLayout();

        static DocumentController CreatePrototypeInk()
        {
            // bcz: default values for data fields can be added, but should not be needed
            Dictionary<KeyController, FieldModelController> fields = new Dictionary<KeyController, FieldModelController>();
            fields.Add(InkFieldKey, new InkFieldModelController());
            return new DocumentController(fields, InkDocType);

        }
        
        static DocumentController CreatePrototypeLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            
            var prototypeInkLayout = new InkBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), InkFieldKey));
            prototypeInkLayout.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            prototypeInkLayout.Document.SetVerticalAlignment(VerticalAlignment.Stretch);
            prototypeInkLayout.Document.SetHeight(double.NaN);
            prototypeInkLayout.Document.SetWidth(double.NaN);
            var prototypeLayout = new StackLayout(new[] {prototypeInkLayout.Document });
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(200), true);
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);

            return prototypeLayout.Document;
        }

        public InkDoc()
        {
            Document = _prototypeTwoImages.MakeDelegate();

            var docLayout = _prototypeLayout.MakeDelegate();
            docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
            docLayout.SetField(new KeyController("opacity", "opacity"), new NumberFieldModelController(0.8), true);
            SetLayoutForDocument(Document, docLayout, forceMask: true, addToLayoutList: true);

        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }
    }
}
