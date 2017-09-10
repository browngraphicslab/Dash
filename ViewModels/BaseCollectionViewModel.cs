using DashShared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Dash
{
    public abstract class BaseCollectionViewModel : BaseSelectionElementViewModel, ICollectionViewModel
    {
        private bool _canDragItems;
        private double _cellSize;
        private bool _isInterfaceBuilder;
        private ListViewSelectionMode _itemSelectionMode;
        private static SelectionElement _previousDragEntered;

        public virtual KeyController CollectionKey => DocumentCollectionFieldModelController.CollectionKey;

        protected BaseCollectionViewModel(bool isInInterfaceBuilder) : base(isInInterfaceBuilder)
        {
            IsInterfaceBuilder = isInInterfaceBuilder;
            SelectionGroup = new List<DocumentViewModel>();
        }

        public bool IsInterfaceBuilder
        {
            get { return _isInterfaceBuilder; }
            private set { SetProperty(ref _isInterfaceBuilder, value); }
        }

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();

        // used to keep track of groups of the currently selected items in a collection
        public List<DocumentViewModel> SelectionGroup { get; set; }

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);
       

        private void DisplayDocument(ICollectionView collectionView, DocumentController docController, Point? where = null)
        {
            if (where != null)
            {
                var h = docController.GetHeightField().Data;
                var w = docController.GetWidthField().Data;

                w = double.IsNaN(w) ? 0 : w;
                h = double.IsNaN(h) ? 0 : h;

                var pos = (Point)where;
                docController.GetPositionField().Data = new Point(pos.X - w / 2, pos.Y - h / 2);
            }
            collectionView.ViewModel.AddDocument(docController, null);
            //DBTest.DBDoc.AddChild(docController);
        }

        private void DisplayDocuments(ICollectionView collectionView, IEnumerable<DocumentController> docControllers, Point? where = null)
        {
            foreach (var documentController in docControllers)
            {
                DisplayDocument(collectionView, documentController, where);
            }
        }

        #region Grid or List Specific Variables I want to Remove

        public double CellSize
        {
            get { return _cellSize; }
            protected set { SetProperty(ref _cellSize, value); }
        }

        public bool CanDragItems
        {
            get { return _canDragItems; }
            set { SetProperty(ref _canDragItems, value); }
            // 
        }

        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }

        #endregion


        #region DragAndDrop

        /// <summary>
        /// fired by the starting collection when a drag event is initiated
        /// </summary>
        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(true);

            var carrier = ItemsCarrier.Instance;
            carrier.Source = this;
            carrier.Payload = e.Items.Cast<DocumentViewModel>().Select(dvmp => dvmp.DocumentController).ToList();
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        /// <summary>
        /// fired by the starting collection when a drag event is over
        /// </summary>
        public void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(false);

            var carrier = ItemsCarrier.Instance;

            if (carrier.Source == carrier.Destination)
                return; // we don't want to drop items on ourself

            if (e.DropResult == DataPackageOperation.Move)
                RemoveDocuments(ItemsCarrier.Instance.Payload);

            carrier.Payload.Clear();
            carrier.Source = null;
            carrier.SourceCollection = null;
            carrier.Destination = null;
        }

        /// <summary>
        /// Fired by a collection when an item is dropped on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            if (DocumentView.DragDocumentView != null)
                DocumentView.DragDocumentView.IsHitTestVisible = true;
            this.RemoveDragDropIndication(sender as SelectionElement);

            var isDraggedFromKeyValuePane = e.DataView.Properties[KeyValuePane.DragPropertyKey] != null;
            var isDraggedFromLayoutBar = e.DataView.Properties[InterfaceBuilder.LayoutDragKey]?.GetType() == typeof(InterfaceBuilder.DisplayTypeEnum);
            if (isDraggedFromLayoutBar || isDraggedFromKeyValuePane) return;

            //return if it's an operator dragged from compoundoperatoreditor listview 
            if (e.Data?.Properties[CompoundOperatorFieldController.OperationBarDragKey] != null) return;

            e.Handled = true;

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
                var action =
                    e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as
                        Action<ICollectionView, DragEventArgs>;
                action?.Invoke(sender as ICollectionView, e);
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    if (storageFile.Path.EndsWith(".pdf"))
                    {
                        var fields = new Dictionary<KeyController, FieldModelController>();
                        fields[DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(new List<DocumentController>());
                        var pdfDoc = new DocumentController(fields, DashConstants.DocumentTypeStore.CollectionDocument);
                        var pdfLayout =
                            new CollectionBox(new ReferenceFieldModelController(pdfDoc.GetId(), DocumentCollectionFieldModelController.CollectionKey), 0, 0, double.NaN, double.NaN, CollectionView.CollectionViewType.Grid).Document;
                        pdfDoc.SetActiveLayout(pdfLayout, forceMask: true, addToLayoutList: true);


                        var where = sender is CollectionFreeformView ?
                            Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                            new Point();
                        var pdf = await PdfDocument.LoadFromFileAsync(storageFile);
                        var children = pdfDoc.GetDereferencedField(DocumentCollectionFieldModelController.CollectionKey, null) as DocumentCollectionFieldModelController;
                        for (uint i = 0; i < pdf.PageCount; i++)
                            using (PdfPage page = pdf.GetPage(i))
                            {
                                var stream = new InMemoryRandomAccessStream();
                                await page.RenderToStreamAsync(stream);
                                BitmapImage src = new BitmapImage();
                                await src.SetSourceAsync(stream);
                                var pageImage = new Image();
                                pageImage.Source = src;

                                // start of hack to display PDF as a single page image (instead of using a new Pdf document model type)
                                var renderTargetBitmap = await RenderImportImageToBitmapToOvercomeUWPSandbox(pageImage);

                                var pageFields = new Dictionary<KeyController, FieldModelController>();
                                pageFields[DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(new List<DocumentController>());
                                var pageDoc = new DocumentController(pageFields, DashConstants.DocumentTypeStore.CollectionDocument);
                                var pageLayout =
                                    new CollectionBox(new ReferenceFieldModelController(pageDoc.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
                                pageDoc.SetActiveLayout(pageLayout, forceMask: true, addToLayoutList: true);

                                var image = new AnnotatedImage(new Uri(storageFile.Path), await ToBase64(renderTargetBitmap), 
                                    "page " + i, 300, 300 * renderTargetBitmap.PixelHeight / renderTargetBitmap.PixelWidth);
                                DBTest.ResetCycleDetection();
                                var pagechildren = pageDoc.GetDereferencedField(DocumentCollectionFieldModelController.CollectionKey, null) as DocumentCollectionFieldModelController;
                                pagechildren?.AddDocument(image.Document);
                                children?.AddDocument(pageDoc);
                            }
                        MainPage.Instance.DisplayDocument(pdfDoc, where);
                    }
                    else if (storageFile.Path.EndsWith(".pptx"))
                    {
                        var sFile = items[0] as StorageFile;
                        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                        StorageFile file = await localFolder.CreateFileAsync("filename.pptx", CreationCollisionOption.ReplaceExisting);
                        await sFile.CopyAndReplaceAsync(file);
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }
                    else
                    {
                        var sFile = items[0] as StorageFile;
                        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                        StorageFile file = await localFolder.CreateFileAsync("filename.jpg", CreationCollisionOption.ReplaceExisting);
                        await sFile.CopyAndReplaceAsync(file);

                        var where = sender is CollectionFreeformView ?
                            Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                            new Point();

                        var image = new AnnotatedImage(new Uri(file.Path), null, file.Path, 300, 300);
                        MainPage.Instance.DisplayDocument(image.Document, where);
                    }
                }
            }
            var carrier = ItemsCarrier.Instance;
            var sourceIsCollection = carrier.Source != null;
            if (sourceIsCollection)
            {
                
                // we don't want to drop items on ourself
                if (carrier.Source.Equals(carrier.Destination)) // works with documents? 
                    return;

                if (carrier.Destination != null && carrier.SourceCollection?.ParentCollection != null)    // cancel collection dropping to its container collection 
                    if (carrier.SourceCollection.ParentCollection.ViewModel.Equals(carrier.Destination))
                        return;

                var where = sender is CollectionFreeformView ?
                    Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                    new Point();

                DisplayDocuments(sender as ICollectionView, carrier.Payload, where);
            }
            SetGlobalHitTestVisiblityOnSelectedItems(false);
        }

        private static async Task<RenderTargetBitmap> RenderImportImageToBitmapToOvercomeUWPSandbox(Image imagery)
        {
            Grid HackGridToRenderImage, HackGridToHideRenderImageWhenRendering;

            HackGridToRenderImage = new Grid();
            HackGridToHideRenderImageWhenRendering = new Grid();
            var w = (imagery.Source as BitmapImage).PixelWidth;
            var h = (imagery.Source as BitmapImage).PixelHeight;
            if (w == 0)
                w = 100;
            if (h == 0)
                h = 100;
            imagery.Width = HackGridToRenderImage.Width = HackGridToHideRenderImageWhenRendering.Width = w;
            imagery.Height = HackGridToRenderImage.Height = HackGridToHideRenderImageWhenRendering.Height = h;
            //HackGridToHideRenderImageWhenRendering.Background = new SolidColorBrush(Colors.Blue);
            HackGridToHideRenderImageWhenRendering.Children.Add(HackGridToRenderImage);
            HackGridToRenderImage.Background = new SolidColorBrush(Colors.Blue);
            HackGridToRenderImage.Children.Add(imagery);
            HackGridToHideRenderImageWhenRendering.Opacity = 0.0;

            var renderTargetBitmap = new RenderTargetBitmap();
            (MainPage.Instance.MainDocView.Content as Grid).Children.Add(HackGridToHideRenderImageWhenRendering);
            await renderTargetBitmap.RenderAsync(HackGridToRenderImage);
            (MainPage.Instance.MainDocView.Content as Grid).Children.Remove(HackGridToHideRenderImageWhenRendering);

            return renderTargetBitmap;
        }

        async Task<string>  ToBase64(RenderTargetBitmap bitmap)
        {
            var image = (await bitmap.GetPixelsAsync()).ToArray();
            var width = (uint)bitmap.PixelWidth;
            var height = (uint)bitmap.PixelHeight;

            double dpiX = 96;
            double dpiY = 96;

            var encoded = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, encoded);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, width, height, dpiX, dpiY, image);
            await encoder.FlushAsync();
            encoded.Seek(0);

            var bytes = new byte[encoded.Size];
            await encoded.AsStream().ReadAsync(bytes, 0, bytes.Length);

            var base64String = Convert.ToBase64String(bytes);
            return base64String;
        }

        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            this.HighlightPotentialDropTarget(sender as SelectionElement);

            SetGlobalHitTestVisiblityOnSelectedItems(true);

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Clear();
                e.DragUIOverride.Caption = e.DataView.Properties.Title;
                e.DragUIOverride.IsContentVisible = false;
                e.DragUIOverride.IsGlyphVisible = false;
                ItemsCarrier.Instance.CurrBaseModel = (MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView);
            }

            var sourceIsCollection = ItemsCarrier.Instance.Source != null;
            if (sourceIsCollection)
            {
                var sourceIsOurself = ItemsCarrier.Instance.Source.Equals(this);
                e.AcceptedOperation = sourceIsOurself
                    ? DataPackageOperation.None // don't accept drag event from ourself
                                : DataPackageOperation.Move;

                ItemsCarrier.Instance.Destination = this;
            }

            // the soruce is assumed to be outside the app
            if ((e.AllowedOperations & DataPackageOperation.Move) != 0)
            {
                e.AcceptedOperation |= DataPackageOperation.Copy;
                e.DragUIOverride.IsContentVisible = true;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Fired by a collection when the item being dragged is no longer over it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            // fix the problem of CollectionViewOnDragEnter not firing when leaving a collection to the outside one 
            var basemodel = ItemsCarrier.Instance.CurrBaseModel; 
            if (basemodel != sender as ICollectionView)
            {
                basemodel.ViewModel.CollectionViewOnDragEnter(basemodel, e);
                basemodel = sender as ICollectionView;
            }

            var element = sender as SelectionElement;
            if (element != null)
            {
                this.ChangeIndicationColor(element, Colors.Transparent);
                element.HasDragLeft = true;
                var parent = element.ParentSelectionElement;
                // if the current collection fires a dragleave event and its parent hasn't
                if (!parent.HasDragLeft)
                {
                    this.ChangeIndicationColor(parent, Colors.LightSteelBlue);
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Highlight a collection when drag enters it to indicate which collection would the document move to if the user were to drop it now
        /// </summary>
        private void HighlightPotentialDropTarget(SelectionElement element)
        {
            // change background of collection to indicate which collection is the potential drop target, determined by the drag entered event
            if (element != null)
            {
                // only one collection should be highlighted at a time
                if (_previousDragEntered != null)
                {
                    this.ChangeIndicationColor(_previousDragEntered, Colors.Transparent);
                }
                element.HasDragLeft = false;
                _previousDragEntered = element;
                this.ChangeIndicationColor(element, Colors.LightSteelBlue);
            }
        }

        /// <summary>
        /// Remove highlight from target drop collection and border from DocumentView being dragged
        /// </summary>
        /// <param name="element"></param>
        private void RemoveDragDropIndication(SelectionElement element)
        {
            // remove drop target indication when doc is dropped
            if (element != null)
            {
                this.ChangeIndicationColor(element, Colors.Transparent);
                _previousDragEntered = null;
            }

            // remove border from DocumentView once it is dropped onto a collection
            if (DocumentView.DragDocumentView != null)
                DocumentView.DragDocumentView.OuterGrid.BorderThickness = new Thickness(0);
            DocumentView.DragDocumentView = null;
        }

        private void ChangeIndicationColor(SelectionElement element, Color fill)
        {
            (element as CollectionFreeformView)?.SetDropIndicationFill(new SolidColorBrush(fill));
            (element as CollectionGridView)?.SetDropIndicationFill(new SolidColorBrush(fill));
            (element as CollectionListView)?.SetDropIndicationFill(new SolidColorBrush(fill));
        }

        #endregion


        #region Selection

        public void ToggleSelectAllItems(ListViewBase listView)
        {
            var isAllItemsSelected = listView.SelectedItems.Count == DocumentViewModels.Count;
            if (!isAllItemsSelected)
                listView.SelectAll();
            else
                listView.SelectedItems.Clear();
        }

        public void ToggleSelectFreeformView()
        {

        }

        public void XGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listViewBase = sender as ListViewBase;
            SelectionGroup.Clear();
            SelectionGroup.AddRange(listViewBase?.SelectedItems.Cast<DocumentViewModel>());
        }

        #endregion

        #region Virtualization

        public void ContainerContentChangingPhaseZero(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            //args.Handled = true;
            //if (args.Phase != 0) throw new Exception("Please start in stage 0");
            //var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            //var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            //var border = (Viewbox)rootGrid?.FindName("xBorder");
            //Debug.Assert(backdrop != null, "backdrop != null");
            //backdrop.Visibility = Visibility.Visible;
            //backdrop.ClearValue(FrameworkElement.WidthProperty);
            //backdrop.ClearValue(FrameworkElement.HeightProperty);
            //backdrop.Width = backdrop.Height = 250;
            //backdrop.xProgressRing.Visibility = Visibility.Visible;
            //backdrop.xProgressRing.IsActive = true;
            //Debug.Assert(border != null, "border != null");
            //border.Visibility = Visibility.Collapsed;
            //args.RegisterUpdateCallback(ContainerContentChangingPhaseOne);

            //if (args.Phase != 0)
            //{
            //    throw new Exception("We should be in phase 0 but we are not");
            //}

            //args.RegisterUpdateCallback(ContainerContentChangingPhaseOne);
            //args.Handled = true;
        }

        private void ContainerContentChangingPhaseOne(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            //if (args.Phase != 1) throw new Exception("Please start in phase 1");
            //var rootGrid = (Grid)args.ItemContainer.ContentTemplateRoot;
            //var backdrop = (DocumentView)rootGrid?.FindName("XBackdrop");
            //var border = (Viewbox)rootGrid?.FindName("xBorder");
            //var document = (DocumentView)border?.FindName("xDocumentDisplay");
            //Debug.Assert(backdrop != null, "backdrop != null");
            //Debug.Assert(border != null, "border != null");
            //Debug.Assert(document != null, "document != null");
            //backdrop.Visibility = Visibility.Collapsed;
            //backdrop.xProgressRing.IsActive = false;
            //border.Visibility = Visibility.Visible;
            //document.IsHitTestVisible = false;
            //var dvParams = ((ObservableCollection<DocumentViewModelParameters>)sender.ItemsSource)?[args.ItemIndex];

            //if (document.ViewModel == null)
            //{
            //    document.DataContext =
            //        new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);               
            //}
            //else if (document.ViewModel.DocumentController.GetId() != dvParams.Controller.GetId())
            //{
            //    document.ViewModel.Dispose();
            //    document.DataContext =
            //        new DocumentViewModel(dvParams.Controller, dvParams.IsInInterfaceBuilder, dvParams.Context);
            //}
            //else
            //{
            //    document.ViewModel.Dispose();
            //}
        }

        #endregion

    }
}