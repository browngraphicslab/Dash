using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Dash.Converters;

namespace Dash
{
    /// <summary>
    /// The data field for a collection is a document collection field model controller
    /// </summary>
    public class CollectionBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.CollectionBoxType;
        private static string PrototypeId = "E1F828EA-D44D-4C3C-BE22-9AAF369C3F19";

        
        public CollectionBox(FieldControllerBase refToCollection, double x = 0, double y = 0, double w = double.NaN, double h = double.NaN, CollectionViewType viewType = CollectionViewType.Freeform)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToCollection);
            fields[KeyStore.CollectionViewTypeKey] = new TextController(viewType.ToString());
            fields[KeyStore.BackgroundColorKey] = new TextController(Colors.White.ToString());
            fields[KeyStore.HorizontalAlignmentKey] = new TextController(HorizontalAlignment.Left.ToString());
            fields[KeyStore.VerticalAlignmentKey] = new TextController(VerticalAlignment.Top.ToString());
            fields[KeyStore.IconTypeFieldKey] = new NumberController((int)IconTypeEnum.Collection); // TODO factor out into SetIconField() method in base class

            SetupDocument(DocumentType, PrototypeId, "Collection Box Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, KeyController key,  Context context)
        {
            return new CollectionView() { DataContext = new CollectionViewModel(docController, key) };
        }
        
    }
}
