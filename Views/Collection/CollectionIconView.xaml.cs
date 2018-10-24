﻿using System;
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
                doc.SetField<TextController>(KeyStore.CollectionViewTypeKey, ViewModel.ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionOpenViewTypeKey,null)?.Data ?? CollectionView.CollectionViewType.Freeform.ToString(), true);
                doc.SetWidth(double.NaN);
                doc.SetHeight(double.NaN);
                doc.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                doc.SetVerticalAlignment(VerticalAlignment.Stretch);
                SplitFrame.OpenInActiveFrame(doc);
            }
        }


         // This replaces the icon folder view document and makes the containing dataBox not hit test visible.
        public void DropDoc(DocumentController dragDoc)
        {
            var containerDoc = ViewModel.ContainerDocument.GetDataDocument();
            containerDoc.SetField(KeyStore.FolderPreviewKey, dragDoc, true);
        }

        private void xFolderPreview_Drop(object sender, DragEventArgs e)
        {
            var dragModel = e.DataView.GetDragModel();
            if (dragModel is DragDocumentModel dm)
            {

                DropDoc(dm.DraggedDocuments.First());
            }
            if (dragModel is DragFieldModel dfm)
            {

                DropDoc(dfm.GetDropDocuments(new Point(),null).First());
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
                    containerDoc.SetField(KeyStore.FolderPreviewDataBoxKey, new DataBox(new DocumentReferenceController(containerDoc, KeyStore.FolderPreviewKey)).Document, true);
                }
                var db = containerDoc.GetDereferencedField<DocumentController>(KeyStore.FolderPreviewDataBoxKey, null);
                db.SetAreContentsHitTestVisible(false);

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
