using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Dash
{
    public static class Actions
    {
        public static void AddDocument(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformBase,
                e.GetPosition(MainPage.Instance));
            collection.ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Elliptical, where));
        }
        public static void AddCollection(ICollectionView collection, DragEventArgs e)
        {
            var where = Util.GetCollectionFreeFormPoint(collection as CollectionFreeformBase,
                e.GetPosition(MainPage.Instance));
            
            collection.ViewModel.AddDocument(new CollectionNote(where, CollectionView.CollectionViewType.Freeform).Document);
        }
        public static void HideDocument(CollectionViewModel collectionViewModel, DocumentController docController)
        {
            docController.SetHidden(true);
        }

        public static bool UnHideDocument(CollectionViewModel collectionViewModel, DocumentController docController)
        {
            foreach (var vm in collectionViewModel.ContainerDocument.GetDereferencedField<ListController<DocumentController>>(collectionViewModel.CollectionKey,null).TypedData)
                if (vm.GetDataDocument().Equals(docController.GetDataDocument()) && vm.GetHidden())
                {
                    vm.SetHidden(false);
                    return true;
                }
            return false;
        }

        public static void DisplayDocument(CollectionViewModel collectionViewModel, DocumentController docController, Point? where = null)
        {
            if (where != null)
            {
                var pos = (Point)where;
                docController.GetPositionField().Data = pos;
            }
            collectionViewModel.AddDocument(docController);
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

    }
}
