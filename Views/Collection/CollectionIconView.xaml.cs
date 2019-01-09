using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        public CollectionViewType ViewType => CollectionViewType.Icon;
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
                var doc = ViewModel.ContainerDocument; //.GetViewCopy();
                //if (doc.GetField(KeyStore.LastWorkspaceKey) == null)
                //{
                //    doc.SetField(KeyStore.LastWorkspaceKey, doc.GetViewCopy(),true);
                //}

                //var openDoc = doc.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                //openDoc.SetField<TextController>(KeyStore.CollectionViewTypeKey, ViewModel.ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionOpenViewTypeKey, null)?.Data ?? CollectionViewType.Freeform.ToString(), true);
                //openDoc.SetWidth(double.NaN);
                //openDoc.SetHeight(double.NaN);
                //openDoc.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                //openDoc.SetVerticalAlignment(VerticalAlignment.Stretch);
                SplitFrame.OpenInActiveFrame(doc);
            }
        }


         // This replaces the icon folder view document and makes the containing dataBox not hit test visible.
        public void DropDoc(DocumentController dragDoc, KeyController key)
        {
            var containerDoc = ViewModel.ContainerDocument.GetDataDocument();
            containerDoc.SetField(key, dragDoc, true);
        }

        private void xFolderPreview_Drop(object sender, DragEventArgs e) { drop(e, KeyStore.FolderPreviewKey); }

        private void xFolderIcon_Drop(object sender, DragEventArgs e)    { drop(e, KeyStore.FolderIconKey); }
        private async Task drop(DragEventArgs e, KeyController key)
        { 
            var dragModel = e.DataView.GetDragModel();
            if (dragModel is DragDocumentModel dm)
            {
                DropDoc(dm.DraggedDocuments.First(), key);
            }
            if (dragModel is DragFieldModel dfm)
            {
                DropDoc((await dfm.GetDropDocuments(new Point(),null)).First(), key);
            }
            e.Handled = true;
        }
        private void CollectionIconView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var containerDoc = ViewModel.ContainerDocument.GetDataDocument();
                if (containerDoc.GetDereferencedField(KeyStore.FolderPreviewKey,null) == null)
                {
                    containerDoc.SetField(KeyStore.FolderPreviewKey, new DocumentReferenceController(containerDoc, KeyStore.TitleKey), true);
                }
                if (containerDoc.GetDereferencedField<DocumentController>(KeyStore.FolderPreviewDataBoxKey, null) == null)
                {
                    containerDoc.SetField(KeyStore.FolderPreviewDataBoxKey, new DataBox(containerDoc, KeyStore.FolderPreviewKey, new Point()).Document, true);
                }
                if (containerDoc.GetDereferencedField<DocumentController>(KeyStore.FolderIconDataBoxKey, null) == null)
                {
                    containerDoc.SetField(KeyStore.FolderIconDataBoxKey, new DataBox(containerDoc, KeyStore.FolderIconKey, new Point()).Document, true);
                }
                var db = containerDoc.GetDereferencedField<DocumentController>(KeyStore.FolderPreviewDataBoxKey, null);
                db.SetAreContentsHitTestVisible(false);
                var dbf = containerDoc.GetDereferencedField<DocumentController>(KeyStore.FolderIconDataBoxKey, null);
                dbf.SetAreContentsHitTestVisible(false);

                xFolderPreview.Content = new DocumentView() { ViewModel = new DocumentViewModel(db) { IsDimensionless = true } };
                xFolderIcon.Content    = new DocumentView() { ViewModel = new DocumentViewModel(dbf) { IsDimensionless = true } };
            }
        }
        public void OnDocumentSelected(bool selected)
        {
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
