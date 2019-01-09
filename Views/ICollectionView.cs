using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Dash.CollectionView;

namespace Dash
{
    public enum CollectionViewType { Freeform, Grid, Page, DB, Stacking, Schema, TreeView, Timeline, Graph, Icon };
    public interface ICollectionView
    {
        CollectionViewModel  ViewModel   { get; }
        UserControl          UserControl { get; }
        CollectionViewType   ViewType    { get; }

        void                 OnDocumentSelected(bool selected);
        void                 SetupContextMenu(MenuFlyout contextMenu);
        void                 SetDropIndicationFill(Brush fill);
    }
}
