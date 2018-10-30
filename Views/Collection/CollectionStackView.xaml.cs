using System;
using System.Linq;
using Dash.FontIcons;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Dash.CollectionView;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection 
{
    public sealed partial class CollectionStackView : UserControl, ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        public CollectionStackView()
        {
            this.InitializeComponent();
        }        

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }
        public void OnDocumentSelected(bool selected)
        {
        }

        public void SetupContextMenu(MenuFlyout contextMenu)
        {
            contextMenu.Items.Add(new MenuFlyoutSubItem()
            {
                Text = "View Children As",
                Icon = new FontIcons.FontAwesome { Icon = FontAwesomeIcon.Eye }
            });
            foreach (var n in Enum.GetValues(typeof(CollectionViewType)).Cast<CollectionViewType>())
            {
                (contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Add(new MenuFlyoutItem() { Text = n.ToString() });
                ((contextMenu.Items.Last() as MenuFlyoutSubItem).Items.Last() as MenuFlyoutItem).Click += (ss, ee) => {
                    foreach (var dvm in ViewModel.DocumentViewModels)
                    {
                        dvm.LayoutDocument.SetField<TextController>(KeyStore.CollectionViewTypeKey, n.ToString(), true);
                    }
                };
            }
        }
    }
}
