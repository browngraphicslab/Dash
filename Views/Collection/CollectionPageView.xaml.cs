using Dash.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionPageView : SelectionElement, ICollectionView
    {
        public BaseCollectionViewModel ViewModel { get; private set; }
        //private ScrollViewer _scrollViewer;

        public CollectionPageView()
        {
            this.InitializeComponent();
            DataContextChanged += CollectionPageView_DataContextChanged;
            xThumbs.Loaded += (sender, e) =>
            {
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Height = xThumbs.ActualHeight;
            };
            xThumbs.SizeChanged += (sender, e) =>
            {
                FitPageButton_Click(null, null);
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Height = xThumbs.ActualHeight;
            };
        }

        public ObservableCollection<DocumentViewModel> PageDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();


        private void CollectionPageView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.ThumbDocumentViewModels.Clear();
            foreach (var pageDoc in ViewModel.DocumentViewModels.Reverse().Select((vm) => vm.DocumentController))
            {
                var pageViewDoc = pageDoc.GetViewCopy();
                pageViewDoc.SetLayoutDimensions(double.NaN, double.NaN);

                CurPage = new DocumentViewModel(pageViewDoc) { Undecorated = true };
                PageDocumentViewModels.Insert(0, CurPage);

                var thumbnailImageViewDoc = ((pageDoc.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentFieldModelController)?.Data ?? pageDoc).GetViewCopy();
                thumbnailImageViewDoc.SetLayoutDimensions(double.NaN, xThumbs.ActualHeight);
                ViewModel.ThumbDocumentViewModels.Insert(0, new DocumentViewModel(thumbnailImageViewDoc) { Undecorated = true });
            }
        }

        public DocumentViewModel CurPage
        {
            get { return this.xDocView.DataContext as DocumentViewModel; }
            set
            {
                xDocView.DataContext = value;

                // replace old layout of page name/id with a new one because
                // fieldbinding's can't be removed yet
                xPageNumContainer.Children.Remove(xPageNum);
                xPageNum = new TextBlock();

                var binding = new FieldBinding<DocumentFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = value.DocumentController,
                    Key = value.DocumentController.GetField(KeyStore.DocumentContextKey, true) == null ? KeyStore.ThisKey : KeyStore.DocumentContextKey,
                    Converter = new DocumentControllerToStringConverter()
                };

                if (value.Content is CollectionView)
                {
                    value.Content.Loaded -= Content_Loaded;
                    value.Content.Loaded += Content_Loaded;
                }

                xPageNumContainer.Children.Add(xPageNum);
                xPageNum.AddFieldBinding(TextBlock.TextProperty, binding);
            }
        }

        private void Content_Loaded(object sender, RoutedEventArgs e)
        {
            var cv = sender as CollectionView;
            if (cv != null)
            {
                var _element = cv.CurrentView as CollectionFreeformView;
                if (_element is CollectionFreeformView)
                {
                    _element.Loaded -= _element_Loaded;
                    _element.Loaded += _element_Loaded;
                }
                cv.Loaded -= Content_Loaded;
            }
        }

        private void _element_Loaded(object sender, RoutedEventArgs e)
        {
            var _element = sender as CollectionFreeformView;
            if (_element is CollectionFreeformView)
            {
                _element.ManipulationControls.FitToParent();
                _element.Loaded -= _element_Loaded;
            }
        }

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
        }

        #endregion

        #region DragAndDrop


        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }
        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
            ViewModel.UpdateDocumentsOnSelection(isSelected);
            if (isSelected)
                xSplitter.Opacity = 1;
            else xSplitter.Opacity = 0;
        }


        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }
        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;
            OnSelected();
        }

        #endregion

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = PageDocumentViewModels.IndexOf(CurPage);
                CurPage = PageDocumentViewModels[Math.Max(0, ind - 1)];
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = PageDocumentViewModels.IndexOf(CurPage);
                CurPage = PageDocumentViewModels[Math.Min(PageDocumentViewModels.Count - 1, ind + 1)];
            }
        }

        private void FitPageButton_Click(object sender, RoutedEventArgs e)
        {
            var _element = ((CurPage?.Content as CollectionView)?.CurrentView as CollectionFreeformView);
            _element?.ManipulationControls.FitToParent();
        }

        private void xThumbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ind = xThumbs.SelectedIndex;
            CurPage = PageDocumentViewModels[Math.Max(0, Math.Min(PageDocumentViewModels.Count - 1, ind))];
        }

        private void xPageNum_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
            e.Complete();
        }

        private void xPageNumContainer_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            e.Data.RequestedOperation = DataPackageOperation.Link;
            e.Data.Properties.Add("View", true);
            e.Data.Properties.Add("Width", xDocView.ActualWidth);
            e.Data.Properties.Add("Height", xDocView.ActualHeight);
            CurPage.DocumentView_DragStarting(sender, e, ViewModel);
        }
    }
}
