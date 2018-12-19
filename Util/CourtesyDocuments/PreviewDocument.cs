using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    // TODO this entire class is a hack for the mit demo and should be completely rethought regardless of how cool it might seem
    public class PreviewDocument : CourtesyDocument
    {

        public static DocumentType DocumentType = new DocumentType("26367A6B-2DDE-4ADF-8CD7-30A8AE354FB5", "Preview Doc");

        public readonly string PrototypeId = "D6FF4388-DB02-41F0-AD52-C895A5C07265";


        public PreviewDocument(DocumentReferenceController refToLayout, Point pos)
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.PositionFieldKey] = new PointController(pos),
                [KeyStore.WidthFieldKey] = new NumberController(400),
                [KeyStore.HeightFieldKey] = new NumberController(400),
                [KeyStore.DataKey] = refToLayout
            };
            SetupDocument(DocumentType, PrototypeId, "PreviewDocument Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var layout = docController.GetDereferencedField<DocumentController>(KeyStore.DataKey, context);
            FrameworkElement innerContent = null;
            if (layout != null)
            {
                foreach (var field in layout.GetDataDocument().EnumFields().Where((F) => !F.Key.IsUnrenderedKey() && !F.Key.Equals(KeyStore.DataKey)))
                    docController.SetField(field.Key, field.Value, true);
                innerContent = layout.MakeViewUI(context);
            }
            

            var returnContent = new ContentPresenter()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = innerContent       
            };

            docController.AddFieldUpdatedListener(KeyStore.DataKey, (sender, args) =>
            {
                //var fargs =
                //    ((DocumentController.DocumentFieldUpdatedEventArgs) args).FieldArgs as
                //    DocumentController.DocumentFieldUpdatedEventArgs;//Update came from reference //TODO Make this like DereferenceToRoot
                //if (fargs != null && fargs.Action == DocumentController.FieldUpdatedAction.Update)
                //{
                //    return;
                //}
                layout = layout ?? docController.GetDereferencedField<DocumentController>(KeyStore.DataKey, context);
                var innerLayout = args.NewValue.DereferenceToRoot<DocumentController>(null);
                foreach (var field in layout.GetDataDocument().EnumFields().Where((F) => !F.Key.IsUnrenderedKey() && !F.Key.Equals(KeyStore.DataKey)))
                    docController.SetField(field.Key, field.Value, true);
                var innerCont = innerLayout.MakeViewUI(null);
                returnContent.Content = innerCont;
            });

            return returnContent;
        }
    }
}
