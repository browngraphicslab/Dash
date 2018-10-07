using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public interface ICollectionView
    {
        CollectionViewModel ViewModel { get; }
        void SetDropIndicationFill(Brush fill);
        UserControl  UserControl { get; }

        void SetupContextMenu(MenuFlyout contextMenu);
    }
}
