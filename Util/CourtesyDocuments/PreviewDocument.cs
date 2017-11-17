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
using DashShared.Models;

namespace Dash
{
    // TODO this entire class is a hack for the mit demo and should be completely rethought regardless of how cool it might seem
    public class PreviewDocument : CourtesyDocument
    {

        public static DocumentType PreviewDocumentType = new DocumentType("26367A6B-2DDE-4ADF-8CD7-30A8AE354FB5", "Preview Doc");

        public readonly string PrototypeId = "D6FF4388-DB02-41F0-AD52-C895A5C07265";


        public PreviewDocument(DocumentReferenceController refToLayout, Point pos)
        {
            Document = GetLayoutPrototype().MakeDelegate();
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.PositionFieldKey] = new PointController(pos),
                [KeyStore.ScaleAmountFieldKey] = new PointController(1, 1),
                [KeyStore.ScaleCenterFieldKey] = new PointController(0, 0),
                [KeyStore.WidthFieldKey] = new NumberController(400),
                [KeyStore.HeightFieldKey] = new NumberController(400),
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
            var layout = docController.GetDereferencedField<DocumentController>(KeyStore.DataKey, context);
            FrameworkElement innerContent = null;
            if (layout != null)
            {
                foreach (var field in layout.GetDataDocument(null).EnumFields().Where((F) => !F.Key.IsUnrenderedKey()))
                    docController.SetField(field.Key, field.Value, true);
                innerContent = layout.MakeViewUI(context, false);
            }
            

            var returnContent = new ContentPresenter()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = innerContent       
            };

            docController.AddFieldUpdatedListener(KeyStore.DataKey, (sender, args, c) =>
            {
                var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
                var innerLayout = dargs.NewValue.DereferenceToRoot<DocumentController>(c);
                foreach (var field in innerLayout.GetDataDocument(null).EnumFields().Where((F) => !F.Key.IsUnrenderedKey()))
                    docController.SetField(field.Key, field.Value, true);
                var innerCont = innerLayout.MakeViewUI(c, false);
                returnContent.Content = innerCont;
            });

            return returnContent;
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId) ??
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
