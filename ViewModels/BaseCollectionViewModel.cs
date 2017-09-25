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
using static Dash.NoteDocuments;

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
        public ObservableCollection<DocumentViewModel> ThumbDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();

        // used to keep track of groups of the currently selected items in a collection
        public List<DocumentViewModel> SelectionGroup { get; set; }

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);

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


        DateTime _dragStart = DateTime.MinValue;
        /// <summary>
        /// fired by the starting collection when a drag event is initiated
        /// </summary>
        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(true);       
            e.Data.Properties.Add("DocumentControllerList", e.Items.Cast<DocumentViewModel>().Select(dvmp => dvmp.DocumentController).ToList());
            e.Data.RequestedOperation = DateTime.Now.Subtract(_dragStart).TotalMilliseconds > 1000 ? DataPackageOperation.Move : DataPackageOperation.Copy;
        }
        public void XGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _dragStart = DateTime.Now;
        }

        /// <summary>
        /// fired by the starting collection when a drag event is over
        /// </summary>
        public void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs e)
        {
            SetGlobalHitTestVisiblityOnSelectedItems(false);
            
            if (e.DropResult == DataPackageOperation.Move)
                RemoveDocuments(e.Items.Select((i)=>(i as DocumentViewModel).DocumentController).ToList());
        }

        /// <summary>
        /// Fired by a collection when an item is dropped on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            //restore previous conditions 
            if (DocumentView.DragDocumentView != null)
                DocumentView.DragDocumentView.IsHitTestVisible = true;
            this.RemoveDragDropIndication(sender as SelectionElement);

            // true if dragged from key value pane in interfacebuilder
            var isDraggedFromKeyValuePane = e.DataView.Properties[KeyValuePane.DragPropertyKey] != null;

            // true if dragged from layoutbar in interfacebuilder
            var isDraggedFromLayoutBar = e.DataView.Properties[InterfaceBuilder.LayoutDragKey]?.GetType() == typeof(InterfaceBuilder.DisplayTypeEnum);
            if (isDraggedFromLayoutBar || isDraggedFromKeyValuePane) return; // in both these cases we don't want the collection to intercept the event

            //return if it's an operator dragged from compoundoperatoreditor listview 
            if (e.Data?.Properties[CompoundOperatorFieldController.OperationBarDragKey] != null) return;

            // from now on we are handling this event!
            e.Handled = true;

            // if we drag from radial menu
            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
                var action =
                    e.DataView.Properties[RadialMenuView.RadialMenuDropKey] as
                        Action<ICollectionView, DragEventArgs>;
                action?.Invoke(sender as ICollectionView, e);
            }

            // if we drag from the file system
            var sourceIsFileSystem = e.DataView.Contains(StandardDataFormats.StorageItems);
            if (sourceIsFileSystem)
            {
                try
                {
                    await FileDropHelper.HandleDropOnCollectionAsync(sender, e, this);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    if (storageFile.Path.EndsWith(".pdf"))
                    {
                        var where = sender is CollectionFreeformView ?
                            Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                            new Point();
                        var pdfDoc = new CollectionNote(where);
                        var pdf = await PdfDocument.LoadFromFileAsync(storageFile);
                        var children = pdfDoc.DataDocument.GetDereferencedField(CollectionNote.CollectedDocsKey, null) as DocumentCollectionFieldModelController;
                        for (uint i = 0; i < pdf.PageCount; i++)
                            using (var page = pdf.GetPage(i))
                            {
                                var src    = new BitmapImage();
                                var stream = new InMemoryRandomAccessStream();
                                await page.RenderToStreamAsync(stream);
                                await src.SetSourceAsync(stream);
                                var pageImage = new Image() { Source = src };

                                // start of hack to display PDF as a single page image (instead of using a new Pdf document model type)
                                var renderTargetBitmap = await RenderImportImageToBitmapToOvercomeUWPSandbox(pageImage);
                                var image = new AnnotatedImage(new Uri(storageFile.Path), await ToBase64(renderTargetBitmap),
                                    300, 300 * renderTargetBitmap.PixelHeight / renderTargetBitmap.PixelWidth, 50, 50);
                                
                                var pageDoc = new CollectionNote(new Point(), Path.GetFileName(storageFile.Path) + ": Page " + i, image.Document).Document;
                                children?.AddDocument(pageDoc);
                            }
                        MainPage.Instance.DisplayDocument(pdfDoc.Document, where);
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
                        StorageFile file = await localFolder.CreateFileAsync(Path.GetFileName(sFile.Path), CreationCollisionOption.ReplaceExisting);
                        await sFile.CopyAndReplaceAsync(file);

                        var where = sender is CollectionFreeformView ?
                            Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                            new Point();

                        var image = new AnnotatedImage(new Uri(file.Path), null, file.Path, 300, 300);
                        MainPage.Instance.DisplayDocument(image.Document, where);
                    }
                }
            }
            else if (e.DataView != null && e.DataView.Properties.ContainsKey("DocumentControllerList"))
            {
                var items = e.DataView?.Properties.ContainsKey("DocumentControllerList") == true ?                  
                          e.DataView.Properties["DocumentControllerList"] as List<DocumentController> : null;
                var where = sender is CollectionFreeformView ?
                    Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                    new Point();

                var payloadLayoutDelegates = items.Select((p) => e.DataView.Properties.ContainsKey("View") || e.AcceptedOperation == DataPackageOperation.Move ? p.GetViewCopy(where): e.AcceptedOperation == DataPackageOperation.Link ? p.GetDataCopy(where) : p.GetCopy(where));
                AddDocuments(payloadLayoutDelegates.ToList(), null);
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
            Debug.WriteLine("CollectionViewOnDragEnter Base");
            this.HighlightPotentialDropTarget(sender as SelectionElement);

            SetGlobalHitTestVisiblityOnSelectedItems(true);

            var sourceIsRadialMenu = e.DataView.Properties[RadialMenuView.RadialMenuDropKey] != null;
            if (sourceIsRadialMenu)
            {
               e.DragUIOverride.Clear();
                e.DragUIOverride.Caption = e.DataView.Properties.Title;
                e.DragUIOverride.IsContentVisible = false;
                e.DragUIOverride.IsGlyphVisible = false;
            }
            
            e.AcceptedOperation |= (DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link) & (e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation);
            e.DragUIOverride.IsContentVisible = true;

            e.Handled = true;
        }

        

        /// <summary>
        /// Fired by a collection when the item being dragged is no longer over it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragLeave Base");
            // fix the problem of CollectionViewOnDragEnter not firing when leaving a collection to the outside one 
            var parentCollection = CollectionView.GetParentCollectionView(CollectionView.GetParentCollectionView(sender as DependencyObject));
            if (parentCollection != null)
            {
                parentCollection.ViewModel?.CollectionViewOnDragEnter(parentCollection.CurrentView, e);
            }

            var element = sender as SelectionElement;
            if (element != null)
            {
                this.ChangeIndicationColor(element, Colors.Transparent);
                element.HasDragLeft = true;
                var parent = element.ParentSelectionElement;
                // if the current collection fires a dragleave event and its parent hasn't
                if (parent != null && !parent.HasDragLeft)
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

        public void ChangeIndicationColor(SelectionElement element, Color fill)
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