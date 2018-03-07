﻿using Dash.Converters;
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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Dash.Models.DragModels;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionPageView : ICollectionView
    {
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }
        public CollectionViewModel OldViewModel = null;

        public CollectionPageView()
        {
            this.InitializeComponent();
            xThumbs.Loaded += (sender, e) =>
            {
                DataContextChanged -= CollectionPageView_DataContextChanged;
                DataContextChanged += CollectionPageView_DataContextChanged;
                if (ViewModel != null)
                    CollectionPageView_DataContextChanged(null, null);
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Width = xThumbs.ActualWidth;
            };
            xThumbs.SizeChanged += (sender, e) =>
            {
                FitPageButton_Click(null, null);
                foreach (var t in ViewModel.ThumbDocumentViewModels)
                    t.Width = xThumbs.ActualWidth;
            };
            Unloaded += (sender, e) =>
            {
                if (ViewModel != null)
                    ViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                if (OldViewModel != null)
                    OldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                OldViewModel = null;
            };


            this.AddHandler(KeyDownEvent, new KeyEventHandler(SelectionElement_KeyDown), true);
            this.xDocContainer.AddHandler(PointerReleasedEvent, new PointerEventHandler(xDocContainer_PointerReleased), true);
            this.GotFocus += CollectionPageView_GotFocus;
            this.LosingFocus += CollectionPageView_LosingFocus;
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PageDocumentViewModels.Clear();
            ViewModel.ThumbDocumentViewModels.Clear();
            foreach (var pageDoc in ViewModel.DocumentViewModels.Select((vm) => vm.DocumentController))
            {
                var pageViewDoc = pageDoc.GetViewCopy();
                pageViewDoc.SetLayoutDimensions(double.NaN, double.NaN);

                PageDocumentViewModels.Add(new DocumentViewModel(pageViewDoc) { Undecorated = true });

                DocumentController thumbnailImageViewDoc = null;
                if (!string.IsNullOrEmpty(pageDoc.Title))
                {
                    thumbnailImageViewDoc = new NoteDocuments.PostitNote(pageDoc.Title.Substring(0, Math.Min(100, pageDoc.Title.Length))).Document;
                    thumbnailImageViewDoc.GetDataDocument().SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(pageDoc.GetDataDocument().GetId(), KeyStore.TitleKey), true);
                }
                else
                {
                    thumbnailImageViewDoc = (pageDoc.GetDereferencedField(KeyStore.ThumbnailFieldKey, null) as DocumentController ?? pageDoc).GetViewCopy();
                }
                thumbnailImageViewDoc.SetLayoutDimensions(xThumbs.ActualWidth, double.NaN);
                ViewModel.ThumbDocumentViewModels.Add(new DocumentViewModel(thumbnailImageViewDoc) { Undecorated = true, BackgroundBrush = new SolidColorBrush(Colors.Transparent) });
            }
            CurPage = PageDocumentViewModels.LastOrDefault();
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
            if (args != null)
                args.Handled = true;
            if (ViewModel != null && ViewModel != OldViewModel)
            {
                if (OldViewModel != null)
                    OldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged1;
                ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
                DocumentViewModels_CollectionChanged(null, null);
                OldViewModel = ViewModel;
            }
        }

        private void DocumentViewModels_CollectionChanged1(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void xThumbs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (xThumbs.Items.Count > 0)
                xThumbs.SelectedIndex = 0;
        }
        private void xThumbs_Loaded(object sender, RoutedEventArgs e)
        {
            if (xThumbs.Items.Count > 0)
                xThumbs.SelectedIndex = 0;
        }

        KeyController CaptionKey = null;
        KeyController DisplayKey = null;
        string DisplayString = "";

        public void SetHackCaptionText(KeyController captionKey)
        {
            captionKey = captionKey ?? KeyStore.TitleKey;
            if (captionKey != null && CurPage != null)
            {
                var bodyDoc = CurPage.DataDocument.GetDereferencedField<DocumentController>(DisplayKey, null)?.GetDataDocument(null);
                xDocTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
                CaptionKey = captionKey;

                var currPageBinding = new FieldBinding<TextController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = CurPage.DataDocument,
                    Key = CaptionKey,
                    FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DontDereference,
                    Converter = new ObjectToStringConverter()
                };
                xDocTitle.AddFieldBinding(TextBox.TextProperty, currPageBinding);

                if (bodyDoc?.Equals(CurPage.DataDocument) == false)
                    bodyDoc?.SetField(CaptionKey,
                        new DocumentReferenceController(CurPage.DataDocument.GetId(),
                            CaptionKey), true);

                xDocTitle.Height = 50;
                xDocCaptionRow.Height = new GridLength(50);
            }
        }
        public void SetHackBodyDoc(KeyController documentKey, string keyasgn)
        {
            documentKey = documentKey ?? KeyStore.DataKey;
            if (documentKey != null && CurPage != null)
            {
                DisplayString = keyasgn;
                DisplayKey = documentKey;
                xDocView.Visibility = Windows.UI.Xaml.Visibility.Visible;
                var data = CurPage.DataDocument.GetDereferencedField(DisplayKey,null);
                if (!string.IsNullOrEmpty(DisplayString))
                {
                    var keysToReplace = new Regex("#[a-z0-9A-Z_]*").Matches(DisplayString);
                    var replacedString = DisplayString;
                    foreach (var keyToReplace in keysToReplace)
                    {
                        var k = KeyController.LookupKeyByName(keyToReplace.ToString().Substring(1));
                        if (k != null)
                        {
                            var value = CurPage.DataDocument.GetDereferencedField<TextController>(k, null)?.Data;
                            if (value != null)
                                replacedString = replacedString.Replace(keyToReplace.ToString(), value);
                        }
                    }
                    var img = replacedString == "this" ? CurPage.DocumentController : MainPage.Instance.xMainSearchBox.SearchForFirstMatchingDocument(replacedString, CurPage.DataDocument);
                    if (img != null && (!(data is DocumentController) || !img.GetDataDocument(null).Equals((data as DocumentController).GetDataDocument(null))))
                    {
                        var imgView = img.GetViewCopy();
                        imgView.GetWidthField().NumberFieldModel.Data = double.NaN;
                        imgView.GetHeightField().NumberFieldModel.Data = double.NaN;
                        data = imgView;
                    }
                }
                if (data != null)
                {
                    CurPage.DataDocument.SetField(DisplayKey, data, true);
                    var db = new DataBox(data,0, 0, double.NaN, double.NaN); // CurPage.DocumentController.GetDataDocument(null).GetField(DisplayKey));
                    
                    xDocView.DataContext = new DocumentViewModel(db.Document) { Undecorated = true };
                }
            }
        }
        public DocumentViewModel CurPage
        {
            get { return (xThumbs.SelectedIndex < PageDocumentViewModels.Count && xThumbs.SelectedIndex >= 0) ? PageDocumentViewModels[xThumbs.SelectedIndex] : this.xDocView.DataContext as DocumentViewModel; }
            set
            {
                xDocView.DataContext = value;

                var binding = new FieldBinding<TextController>()
                {
                    Mode = BindingMode.OneWay,
                    Document = value.DataDocument,
                    Key = KeyStore.TitleKey,
                    Tag = "CurPage Title Binding"
                };

                if (value.Content is CollectionView)
                {
                    value.Content.Loaded -= Content_Loaded;
                    value.Content.Loaded += Content_Loaded;
                }

                SetHackBodyDoc(DisplayKey, DisplayString); // TODO order of these maters cause of writing body doc
                SetHackCaptionText(CaptionKey);
                
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
            (cview?.CurrentView as CollectionFreeformView)?.ViewManipulationControls?.FitToParent();
        }

        private void Cview_Loaded(object sender, RoutedEventArgs e)
        {
            var cview = sender as CollectionView;
            cview.ViewModel.ContainerDocument.FieldModelUpdated -= ContainerDocument_FieldModelUpdated;
            cview.ViewModel.ContainerDocument.FieldModelUpdated += ContainerDocument_FieldModelUpdated;
            (cview?.CurrentView as CollectionFreeformView)?.ViewManipulationControls?.FitToParent();
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
                _element.ViewManipulationControls.FitToParent();
                _element.Loaded -= _element_Loaded;
            }
        }
        

        #region DragAndDrop

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void CollectionViewOnDragOver(object sender, DragEventArgs e)
        {
            this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void Top_Drop(object sender, DragEventArgs e)
        {
            this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (!e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
                return;
            var dragModel = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
            var keyString = dragModel.GetDraggedDocument().GetDataDocument(null)?.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data;
            if (keyString?.StartsWith("#") == true)
            {
                var key = keyString.Substring(1);
                var splits = key.Split("=");
                var keyName = splits.Length > 0 ? splits[0] : key;
                var k = KeyController.LookupKeyByName(keyName);
                var keyasgn = splits.Length > 1 ? splits[1] : "";
                if (k == null)
                {
                    k = new KeyController(UtilShared.GenerateNewId(), keyName);
                }
                SetHackBodyDoc(k, keyasgn);
                
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            e.Handled = true;
        }

        private void Bottom_Drop(object sender, DragEventArgs e)
        {
            this.xDockSpots.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (!e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
                return;
            var dragModel = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
            if (dragModel.DraggedKey != null)
            {
                SetHackCaptionText(dragModel.DraggedKey);
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else
            {
                var keyString = dragModel.GetDraggedDocument().GetDataDocument(null)?.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data;
                if (keyString?.StartsWith("#") == true)
                {
                    var key = keyString.Substring(1);
                    var k = KeyController.LookupKeyByName(key);
                    if (k == null)
                    {
                        var splits = key.Split("=");
                        k = new KeyController(UtilShared.GenerateNewId(), splits.Length > 0 ? splits[0] : key);
                    }
                    SetHackCaptionText(k);

                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
            e.Handled = true;
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }
        #endregion

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = PageDocumentViewModels.IndexOf(CurPage);
                xThumbs.SelectedIndex = Math.Max(0, ind - 1);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurPage != null)
            {
                var ind = PageDocumentViewModels.IndexOf(CurPage);
                xThumbs.SelectedIndex = Math.Min(PageDocumentViewModels.Count - 1, ind + 1);
            }
        }

        private void FitPageButton_Click(object sender, RoutedEventArgs e)
        {
            var _element = ((CurPage?.Content as CollectionView)?.CurrentView as CollectionFreeformView);
            _element?.ViewManipulationControls?.FitToParent();
        }

        private void xThumbs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ind = xThumbs.SelectedIndex;
            if (PageDocumentViewModels.Count > 0)
                CurPage = PageDocumentViewModels[Math.Max(0, Math.Min(PageDocumentViewModels.Count - 1, ind))];
            if (xThumbs.ItemsPanelRoot != null &&  ind >= 0 && ind < xThumbs.ItemsPanelRoot.Children.Count)
            {
                var x = xThumbs.ItemsPanelRoot.Children[ind].GetFirstDescendantOfType<Control>();
                if (x != null)
                {

                    try
                    {
                        x.Focus(FocusState.Keyboard);
                        x.Focus(FocusState.Pointer);
                    } catch (Exception)
                    {

                    }
                }
            }
        }

        private void xDrag_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
            e.Complete();
        }

        private void xDragContainer_DragStarting(UIElement sender, DragStartingEventArgs e)
        {
            e.Data.RequestedOperation = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            e.AllowedOperations       = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            e.Data.Properties.Add("Width", xDocView.ActualWidth);
            e.Data.Properties.Add("Height", xDocView.ActualHeight);
            e.Data.Properties.Add(nameof(DragDocumentModel), new DragDocumentModel(CurPage.DocumentController, true));
        }

        private void SelectionElement_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled)
                return;
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

        private void TextBlock_GettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            try
            {
                args.Cancel = true;
            } catch (Exception)
            {

            }
        }

        private void xThumbs_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try { 
                xThumbs.Focus(FocusState.Pointer);
            }
            catch (Exception)
            {

            }
        }
        private void xDocContainer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var focus = FocusManager.GetFocusedElement() as FrameworkElement;
            if (focus == null || focus.GetFirstAncestorOfType<CollectionPageView>() != this || xThumbs.GetDescendants().Contains(focus))
            {
                xThumbs.Focus(FocusState.Pointer);
                e.Handled = true;
            }
        }

        private void xThumbs_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
            foreach (var m in e.Items)
            {
                var ind = ViewModel.ThumbDocumentViewModels.IndexOf(m as DocumentViewModel);
                e.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(PageDocumentViewModels[ind].DocumentController, true);
            }
        }

        private void xThumbs_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
        }
    }
}
