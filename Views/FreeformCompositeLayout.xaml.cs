using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Converters;
using Dash.ViewModels;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class FreeformCompositeLayout : UserControl
    {
        public DocumentCollectionFieldModelController DocumentCollection;
        public DocumentController ParentDocument;
        public bool CanEdit;

        public FreeformCompositeLayout(SelectableContainer container, DocumentController parentDocument)
        {
            this.InitializeComponent();
            ParentDocument = parentDocument;
            container.SetContent(this);
            DataContextChanged += OnDataContextChanged;
            DragOver += OnDragOver;
            Drop += OnDrop;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var key = e.Data.Properties[KeyValuePane.DragPropertyKey] as Key;
            if (key != null)
            {
                DocumentCollection.AddDocument(ParentDocument.GetField(key));
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = CanEdit ? DataPackageOperation.Move : DataPackageOperation.None;
        }


        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            DocumentCollection = DataContext as DocumentCollectionFieldModelController;
            Binding childrenBinding = new Binding()
            {
                Source = DocumentCollection,
                Path = new PropertyPath(nameof(DocumentCollection.GetDocuments)),
                Mode = BindingMode.OneWay,
                Converter = new DocumentToUIElementConverter()
            };
            xItemsControl.SetBinding(ItemsControl.ItemsSourceProperty, childrenBinding);
            DocumentCollection.OnDocumentsChanged += ViewModelOnOnDocumentsChanged;
        }

        private void ViewModelOnOnDocumentsChanged(IEnumerable<DocumentController> currentDocuments)
        {
            //throw new NotImplementedException();
        }

        private void XGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            XClipRect.Rect = new Rect(0,0, XGrid.Width, XGrid.Height);
        }

    }
}
