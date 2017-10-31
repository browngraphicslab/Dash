using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

namespace Dash
{
    // TODO this entire class is a hack for the mit demo and should be completely rethought regardless of how cool it might seem
    public class PreviewDocument : CourtesyDocument
    {

        public static DocumentType PreviewDocumentType = new DocumentType("26367A6B-2DDE-4ADF-8CD7-30A8AE354FB5", "Preview Doc");

        public readonly string PrototypeId = "D6FF4388-DB02-41F0-AD52-C895A5C07265";


        public PreviewDocument(DocumentReferenceFieldController refToLayout, Point pos)
        {
            Document = GetLayoutPrototype().MakeDelegate();
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.PositionFieldKey] = new PointFieldModelController(pos),
                [KeyStore.ScaleAmountFieldKey] = new PointFieldModelController(1, 1),
                [KeyStore.ScaleCenterFieldKey] = new PointFieldModelController(0, 0),
                [KeyStore.DataKey] = refToLayout
            };
            Document.SetFields(fields, true);
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, 
            bool isInterfaceBuilderLayout = false)
        {
            var layout = docController.GetDereferencedField<DocumentFieldModelController>(KeyStore.DataKey, context).Data;
            var innerContent = layout.MakeViewUI(context, false);
            Debug.WriteLine("The preview document inner content's render transform is being changed" +
                            "other than that this view needs a couple more bindings but it should be easy to finish");
            var returnContent = new ContentPresenter()
            {
                Content = innerContent       
            };


            docController.AddFieldUpdatedListener(KeyStore.DataKey, (sender, args) =>
            {
                returnContent.Content = args.NewValue.DereferenceToRoot<DocumentFieldModelController>(args.Context).Data.MakeViewUI(args.Context, false);
            });

            BindPosition(returnContent, docController, context);

            return returnContent;
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<DocumentModel>.GetController<DocumentController>(PrototypeId) ??
                            InstantiatePrototypeLayout();
            return prototype;
        }


        protected override DocumentController InstantiatePrototypeLayout()
        {
            var prototypeDocument = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), PreviewDocumentType, PrototypeId);
            return prototypeDocument;
        }
    }
}
