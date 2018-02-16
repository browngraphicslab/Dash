using Dash.Models.DragModels;
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
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Dash.NoteDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionGridView : SelectionElement, ICollectionView
    {
        private bool _rightPressed;

        public BaseCollectionViewModel ViewModel { get; private set; }
        //private ScrollViewer _scrollViewer;
        public CollectionGridView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            //Unloaded += CollectionGridView_Unloaded;

            PointerWheelChanged += CollectionGridView_PointerWheelChanged;
        }

        private void CollectionGridView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                var point = e.GetCurrentPoint(this);

                // get the scale amount
                var scaleAmount = point.Properties.MouseWheelDelta > 0 ? 10 : -10;

                ViewModel.CellSize += scaleAmount;
                var style = new Style(typeof(GridViewItem));
                style.Setters.Add(new Setter(WidthProperty, ViewModel.CellSize));
                style.Setters.Add(new Setter(HeightProperty, ViewModel.CellSize));
                xGridView.ItemContainerStyle = style;
                e.Handled = true;
            }
        }

        public CollectionGridView(BaseCollectionViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
        //private void XGridView_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    //_scrollViewer = xGridView.GetFirstDescendantOfType<ScrollViewer>();
        //    //_scrollViewer.ViewChanging += ScrollViewerOnViewChanging;
        //    //UpdateVisibleIndices(true);
        //}

        //private int _prevOffset;
        //private void ScrollViewerOnViewChanging(object sender, ScrollViewerViewChangingEventArgs scrollViewerViewChangingEventArgs)
        //{
        //    UpdateVisibleIndices();
        //}

        //private void UpdateVisibleIndices(bool forceUpdate = false)
        //{
        //    var source = ViewModel.DocumentViewModels;
        //    _scrollViewer.UpdateLayout();
        //    var displayableOnRow = (int)(_scrollViewer.ActualWidth / ViewModel.CellSize);
        //    var displayableOnCol = (int)(_scrollViewer.ActualHeight / ViewModel.CellSize) + 1;
        //    var verticalOffset = (int)(_scrollViewer.VerticalOffset / ViewModel.CellSize);
        //    if (_prevOffset == verticalOffset && !forceUpdate) return;
        //    _prevOffset = verticalOffset;
        //    var firstIndex = verticalOffset * displayableOnRow;
        //    for (var i = firstIndex; i < firstIndex + displayableOnRow * displayableOnCol; i++)
        //    {
        //        Debug.WriteLine(i);
        //        source[i].VisibleOnView = true;
        //    }
        //}

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as BaseCollectionViewModel;

            if (vm != null)
            {
                // remove events from current view model if there is a current view model
                if (ViewModel != null)
                {
                    //xGridView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
                    //xGridView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
                    //xGridView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
                    xGridView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
                    //xGridView.PointerPressed -= ViewModel.XGridView_PointerPressed;
                    //xGridView.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed));
                }

                ViewModel = vm;
                ViewModel.SetSelected(this, IsSelected);
                //xGridView.DragItemsStarting += ViewModel.xGridView_OnDragItemsStarting;
                //xGridView.DragItemsCompleted += ViewModel.xGridView_OnDragItemsCompleted;
                //xGridView.SelectionChanged += ViewModel.XGridView_SelectionChanged;
                xGridView.ContainerContentChanging += ViewModel.ContainerContentChangingPhaseZero;
                //xGridView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed), true);
                var style = new Style(typeof(GridViewItem));
                style.Setters.Add(new Setter(WidthProperty, ViewModel.CellSize));
                style.Setters.Add(new Setter(HeightProperty, ViewModel.CellSize));
                xGridView.ItemContainerStyle = style;
            }
        }

        //private void CollectionGridView_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    if (ViewModel != null)
        //    {
        //        //xGridView.DragItemsStarting -= ViewModel.xGridView_OnDragItemsStarting;
        //        //xGridView.DragItemsCompleted -= ViewModel.xGridView_OnDragItemsCompleted;
        //        //xGridView.SelectionChanged -= ViewModel.XGridView_SelectionChanged;
        //        xGridView.ContainerContentChanging -= ViewModel.ContainerContentChangingPhaseZero;
        //        //xGridView.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ViewModel.XGridView_PointerPressed));
        //        //xGridView.Loaded -= XGridView_OnLoaded;
        //        //_scrollViewer.ViewChanging -= ScrollViewerOnViewChanging;
        //    }
        //    Unloaded -= CollectionGridView_Unloaded;
        //}

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
            ViewModel.ToggleSelectAllItems(xGridView);
        }

        #endregion

        #region DragAndDrop


        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

            var parentCollection = this.GetFirstAncestorOfType<CollectionView>();
            if (dragModel != null)
            {
                var template       = dragModel.GetDraggedDocument();
                var templateFields = template.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.CollectionKey, null)?.TypedData;
                foreach (var dvm in ViewModel.DocumentViewModels.ToArray())
                {
                    var listOfFields = new List<DocumentController>();
                    var doc  = dvm.DocumentController;
                    var maxW = 0.0;
                    var maxH = 0.0;
                    foreach (var templateField in templateFields)
                    {
                        var p = templateField.GetPositionField(null)?.Data ?? new Point();
                        var w = templateField.GetWidthField(null)?.Data ?? 10;
                        var h = templateField.GetHeightField(null)?.Data ?? 10;
                        if (p.Y + h > maxH)
                            maxH = p.Y + h;
                        if (p.X + w > maxW)
                            maxW = p.X = w;
                        var templateFieldDataRef = (templateField as DocumentController)?.GetDataDocument().GetDereferencedField<RichTextController>(RichTextNote.RTFieldKey, null)?.Data?.ReadableString;
                        if (!string.IsNullOrEmpty(templateFieldDataRef) && templateFieldDataRef.StartsWith("#"))
                        {
                            var k = KeyController.LookupKeyByName(templateFieldDataRef.Substring(1));
                            if (k != null)
                            {
                                listOfFields.Add(new DataBox(new DocumentReferenceController(doc.GetDataDocument().GetId(), k), p.X, p.Y, w, h).Document);
                            }
                        }
                        else
                            listOfFields.Add(templateField);
                    }
                    var cbox = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, maxW, maxH, listOfFields).Document;
                    doc.SetField(KeyStore.ActiveLayoutKey, cbox, true);
                    dvm.Content = null;
                    parentCollection.ViewModel.DocumentViewModels.Remove(dvm);
                    parentCollection.ViewModel.DocumentViewModels.Add(dvm);
                }
                e.Handled = true;
            }
            else ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
            XDropIndicationRectangle.Fill = fill;
        }
        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
            ViewModel.UpdateDocumentsOnSelection(isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }
        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var cv = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController.GetDataDocument(null);
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;
            OnSelected();
        }

        #endregion


        private void XGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var dvm = e.Items.Cast<DocumentViewModel>().FirstOrDefault();
            if (dvm != null)
            {
                e.Data.Properties["Collection View Model"] = ViewModel;
                e.Data.Properties["View Doc To Move"] = dvm.DocumentController;
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }

        private void XGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.None)
            {
                return;
            }
            if (args.DropResult == DataPackageOperation.Move)
            {
                var dvm = args.Items.Cast<DocumentViewModel>().FirstOrDefault();
                if (dvm != null)
                {
                    var pc = this.GetFirstAncestorOfType<CollectionView>();
                    var group = pc?.GetDocumentGroup(dvm.DocumentController) ?? dvm.DocumentController;
                    //GroupManager.RemoveGroup(pc, group);
                    ViewModel.RemoveDocument(dvm.DocumentController);
                }
            }
        }
    }
}
