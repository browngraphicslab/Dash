using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection
{
    public sealed partial class CollectionIconView : UserControl, ICollectionView
    {
        public CollectionIconView()
        {
            this.InitializeComponent();
            Loaded += CollectionIconView_Loaded;
            DoubleTapped += CollectionIconView_DoubleTapped;
        }

        private void CollectionIconView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var doc = ViewModel.ContainerDocument.GetViewCopy();
                doc.SetField<TextController>(KeyStore.CollectionViewTypeKey, ViewModel.ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionOpenViewTypeKey,null).Data, true);
                doc.SetWidth(double.NaN);
                doc.SetHeight(double.NaN);
                doc.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                doc.SetVerticalAlignment(VerticalAlignment.Stretch);
                SplitFrame.OpenInActiveFrame(doc);
            }
        }

        private void CollectionIconView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                if (ViewModel.ContainerDocument.GetDataDocument().GetDereferencedField(KeyStore.FolderPreviewKey,null) == null)
                {
                    ViewModel.ContainerDocument.GetDataDocument().SetField<TextController>(KeyStore.FolderPreviewKey, "<description>", true);
                }
                var db = new DataBox(new DocumentReferenceController(ViewModel.ContainerDocument.GetDataDocument(), KeyStore.FolderPreviewKey)).Document;
                //TODO Maybe just store the data box on the document?
                FieldControllerBase.MakeRoot(db);
                xFolderPreview.Content = new DocumentView() { ViewModel = new DocumentViewModel(db) { IsDimensionless = true } };
            }
        }

        public UserControl UserControl => this;
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }

        public void SetDropIndicationFill(Brush fill)
        {
            xFolderIcon.Foreground = (fill as SolidColorBrush)?.Color != Colors.Transparent ? fill : new SolidColorBrush(Colors.DarkGoldenrod);
         }

        public void SetupContextMenu(MenuFlyout contextMenu)
        {
        }
    }
}
