using Dash.Converters;
using DashShared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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
                    t.Width = xThumbs.ActualWidth;
            };
            xThumbs.SizeChanged += (sender, e) =>
            {
                FitPageButton_Click(null, null);
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Width = xThumbs.ActualWidth;
            };

            this.AddHandler(KeyDownEvent, new KeyEventHandler(SelectionElement_KeyDown), true);
            this.GotFocus += CollectionPageView_GotFocus;
            this.LosingFocus += CollectionPageView_LosingFocus;
        }

        private void CollectionPageView_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.FocusState == FocusState.Pointer)
            {
                if (this.GetFirstDescendantOfType<ScrollViewer>() == args.OldFocusedElement)
                    args.Handled = args.Cancel = true;
                if (this.GetFirstDescendantOfType<Microsoft.Toolkit.Uwp.UI.Controls.GridSplitter>() == args.OldFocusedElement)
                {
                    var xx = this.GetFirstDescendantOfType<Microsoft.Toolkit.Uwp.UI.Controls.GridSplitter>();
                    args.Handled = args.Cancel = true;
                }
            }
            else if (args.FocusState == FocusState.Keyboard)
            {
                //if (this.GetDescendantsOfType<RichEditBox>().Contains(args.OldFocusedElement))
                    args.Handled = args.Cancel = true;
            }
        }

        private void CollectionPageView_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        public ObservableCollection<DocumentViewModel> PageDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();


        private void CollectionPageView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            args.Handled = true;
            if (ViewModel != DataContext)
            {
                ViewModel = DataContext as BaseCollectionViewModel;
                ViewModel.ThumbDocumentViewModels.Clear();
                foreach (var pageDoc in ViewModel.DocumentViewModels.Select((vm) => vm.DocumentController))
                {
                    var pageViewDoc = pageDoc.GetViewCopy();
                    pageViewDoc.SetLayoutDimensions(double.NaN, double.NaN);

                    PageDocumentViewModels.Add(new DocumentViewModel(pageViewDoc) { Undecorated = true });

                    var thumbnailImageViewDoc = (pageDoc.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentController ?? pageDoc).GetViewCopy();
                    thumbnailImageViewDoc.SetLayoutDimensions(xThumbs.ActualWidth, double.NaN);
                    ViewModel.ThumbDocumentViewModels.Add(new DocumentViewModel(thumbnailImageViewDoc) { Undecorated = true });

                    CurPage = PageDocumentViewModels.First();
                }
            }
        }

        KeyController CaptionKey = null;
        KeyController DisplayKey = null;
        string DisplayString = "";
        public void SetHackText(KeyController newKey, string keyasgn)
        {
            if (CaptionKey == null)
                SetHackText(newKey, null,  keyasgn);
            else SetHackText(CaptionKey, newKey, keyasgn);
        }
        public void SetHackText(KeyController captionKey, KeyController documentKey, string keyasgn)
        {
            if (captionKey != null)
            {
                CaptionKey = captionKey;
                xDocContainer.Children.Remove(xDocTitle);
                xDocTitle = new TextBox() { VerticalAlignment = VerticalAlignment.Bottom, Width = 200, Height = 0, Visibility = xDocTitle.Visibility };
                Grid.SetRow(xDocTitle, 1);
                xDocContainer.Children.Add(xDocTitle);
                if (captionKey != null)
                {
                    var captionBinding = new FieldBinding<FieldControllerBase>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = CurPage.DocumentController.GetDataDocument(null),
                        Key = CaptionKey,
                        Converter = new ObjectToStringConverter()
                    };
                    xDocTitle.AddFieldBinding(TextBox.TextProperty, captionBinding);
                    xDocTitle.Height = 30;
                    xDocCaptionRow.Height = new GridLength(30);
                }
                else
                {
                    xDocTitle.Height = 0;
                    xDocCaptionRow.Height = new GridLength(0);
                }
            }
            if (documentKey != null)
            {
                DisplayString = keyasgn;
                DisplayKey = documentKey;
                var data = CurPage.DocumentController.GetDataDocument(null).GetField(DisplayKey);
                if (data == null && !string.IsNullOrEmpty(DisplayString))
                {
                    var keysToReplace = new Regex("#[a-z0-9A-Z_]*").Matches(DisplayString);
                    var replacedString = DisplayString;
                    foreach (var keyToReplace in keysToReplace)
                    {
                        var k = KeyController.LookupKeyByName(keyToReplace.ToString().Substring(1));
                        if (k != null) { 
                            var value = CurPage.DocumentController.GetDataDocument(null).GetDereferencedField<TextController>(k, null)?.Data;
                            if (value != null)
                                replacedString = replacedString.Replace(keyToReplace.ToString(), value);
                        }
                    }
                    var img = MainPage.Instance.xMainSearchBox.SearchForFirstMatchingDocument(replacedString)?.GetViewCopy(new Point());
                    if (img != null)
                    {
                        img.GetWidthField().NumberFieldModel.Data = double.NaN;
                        img.GetHeightField().NumberFieldModel.Data = double.NaN;
                    }
                    data = img;
                }
                if (data != null)
                {
                    CurPage.DocumentController.GetDataDocument(null).SetField(DisplayKey, data, true);
                    var db = new DataBox(CurPage.DocumentController.GetDataDocument(null).GetField(DisplayKey));
                    xDocView.DataContext = new DocumentViewModel(db.Document);
                }
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


                var binding = new FieldBinding<TextController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = value.DocumentController.GetDataDocument(null),
                    Key = KeyStore.TitleKey
                };

                if (value.Content is CollectionView)
                {
                    value.Content.Loaded -= Content_Loaded;
                    value.Content.Loaded += Content_Loaded;
                }

                xPageNumContainer.Children.Add(xPageNum);
                xPageNum.AddFieldBinding(TextBlock.TextProperty, binding);

                SetHackText(CaptionKey, DisplayKey, DisplayString);

                var ind = PageDocumentViewModels.IndexOf(CurPage);
                if (ind >= 0 && ViewModel.ThumbDocumentViewModels.Count > ind)
                {
                    var thumb = ViewModel.ThumbDocumentViewModels[ind];
                    foreach (var t in ViewModel.ThumbDocumentViewModels)
                        t.SetSelected(null, false);
                    thumb.SetSelected(null, true);
                }
                var cview = (CurPage?.Content as CollectionView);
                if (cview != null)
                {
                    cview.ViewModel.ContainerDocument.FieldModelUpdated -= ContainerDocument_FieldModelUpdated;
                    cview.ViewModel.ContainerDocument.FieldModelUpdated += ContainerDocument_FieldModelUpdated;
                    cview.Loaded -= Cview_Loaded;
                    cview.Loaded += Cview_Loaded;
                }
            }
        }

        private void ContainerDocument_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var cview = (CurPage?.Content as CollectionView);
            (cview?.CurrentView as CollectionFreeformView)?.ManipulationControls?.FitToParent();
        }

        private void Cview_Loaded(object sender, RoutedEventArgs e)
        {
            var cview = sender as CollectionView;
            cview.ViewModel.ContainerDocument.FieldModelUpdated -= ContainerDocument_FieldModelUpdated;
            cview.ViewModel.ContainerDocument.FieldModelUpdated += ContainerDocument_FieldModelUpdated;
            (cview?.CurrentView as CollectionFreeformView)?.ManipulationControls?.FitToParent();
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
            Focus(FocusState.Keyboard);
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
            _element?.ManipulationControls?.FitToParent();
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

        private void SelectionElement_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.PageDown || e.Key == Windows.System.VirtualKey.Down)
            {
                NextButton_Click(sender, e);
                e.Handled = true;
            }
            if (e.Key == Windows.System.VirtualKey.PageUp || e.Key == Windows.System.VirtualKey.Up)
            {
                PrevButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
