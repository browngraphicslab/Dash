using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using static Dash.NoteDocuments;

namespace Dash
{
    public static class Actions
    {
        public static void OnOperatorAdd(ICollectionView collection, DragEventArgs e)
        {
            MainPage.Instance.AddOperatorsFilter(collection, e);
        }
        public static void AddDocument(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));


            //var newDocProto = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
            //newDocProto.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Dynamic Doc API"), true);
            //var newDoc = newDocProto.MakeDelegate();
            //newDoc.SetActiveLayout(new FreeFormDocument(new List<DocumentController>(), where, new Size(400, 400)).Document, true, true);
            //newDoc.SetActiveLayout(new FreeFormDocument(new List<DocumentController>(), where, new Size(400, 400)).Document, true, true);

            //collection.ViewModel.AddDocument(newDoc, null);
            collection.ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundBox.AdornmentShape.Elliptical, where), null);

            //DBTest.DBDoc.AddChild(newDoc);
        }
        public static void AddCollection(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformView,
                e.GetPosition(MainPage.Instance));

            var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Freeform);
            var newDoc = cnote.Document;
            
            collection.ViewModel.AddDocument(newDoc, null);
            //DBTest.DBDoc.AddChild(newDoc);
        }

        public static void DisplayDocument(CollectionViewModel collectionViewModel, DocumentController docController, Point? where = null)
        {
            if (where != null)
            {
                var pos = (Point)where;
                docController.GetPositionField().Data = pos;

                // TODO this is arbitrary should not be getting set here
                //var h = docController.GetHeightField().Data;
                //var w = docController.GetWidthField().Data;
                //docController.GetPositionField().Data = double.IsNaN(h) || double.IsNaN(w) ? pos : new Point(pos.X - w / 2, pos.Y - h / 2);
            }
            collectionViewModel.AddDocument(docController, null);
        }

        #region Ink Commands

        public static void SetTouchInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Touch;
        }

        public static void SetPenInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Pen;
        }

        public static void SetMouseInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.Mouse;
        }

        public static void SetNoInput(object obj)
        {
            GlobalInkSettings.InkInputType = CoreInputDeviceTypes.None;
        }

        public static void ToggleSelectionMode(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Selection;
        }

        public static void ChoosePen(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pen;
        }

        public static void ChoosePencil(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Pencil;
        }

        public static void ChooseEraser(object o)
        {
            GlobalInkSettings.StrokeType = GlobalInkSettings.StrokeTypes.Eraser;
        }
        
        #endregion

        public static void ChangeTheme(ICollectionView collectionView, DragEventArgs e)
        {
            MainPage.Instance.ThemeChange();
        }
    }
}
