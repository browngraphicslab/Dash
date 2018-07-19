using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.UI;
using Dash.Models.DragModels;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Color = Windows.UI.Color;
using Size = Windows.Foundation.Size;
using Windows.ApplicationModel.AppService;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Core;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    {
        static UserControl _previousDragEntered;
        bool _canDragItems = true;
        double _cellSize;
        private bool _isLoaded;
        private SettingsView.WebpageLayoutMode WebpageLayoutMode => SettingsView.Instance.WebpageLayout;

        ListViewSelectionMode _itemSelectionMode;
        public ListController<DocumentController> CollectionController => ContainerDocument.GetDereferencedField<ListController<DocumentController>>(CollectionKey, null);
        private Point _pasteWhereHack;

        //this table saves requests to appData for htmlImport
        private static ValueSet table = null;
        //this is for copy and paste

        #region StandardView
        public enum StandardViewLevel
        {
            None = 0,
            Overview = 1,
            Region = 2,
            Detail = 3
        }

        private StandardViewLevel _viewLevel = StandardViewLevel.None;

        private double _prevScale = 1;

        DataPackage dataPackage = new DataPackage();
        public StandardViewLevel ViewLevel
        {
            get => _viewLevel;
            set
            {
                SetProperty(ref _viewLevel, value);
                UpdateViewLevel();
            }
        }

        public double PrevScale
        {
            get => _prevScale;
            set => SetProperty(ref _prevScale, value);
        }
        private void UpdateViewLevel()
        {
            foreach (var dvm in DocumentViewModels)
            {
                dvm.ViewLevel = ViewLevel;
                dvm.DecorationState = false;
            }
        }
        #endregion

        void PanZoomFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            OnPropertyChanged(nameof(TransformGroup));
        }
        void ActualSizeFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            if (!MainPage.Instance.IsShiftPressed())
                FitContents();   // pan/zoom collection so all of its contents are visible
        }

        public void Loaded(bool isLoaded)
        {
            _isLoaded = isLoaded;
            if (isLoaded)
            {
                ContainerDocument.AddFieldUpdatedListener(CollectionKey, collectionFieldChanged);
                ContainerDocument.AddFieldUpdatedListener(KeyStore.PanPositionKey, PanZoomFieldChanged);
                ContainerDocument.AddFieldUpdatedListener(KeyStore.PanZoomKey, PanZoomFieldChanged);
                ContainerDocument.AddFieldUpdatedListener(KeyStore.ActualSizeKey, ActualSizeFieldChanged);
                // force the view to refresh now that everything is loaded.  These changed handlers will cause the
                // TransformGroup to be re-read by thew View and will force FitToContents if necessary.
                PanZoomFieldChanged(null, null, null); // bcz: setting the TransformGroup scale before this view is loaded causes a hard crash at times.
                ActualSizeFieldChanged(null, null, null);
                //Stuff may have changed in the collection while we weren't listening, so remake the list
                if (CollectionController != null)
                {
                    DocumentViewModels.Clear();
                    addViewModels(CollectionController.TypedData);
                }

                _lastDoc = ContainerDocument;
            }
            else
            {
                _lastDoc?.RemoveFieldUpdatedListener(KeyStore.PanPositionKey, PanZoomFieldChanged);
                _lastDoc?.RemoveFieldUpdatedListener(KeyStore.PanZoomKey, PanZoomFieldChanged);
                _lastDoc?.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, ActualSizeFieldChanged);
                _lastDoc?.RemoveFieldUpdatedListener(CollectionKey, collectionFieldChanged);
                _lastDoc = null;
            }
        }
        public InkController InkController;
        public TransformGroupData TransformGroup
        {
            get
            {
                var trans = ContainerDocument.GetField<PointController>(KeyStore.PanPositionKey)?.Data ?? new Point();
                var scale = ContainerDocument.GetField<PointController>(KeyStore.PanZoomKey)?.Data ?? new Point(1, 1);
                if (trans.Y > 0 && !SettingsView.Instance.NoUpperLimit)   // clamp the y offset so that we can only scroll down
                {
                    trans = new Point(trans.X, 0);
                }
                return new TransformGroupData(trans, _isLoaded ? scale : new Point(1, 1));
            }
            set
            {
                ContainerDocument.SetField<PointController>(KeyStore.PanPositionKey, value.Translate, true);
                ContainerDocument.SetField<PointController>(KeyStore.PanZoomKey, value.ScaleAmount, true);
            }
        }

        public DocumentController ContainerDocument { get; set; }
        public KeyController CollectionKey { get; set; }
        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<DocumentViewModel> ThumbDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public AdvancedCollectionView BindableDocumentViewModels { get; set; }

        public CollectionViewModel(DocumentController containerDocument, KeyController fieldKey, Context context = null) : base()
        {
            BindableDocumentViewModels = new AdvancedCollectionView(DocumentViewModels, true) { Filter = o => true };

            SetCollectionRef(containerDocument, fieldKey);

            CellSize = 250; // TODO figure out where this should be set
                            //  OutputKey = KeyStore.CollectionOutputKey;  // bcz: this wasn't working -- can't assume the collection is backed by a document with a CollectionOutputKey.  
        }

        DocumentController _lastDoc = null;
        /// <summary>
        /// Sets the reference to the field that contains the documents to display.
        /// </summary>
        /// <param name="refToCollection"></param>
        /// <param name="context"></param>
        public void SetCollectionRef(DocumentController containerDocument, KeyController fieldKey)
        {
            var wasLoaded = _isLoaded;
            Loaded(false);

            ContainerDocument = containerDocument;
            CollectionKey = fieldKey;
            if (_isLoaded && wasLoaded)
            {
                Loaded(true);
            }
            _lastDoc = ContainerDocument;
        }
        /// <summary>
        /// pan/zooms the document so that all of its contents are visible.  
        /// This only applies of the CollectionViewType is Freeform/Standard, and the CollectionFitToParent field is true
        /// </summary>
        public void FitContents()
        {
            if (FitToParent && (ViewType == CollectionView.CollectionViewType.Freeform || ViewType == CollectionView.CollectionViewType.Standard))
            {
                var parSize = ContainerDocument.GetActualSize() ?? new Point();
                var r = Rect.Empty;
                foreach (var d in DocumentViewModels)
                {
                    r.Union(d.Bounds);
                }
                if (r.Width != 0 && r.Height != 0)
                {
                    var rect = new Rect(new Point(), new Point(parSize.X, parSize.Y));
                    var scaleWidth = r.Width / r.Height > rect.Width / rect.Height;
                    var scaleAmt = scaleWidth ? rect.Width / r.Width : rect.Height / r.Height;
                    var scale = new Point(scaleAmt, scaleAmt);
                    var trans = new Point(-r.Left * scaleAmt, -r.Top * scaleAmt);
                    if (scaleAmt > 0)
                    {
                        TransformGroup = new TransformGroupData(trans, scale);
                    }
                }
            }
        }

        void collectionFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context1)
        {
            if (args.Action == DocumentController.FieldUpdatedAction.Update && args.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs docListFieldArgs)
            {
                updateViewModels(docListFieldArgs);
            }
            else
            {
                if (args.NewValue != null)
                {
                    var collectionFieldModelController =
                        args.NewValue.DereferenceToRoot<ListController<DocumentController>>(null);
                    if (collectionFieldModelController != null)
                    {
                        updateViewModels(new ListController<DocumentController>.ListFieldUpdatedEventArgs(
                            ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace,
                            collectionFieldModelController.GetElements(), new List<DocumentController>(), 0));
                    }
                }
            }
        }

        #region DocumentModel and DocumentViewModel Data Changes

        public string Tag;
        void updateViewModels(ListController<DocumentController>.ListFieldUpdatedEventArgs args)
        {
            switch (args.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content:
                    // we only care about changes to the Hidden field of the contained documents.
                    foreach (var d in args.NewItems)
                    {
                        var visible = !d.GetHidden();
                        var shown = DocumentViewModels.Where((dvm) => dvm.DocumentController.Equals(d)).Count() > 0;
                        if (visible && !shown)
                            addViewModels(new List<DocumentController>(new DocumentController[] { d }));
                        if (!visible && shown)
                            removeViewModels(new List<DocumentController>(new DocumentController[] { d }));
                    }
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                    addViewModels(args.NewItems);
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                    DocumentViewModels.Clear();
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                    removeViewModels(args.OldItems);
                    break;
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
                    DocumentViewModels.Clear();
                    addViewModels(args.NewItems);
                    break;
            }
        }

        void addViewModels(List<DocumentController> documents)
        {
                using (BindableDocumentViewModels.DeferRefresh())
                {
                    foreach (var documentController in documents)
                    {
                        if (!documentController.GetHidden())
                            DocumentViewModels.Add(new DocumentViewModel(documentController));
                    }
                }
        }

        void removeViewModels(List<DocumentController> documents)
        {
            using (BindableDocumentViewModels.DeferRefresh())
            {
                var ids = documents.Select(doc => doc.Id);
                var vms = DocumentViewModels.Where(vm => ids.Contains(vm.DocumentController.Id)).ToList();
                foreach (var vm in vms)
                {
                    DocumentViewModels.Remove(vm);
                }
            }
        }

        public void AddDocuments(List<DocumentController> documents)
        {
            using (BindableDocumentViewModels.DeferRefresh())
            {
                foreach (var doc in documents)
                {
                    AddDocument(doc);
                }
            }
        }


        bool createsCycle(DocumentController newDoc)
        {
            var curLayout = ContainerDocument.GetActiveLayout() ?? ContainerDocument;
            var newLayout = newDoc.GetActiveLayout() ?? newDoc;
            if (newLayout.DocumentType.Equals(CollectionBox.DocumentType) && curLayout.GetDataDocument().Equals(newLayout.GetDataDocument()))
                return true;
            if (newLayout.DocumentType.Equals(CollectionBox.DocumentType))
            {
                var newDocList = newLayout.GetDereferencedField(KeyStore.DataKey, null) as ListController<DocumentController>;
                foreach (var subDoc in newDocList.TypedData)
                {
                    var subLayout = subDoc.GetActiveLayout() ?? subDoc;
                    if (subLayout.DocumentType.Equals(CollectionBox.DocumentType))
                    {
                        if (curLayout.GetDataDocument().Equals(subLayout.GetDataDocument()))
                            return true;
                        if (createsCycle(subDoc))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a document to the given collectionview.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="context"></param>
        public void AddDocument(DocumentController doc)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (!createsCycle(doc))
                {
                    doc.CaptureNeighboringContext();

                    ContainerDocument.GetDataDocument().AddToListField(CollectionKey, doc);
                }
                if (ViewLevel.Equals(StandardViewLevel.Overview) || ViewLevel.Equals(StandardViewLevel.Region))
                    UpdateViewLevel();
            }
        }


        public void RemoveDocuments(List<DocumentController> documents)
        {
            using (UndoManager.GetBatchHandle())
            {
                using (BindableDocumentViewModels.DeferRefresh())
                {
                    foreach (var doc in documents)
                    {
                        RemoveDocument(doc);
                    }
                }
            }
        }

        public void RemoveDocument(DocumentController document)
        {
            using (UndoManager.GetBatchHandle())
            {
                // just update the collection, the colllection will update our view automatically
                ContainerDocument.GetDataDocument().RemoveFromListField(CollectionKey, document);
            }
        }

        #endregion

        #region Grid or List Specific Variables I want to Remove

        // todo: this should be tied to a field on the collection so that it will be persisted
        public double CellSize
        {
            get { return _cellSize; }
            set { SetProperty(ref _cellSize, value); }
        }

        public bool CanDragItems
        {
            get { return _canDragItems; }
            set { SetProperty(ref _canDragItems, value); }
        }
        public bool FitToParent
        {
            get => ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionFitToParentKey, null)?.Data == "true";
            set => ContainerDocument.SetFitToParent(value);
        }
        public CollectionView.CollectionViewType ViewType
        {
            get => Enum.Parse<CollectionView.CollectionViewType>(ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionViewTypeKey, null)?.Data ?? CollectionView.CollectionViewType.Grid.ToString());
            set => ContainerDocument.SetField<TextController>(KeyStore.CollectionViewTypeKey, value.ToString(), true);
        }


        /// <summary>
        /// Determines whether a document can be added to the collection based on whether it would create a layout cycle.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool CanDrop(DocumentController doc)
        {
            return !createsCycle(doc);
        }

        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }

        #endregion


        #region DragAndDrop

        List<DocumentController> pivot(List<DocumentController> docs, KeyController pivotKey)
        {
            var dictionary = new Dictionary<object, Dictionary<KeyController, List<object>>>();
            var pivotDictionary = new Dictionary<object, DocumentController>();

            foreach (var d in docs.Select((dd) => dd.GetDataDocument()))
            {
                var fieldDict = setupPivotDoc(pivotKey, dictionary, pivotDictionary, d);
                if (fieldDict == null)
                    continue;
                foreach (var f in d.EnumDisplayableFields())
                    if (!f.Key.Equals(pivotKey))
                    {
                        if (!fieldDict.ContainsKey(f.Key))
                        {
                            fieldDict.Add(f.Key, new List<object>());
                        }
                        fieldDict[f.Key].Add(f.Value.GetValue(new Context(d)));
                    }
            }

            var pivoted = new List<DocumentController>();
            foreach (var d in dictionary)
            {
                var doc = pivotDictionary.ContainsKey(d.Key) ? pivotDictionary[d.Key] : null;
                if (doc == null)
                    continue;
                foreach (var f in d.Value)
                    if (doc.GetField(f.Key) == null)
                    {
                        var items = new List<FieldControllerBase>();
                        foreach (var i in f.Value)
                        {
                            if (i is string)
                                items.Add(new TextController(i as string));
                            else if (i is double)
                                items.Add(new NumberController((double)i));
                            else if (i is DocumentController)
                                items.Add((DocumentController)i);
                        }

                        if (items.Count > 0)
                        {
                            FieldControllerBase field = null;

                            //TODO tfs: why are we making copies of all of these fields?
                            if (items.First() is TextController)
                                field = (items.Count == 1) ? (FieldControllerBase)new TextController((items.First() as TextController).Data) :
                                                new ListController<TextController>(items.OfType<TextController>());
                            else if (items.First() is NumberController)
                                field = (items.Count == 1) ? (FieldControllerBase)new NumberController((items.First() as NumberController).Data) :
                                                new ListController<NumberController>(items.OfType<NumberController>());
                            else if (items.First() is RichTextController)
                                field = (items.Count == 1) ? (FieldControllerBase)new RichTextController((items.First() as RichTextController).Data) :
                                                new ListController<RichTextController>(items.OfType<RichTextController>());
                            else if (items.First() is DocumentController)
                                field = (items.Count == 1) ? (FieldControllerBase)(items.First() as DocumentController) :
                                               new ListController<DocumentController>(items.OfType<DocumentController>());
                            if (field != null)
                                doc.SetField(f.Key, field, true);
                        }
                    }
                pivoted.Add(doc);
            }
            return pivoted;
        }

        Dictionary<KeyController, List<object>> setupPivotDoc(KeyController pivotKey, Dictionary<object, Dictionary<KeyController, List<object>>> dictionary, Dictionary<object, DocumentController> pivotDictionary, DocumentController d)
        {
            var obj = d.GetDataDocument().GetDereferencedField(pivotKey, null)?.GetValue(null);
            DocumentController pivotDoc = null;
            if (obj != null && !dictionary.ContainsKey(obj))
            {
                var pivotField = d.GetDataDocument().GetField(pivotKey);
                pivotDoc = (pivotField as ReferenceController)?.GetDocumentController(null);
                if (d.GetDataDocument().GetAllPrototypes().Contains(pivotDoc) || pivotDoc == null || pivotDoc.DocumentType.Equals(DashConstants.TypeStore.OperatorType))
                {
                    pivotDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>()
                    {
                    }, DocumentType.DefaultType);
                    if (obj is string)
                    {
                        pivotDoc.SetField(pivotKey, new TextController(obj as string), true);
                    }
                    else if (obj is RichTextModel.RTD)
                    {
                        pivotDoc.SetField(pivotKey, new RichTextController(obj as RichTextModel.RTD), true);
                    }
                    else if (obj is double)
                    {
                        pivotDoc.SetField(pivotKey, new NumberController((double)obj), true);
                    }
                    else if (obj is DocumentController)
                    {
                        pivotDoc = obj as DocumentController;
                    }
                    else if (obj is ListController<DocumentController>)
                    {
                        pivotDoc.SetField(pivotKey, new ListController<DocumentController>(obj as List<DocumentController>), true);
                    }
                    //DBTest.DBDoc.AddChild(pivotDoc);
                    d.SetField(pivotKey, new DocumentReferenceController(pivotDoc, pivotKey), true);
                }
                pivotDictionary.Add(obj, pivotDoc);
                dictionary.Add(obj, new Dictionary<KeyController, List<object>>());
            }

            if (obj != null)
            {
                d.SetField(pivotKey, new DocumentReferenceController(pivotDictionary[obj], pivotKey), true);
                return dictionary[obj];
            }
            return null;
        }

        KeyController expandCollection(KeyController fieldKey, List<DocumentController> getDocs, List<DocumentController> subDocs, KeyController showField)
        {
            foreach (var d in getDocs)
            {
                var fieldData = d.GetDataDocument().GetDereferencedField(fieldKey, null);
                if (fieldData is ListController<DocumentController>)
                    foreach (var dd in (fieldData as ListController<DocumentController>).TypedData)
                    {
                        var dataDoc = dd.GetDataDocument();

                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
                        expandedDoc.SetField(showField, dataDoc, true);
                        subDocs.Add(expandedDoc);
                    }
                else if (fieldData is ListController<TextController>)
                    foreach (var dd in (fieldData as ListController<TextController>).Data)
                    {
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
                        expandedDoc.SetField(showField, new TextController((dd as TextController).Data), true);
                        subDocs.Add(expandedDoc);
                    }
                else if (fieldData is ListController<NumberController>)
                    foreach (var dd in (fieldData as ListController<NumberController>).Data)
                    {
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
                        expandedDoc.SetField(showField, new NumberController((dd as NumberController).Data), true);
                        subDocs.Add(expandedDoc);
                    }
            }

            return showField;
        }

        public async void Paste(DataPackageView dvp, Point where)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (dvp.Contains(StandardDataFormats.StorageItems))
                {
                    var droppedDoc = await FileDropHelper.HandleDrop(where, dvp, this);
                    AddDocument(droppedDoc);
                }
                else if (dvp.Contains(StandardDataFormats.Bitmap))
                {
                    PasteBitmap(dvp, where);
                }
                else if (dvp.Contains(StandardDataFormats.Rtf))
                {
                    var text = await dvp.GetRtfAsync();
                    if (text != "")
                    {
                        if (SettingsView.Instance.MarkdownEditOn)
                        {
                            var postitNote = new MarkdownNote(text: text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                        }
                        else
                        {
                            var postitNote = new RichTextNote(text: text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                        }
                    }
                }
                else if (dvp.Contains(StandardDataFormats.Html) && false)
                {
                    //Create an instance for word app
                    //Microsoft.Office.Interop.Word.Application winword = new Microsoft.Office.Interop.Word.Application();

                    ////Set animation status for word application
                    //winword.ShowAnimation = false;

                    ////Set status for word application is to be visible or not.
                    //winword.Visible = false;

                    ////Create a missing variable for missing value
                    //object missing = System.Reflection.Missing.Value;

                    ////Create a new document
                    //Microsoft.Office.Interop.Word.Document document = winword.Documents.Add(ref missing, ref missing, ref missing, ref missing);
                    //document.Content.Paste();
                    //document.Content.Select();
                    //var dvp2 = Clipboard.GetContent();
                    //if (dvp2.Contains(StandardDataFormats.Rtf))
                    //{
                    //}
                }
                else if (dvp.Contains(StandardDataFormats.Text))
                {
                    var text = await dvp.GetTextAsync();
                    if (text != "")
                    {
                        if (SettingsView.Instance.MarkdownEditOn)
                        {
                            var postitNote = new MarkdownNote(text: text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                        }
                        else
                        {
                            var postitNote = new RichTextNote(text: text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                        }
                    }
                }
            }
        }

        private async void PasteBitmap(DataPackageView dvp, Point where)
        {
            using (UndoManager.GetBatchHandle())
            {
                var streamRef = await dvp.GetBitmapAsync();
                WriteableBitmap writeableBitmap = new WriteableBitmap(400, 400);
                await writeableBitmap.SetSourceAsync(await streamRef.OpenReadAsync());

                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile savefile = await storageFolder.CreateFileAsync("paste.jpg",
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);
                IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                // Get pixels of the WriteableBitmap object 
                Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                // Save the image file with jpg extension 
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)writeableBitmap.PixelWidth,
                    (uint)writeableBitmap.PixelHeight, 96.0, 96.0, pixels);
                await encoder.FlushAsync();
                var dp = new DataPackage();
                dp.SetStorageItems(new IStorageItem[] { savefile });
                var droppedDoc = await FileDropHelper.HandleDrop(where, dp.GetView(), this);
                AddDocument(droppedDoc);
            }
        }

        /// <summary>
        /// Fired by a collection when an item is dropped on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                e.Handled = true;
                // accept move, then copy, and finally accept whatever they requested (for now)
                if (e.AllowedOperations.HasFlag(DataPackageOperation.Move))
                    e.AcceptedOperation = DataPackageOperation.Move;
                else e.AcceptedOperation = e.DataView.RequestedOperation;

                RemoveDragDropIndication(sender as UserControl);

                var senderView = (sender as CollectionView)?.CurrentView as ICollectionView;
                var where = new Point();
                if (senderView is CollectionFreeformBase)
                    where = Util.GetCollectionFreeFormPoint(senderView as CollectionFreeformBase,
                        e.GetPosition(MainPage.Instance.MainDocView));
                else if (DocumentViewModels.Count > 0)
                {
                    var lastPos = DocumentViewModels.Last().Position;
                    where = new Point(lastPos.X + DocumentViewModels.Last().ActualSize.X, lastPos.Y);
                }





                // if we drag from the file system
                if (e.DataView?.Contains(StandardDataFormats.StorageItems) == true)
                {
                    try
                    {
                        var droppedDoc = await FileDropHelper.HandleDrop(where, e.DataView, this);
                        if (droppedDoc != null)
                            AddDocument(droppedDoc);
                        return;
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception);
                    }
                }

                if (e.DataView?.Contains(StandardDataFormats.Html) == true)
                {
                    _pasteWhereHack = where;
                    var html = await e.DataView.GetHtmlFormatAsync();

                    //get url of where this html is coming from
                    var htmlStartIndex = html.IndexOf("<html>", StringComparison.Ordinal);
                    var beforeHtml = html.Substring(0, htmlStartIndex);
                    var introParts = beforeHtml.Split("\r\n").Where(s => s != "").ToList();
                    var uri = introParts.Last().Substring(10);
                    var addition = "<br><div> From < <a href = \"" + uri + "\" >" + uri + "</a>> </div>";

                    //update html length in intro - the way that word reads HTML is kinda funny
                    //it uses numbers in heading that say when html starts and ends, so in order to edit html, 
                    //we must change these numbers
                    var endingInfo = introParts.ElementAt(2);
                    var endingNum = (Convert.ToInt32(endingInfo.Substring(8)) + addition.Length)
                        .ToString().PadLeft(10, '0');
                    introParts[2] = endingInfo.Substring(0, 8) + endingNum;
                    var endingInfo2 = introParts.ElementAt(4);
                    var endingNum2 = (Convert.ToInt32(endingInfo2.Substring(12)) + addition.Length)
                        .ToString().PadLeft(10, '0');
                    introParts[4] = endingInfo2.Substring(0, 12) + endingNum2;
                    var newHtmlStart = String.Join("\r\n", introParts) + "\r\n";


                    //get parts so additon is before closing
                    var endPoint = html.IndexOf("<!--EndFragment-->", StringComparison.Ordinal);
                    var mainHtml = html.Substring(htmlStartIndex, endPoint - htmlStartIndex);
                    var htmlClose = html.Substring(endPoint);
                   

                    //combine all parts
                    html = newHtmlStart + mainHtml + addition + htmlClose;

                    //Overrides problematic in-line styling pdf.js generates, such as transparent divs and translucent elements
                    html = String.Concat(html,
                        @"<style>
                      div
                      {
                        color: black !important;
                      }
                      html * {
                        opacity: 1.0 !important
                      }
                    </style>"
                    );

                    var splits = new Regex("<").Split(html);
                    var imgs = splits.Where((s) => new Regex("img.*src=\"[^>\"]*").Match(s).Length > 0).ToList();
                    var text = e.DataView.Contains(StandardDataFormats.Text)
                        ? (await e.DataView.GetTextAsync()).Trim()
                        : "";
                    if (string.IsNullOrEmpty(text) && imgs.Count == 1)
                    {
                        var srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(imgs.First().ToString()).Value;
                        var src = srcMatch.Substring(6, srcMatch.Length - 6);
                        var imgNote = new ImageNote(new Uri(src), where, new Size(), src.ToString());
                        AddDocument(imgNote.Document);
                        return;
                    }




                    DocumentController htmlNote = null;

                    if ((WebpageLayoutMode.Equals(SettingsView.WebpageLayoutMode.HTML) && !MainPage.Instance.IsCtrlPressed()) || 
                        (WebpageLayoutMode.Equals(SettingsView.WebpageLayoutMode.RTF) && MainPage.Instance.IsCtrlPressed()))
                    {

                        htmlNote = new HtmlNote(html, BrowserView.Current?.Title ?? "", where: where).Document;

                    }

                    else if ((WebpageLayoutMode.Equals(SettingsView.WebpageLayoutMode.RTF) && !MainPage.Instance.IsCtrlPressed()) || 
                        (WebpageLayoutMode.Equals(SettingsView.WebpageLayoutMode.HTML) && MainPage.Instance.IsCtrlPressed()))
                    {

                        //copy html to clipboard
                        dataPackage.RequestedOperation = DataPackageOperation.Copy;
                        dataPackage.SetHtmlFormat(html);
                        Clipboard.SetContent(dataPackage);

                        //to import from html
                        // create a ValueSet from the datacontext, used to create word doc to copy html to
                        var table = new ValueSet { { "REQUEST", "HTML to RTF" } };

                        await DotNetRPC.CallRPCAsync(table);
                        var dataPackageView = Clipboard.GetContent();
                        var richtext = await dataPackageView.GetRtfAsync();
                        //richtext +=
                        //    "{\\field{\\*\\fldinst HYPERLINK \"" + uri +
                        //            "\"} {\\fldrslt" + uri + "}}";
                        htmlNote = new RichTextNote(richtext, _pasteWhereHack, new Size(300, 300)).Document;
                    }

                    else if (WebpageLayoutMode.Equals(SettingsView.WebpageLayoutMode.Default))
                    {
                        var layoutType = await MainPage.Instance.GetLayoutType();
                        if (layoutType.Equals(SettingsView.WebpageLayoutMode.HTML))
                        {
                            htmlNote = new HtmlNote(html, BrowserView.Current?.Title ?? "", where: where).Document;
                        }
                        else if (layoutType.Equals(SettingsView.WebpageLayoutMode.RTF))
                        {
                            //copy html to clipboard
                            dataPackage.RequestedOperation = DataPackageOperation.Copy;
                            dataPackage.SetHtmlFormat(html);
                            Clipboard.SetContent(dataPackage);

                            //to import from html
                            // create a ValueSet from the datacontext, used to create word doc to copy html to
                            var table = new ValueSet { { "REQUEST", "HTML to RTF" } };

                            await DotNetRPC.CallRPCAsync(table);
                            var dataPackageView = Clipboard.GetContent();
                            if (dataPackageView.Contains(StandardDataFormats.Rtf))
                            {
                                var richtext = await dataPackageView.GetRtfAsync();
                                //richtext +=
                                //    "{\\field{\\*\\fldinst HYPERLINK \"" + uri +
                                //    "\"} {\\fldrslt" + uri + "}}";
                                htmlNote = new RichTextNote(richtext, _pasteWhereHack, new Size(300, 300)).Document;
                            }
                            else
                                htmlNote = new HtmlNote(html, BrowserView.Current?.Title ?? "", where: where).Document;
                        }

                    }




                    //Syncfusion version
                    /*
                    WordDocument d = new WordDocument();
                    d.EnsureMinimal();
                    d.LastParagraph.AppendHTML(html);
                    MemoryStream mem = new MemoryStream();
                    d.Save(mem, FormatType.Rtf);
                    mem.Position = 0;
                    byte[] arr = new byte[mem.Length];
                    arr = mem.ToArray();
                    string rtf = Encoding.Default.GetString(arr);
                    var t = new RichTextNote(rtf, where, new Size(300,double.NaN));
                    //var matches = new Regex(".*:.*").Matches(rtf);
                    //foreach (var match in matches)
                    //{
                    //    var pair = new Regex(":").Split(match.ToString());
                    //    t.Document.GetDataDocument().SetField(KeyController.LookupKeyByName(pair[0],true), new TextController(pair[1].Trim('\r')), true);
                    //}
                    AddDocument(t.Document);
                    */

                    var strings = text.Split(new char[] { '\r' });
                    foreach (var str in html.Split(new char[] { '\r' }))
                    {
                        var matches = new Regex("^SourceURL:.*").Matches(str.Trim());
                        if (matches.Count != 0)
                        {
                            htmlNote.GetDataDocument().SetField<TextController>(KeyStore.SourecUriKey,
                                matches[0].Value.Replace("SourceURL:", ""), true);
                            break;
                        }
                    }

                    if (imgs.Count() == 0)
                    {
                        var matches = new Regex(".{1,100}:.*").Matches(text.Trim());
                        var title = (matches.Count == 1 && matches[0].Value == text)
                            ? new Regex(":").Split(matches[0].Value)[0]
                            : "";
                        htmlNote.GetDataDocument().SetField<TextController>(KeyStore.DocumentTextKey, text, true);
                        if (title == "")
                            foreach (var match in matches)
                            {
                                var pair = new Regex(":").Split(match.ToString());
                                htmlNote.GetDataDocument()
                                    .SetField<TextController>(new KeyController(pair[0], pair[0]),
                                        pair[1].Trim(), true);
                            }
                        else
                            htmlNote.SetTitle(title);
                    }
                    else
                    {
                        var related = new List<DocumentController>();
                        foreach (var img in imgs)
                        {
                            var srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(img.ToString()).Value;
                            var src = srcMatch.Substring(6, srcMatch.Length - 6);
                            var i = new ImageNote(new Uri(src), new Point(), new Size(), src.ToString());
                            related.Add(i.Document);
                        }

                        htmlNote.GetDataDocument()
                            .SetField<ListController<DocumentController>>(
                                new KeyController("Html Images", "Html Images"), related, true); //
                                                                                                 //htmlNote.GetDataDocument(null).SetField(new KeyController("Html Images", "Html Images"), new ListController<DocumentController>(related), true);
                        htmlNote.GetDataDocument().SetField<TextController>(KeyStore.DocumentTextKey, text, true);
                        foreach (var str in strings)
                        {
                            var matches = new Regex("^.{1,100}:.*").Matches(str.Trim());
                            if (matches.Count != 0)
                            {
                                foreach (var match in matches)
                                {
                                    var pair = new Regex(":").Split(match.ToString());
                                    htmlNote.GetDataDocument()
                                        .SetField<TextController>(new KeyController(pair[0]),
                                            pair[1].Trim(),
                                            true);
                                }
                            }
                        }
                    }

                    //make context navigate back to website
                    htmlNote.GetDataDocument().SetField(KeyStore.WebContextKey, new TextController(uri), true);

                    AddDocument(htmlNote);
                }

                else if (e.DataView?.Contains(StandardDataFormats.Rtf) == true)
                {
                    var text = await e.DataView.GetRtfAsync();

                    var t = new RichTextNote(text, where, new Size(300, double.NaN));
                    AddDocument(t.Document);
                }


                else if (e.DataView?.Contains(StandardDataFormats.Bitmap) == true)
                {
                    var bmp = await e.DataView.GetBitmapAsync();
                    IRandomAccessStreamWithContentType streamWithContent = await bmp.OpenReadAsync();
                    byte[] buffer = new byte[streamWithContent.Size];
                    using (DataReader reader = new DataReader(streamWithContent))
                    {
                        await reader.LoadAsync((uint)streamWithContent.Size);
                        reader.ReadBytes(buffer);
                    }

                    var localFolder = ApplicationData.Current.LocalFolder;
                    var uniqueFilePath =
                        UtilShared.GenerateNewId() + ".jpg"; // somehow this works for all images... who knew
                    var localFile =
                        await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
                    localFile.OpenStreamForWriteAsync().Result.Write(buffer, 0, buffer.Count());

                    var img = await ImageToDashUtil.CreateImageBoxFromLocalFile(localFile, "dropped image");
                    AddDocument(img);
                    var t = new ImageNote(new Uri(localFile.FolderRelativeId));
                    // var t = new AnnotatedImage(null, Convert.ToBase64String(buffer), "", "");
                    AddDocument(t.Document);
                }
                else if (e.DataView?.Properties.ContainsKey(nameof(DragCollectionFieldModel)) == true)
                {
                    var dragData = (DragCollectionFieldModel)e.DataView.Properties[nameof(DragCollectionFieldModel)];
                    var showField = dragData.FieldKey;

                    if (showField != null && dragData.CollectionReference != null)
                    {
                        var subDocs = new List<DocumentController>();
                        if (dragData.DraggedItems?.Any() == true)
                        {
                            var firstDocValue = dragData.DraggedItems.First().GetDataDocument()
                                .GetDereferencedField(showField, null);
                            if (firstDocValue is ListController<DocumentController> ||
                                firstDocValue?.GetValue(null) is List<FieldControllerBase>)
                                showField = expandCollection(dragData.FieldKey, dragData.DraggedItems, subDocs,
                                    showField);
                            else if (firstDocValue is DocumentController)
                                subDocs = dragData.DraggedItems.Select((d) =>
                                        d.GetDataDocument().GetDereferencedField<DocumentController>(showField, null))
                                    .ToList();
                            else subDocs = pivot(dragData.DraggedItems, showField);
                        }

                        var cnote = new CollectionNote(where, dragData.ViewType);
                        if (subDocs != null)
                            cnote.SetDocuments(new List<DocumentController>(subDocs));
                        else
                            cnote.Document.GetDataDocument()
                                .SetField(KeyStore.DataKey, dragData.CollectionReference, true);
                        cnote.Document.SetField(CollectionDBView.FilterFieldKey, showField, true);
                        AddDocument(cnote.Document);
                    }
                    else
                    {
                        var parentDocs = (sender as FrameworkElement)?.GetAncestorsOfType<CollectionView>()
                            .Select((cv) => cv.ParentDocument?.ViewModel?.DataDocument);
                        var filteredDocs = dragData.DraggedItems.Where((d) =>
                            !parentDocs.Contains(d.GetDataDocument()) &&
                            d?.DocumentType?.Equals(DashConstants.TypeStore.MainDocumentType) == false);

                        var payloadLayoutDelegates = filteredDocs.Select((p) =>
                        {
                            if (p.GetActiveLayout() == null &&
                                p.GetDereferencedField(KeyStore.DocumentContextKey, null) == null)
                                p.SetActiveLayout(new DefaultLayout().Document, true, true);
                            var newDoc = e.AcceptedOperation == DataPackageOperation.Move ? p.GetSameCopy(where)
                                       : e.AcceptedOperation == DataPackageOperation.Link ? p.GetViewCopy(where)
                                       : p.GetCopy(where);
                            if (double.IsNaN(newDoc.GetWidthField().Data))
                                newDoc.SetWidth(dragData.Width ?? double.NaN);
                            if (double.IsNaN(newDoc.GetHeightField().Data))
                                newDoc.SetHeight(dragData.Height ?? double.NaN);
                            return newDoc;
                        });
                        AddDocument(new CollectionNote(where, dragData.ViewType, 500, 300,
                            payloadLayoutDelegates.ToList()).Document);
                    }
                }
                // if the user drags a data document
                else if (e.DataView?.Properties.ContainsKey(nameof(List<DragDocumentModel>)) == true)
                {
                    var dragModel = (List<DragDocumentModel>)e.DataView.Properties[nameof(List<DragDocumentModel>)];
                    foreach (var d in dragModel.Where((dm) => dm.CanDrop(sender as FrameworkElement)))
                    {
                        var start = dragModel.First().DraggedDocument.GetPositionField().Data;
                        AddDocuments(dragModel.Where((dm) => dm.CanDrop(sender as FrameworkElement)).Select(
                            (dm) => dm.GetDropDocument(
                                new Point(dm.DraggedDocument.GetPositionField().Data.X - start.X + where.X,
                                    dm.DraggedDocument.GetPositionField().Data.Y - start.Y + where.Y), true)).ToList());
                    }
                }
                // if the user drags a data document
                else if (e.DataView?.Properties.ContainsKey(nameof(DragDocumentModel)) == true)
                {
                    var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
                    if (dragModel.LinkSourceView != null
                    ) // The LinkSourceView is non-null when we're dragging the green 'link' dot from a document
                    {
                        // bcz:  Needs to support LinksFrom as well as LinksTo...
                        if (MainPage.Instance.IsShiftPressed()
                        ) // if shift is pressed during this drag, we want to see all the linked documents to this document as a collection
                        {
                            var regions = dragModel.DraggedDocument.GetDataDocument()
                                .GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)
                                ?.TypedData;
                            if (regions != null)
                            {
                                var links = regions.SelectMany((r) =>
                                    r.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData);
                                var targets = links.SelectMany((l) =>
                                    l.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData);
                                var aliases = targets.Select((t) =>
                                {
                                    var vc = t.GetViewCopy();
                                    vc.SetHidden(false);
                                    return vc;
                                });
                                var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Grid, 500, 300,
                                    aliases.ToList());
                                AddDocument(cnote.Document);
                            }
                        }
                        else if (MainPage.Instance.IsCtrlPressed()
                        ) // if control is pressed during this drag, we want to see a collection of the actual link documents
                        {
                            var regions = dragModel.DraggedDocument.GetDataDocument()
                                .GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)
                                ?.TypedData;
                            var directlyLinkedTo = dragModel.DraggedDocument.GetDataDocument()
                                .GetLinks(KeyStore.LinkToKey)?.TypedData;
                            var regionLinkedTo = regions?.SelectMany((r) =>
                                r.GetDataDocument().GetLinks(KeyStore.LinkToKey)?.TypedData);
                            if (regionLinkedTo != null || directlyLinkedTo != null)
                            {
                                var links = regionLinkedTo != null
                                    ? regionLinkedTo.ToList()
                                    : new List<DocumentController>();
                                if (directlyLinkedTo != null)
                                    links.AddRange(directlyLinkedTo);
                                var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Grid, 500, 300,
                                    links.ToList());
                                AddDocument(cnote.Document);
                            }
                        }
                        else // if no modifiers are pressed, we want to create a new annotation document and link it to the source document (region)
                        {
                            var dragDoc = dragModel.DraggedDocument;
	                        if (dragModel.LinkSourceView != null && KeyStore.RegionCreator[dragDoc.DocumentType] != null)
	                        {
		                        // if RegionCreator exists, then dragDoc becomes the region document
								dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](dragModel.LinkSourceView);
	                        }
							// note is the new annotation textbox that is created
							var note = new RichTextNote("<annotation>", where).Document;
	                        note.SetField(KeyStore.AnnotationVisibilityKey, new BoolController(true), true);

                            dragDoc.Link(note);
                            AddDocument(note);
                        }
                    }
                    else if (dragModel.CanDrop(sender as FrameworkElement))
                    {
                        //var draggedDocument = dragModel.GetDraggedDocument();
                        //if (draggedDocument.DocumentType.Equals(DashConstants.TypeStore.CollectionBoxType) &&
                        //    (sender as DependencyObject).GetFirstAncestorOfType<DocumentView>()?.ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) == false &&
                        //    this.DocumentViewModels.Where((dvm) => dvm.DocumentController.Equals(draggedDocument)).Count() == 0)
                        //{
                        //    HandleTemplateLayoutDrop(dragModel);
                        //    e.Handled = true;
                        //    return;
                        //}
                        //else
                        AddDocument(dragModel.GetDropDocument(where));
                    }
                }
            }
        }

        /// <summary>
        /// If you drop a collection, the collection serves as a layout template for all the documents in the collection and changes the way they are displayed.
        /// TODO: This needs an explicit GUI drop target for setting a template...
        /// </summary>
        /// <param name="dragModel"></param>
        private void HandleTemplateLayoutDrop(DragDocumentModel dragModel)
        {
            var template = dragModel.DraggedDocument;
            var templateFields = template.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null)?.TypedData;
            foreach (var dvm in DocumentViewModels.ToArray())
            {
                var listOfFields = new List<DocumentController>();
                var doc = dvm.DocumentController;
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
                    var templateFieldDataRef = (templateField as DocumentController)?.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data;
                    if (!string.IsNullOrEmpty(templateFieldDataRef) && templateFieldDataRef.StartsWith("#"))
                    {
                        var k = new KeyController(templateFieldDataRef.Substring(1));
                        if (k != null)
                        {
                            listOfFields.Add(new DataBox(new DocumentReferenceController(doc.GetDataDocument(), k), p.X, p.Y, w, h).Document);
                        }
                    }
                    else
                        listOfFields.Add(templateField);
                }
                var cbox = new CollectionNote(new Point(), CollectionView.CollectionViewType.Freeform, maxW, maxH, listOfFields).Document;
                doc.SetField(KeyStore.ActiveLayoutKey, cbox, true);
                // dvm.OnActiveLayoutChanged(new Context(dvm.LayoutDocument));
            }
        }

        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            this.HighlightPotentialDropTarget(sender as UserControl);

            e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

            e.DragUIOverride.IsContentVisible = true;

            e.Handled = true;
        }
        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragOver(object sender, DragEventArgs e)
        {
            HighlightPotentialDropTarget(sender as UserControl);

            e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

            if (e.DataView?.Properties.ContainsKey(nameof(DragDocumentModel)) == true)
            {
                var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

                if (!dragModel.CanDrop(sender as FrameworkElement))
                    e.AcceptedOperation = DataPackageOperation.None;

            }

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
            // bcz: this fix causes a hard crash sometimes -- just call HighlightPotentialDropTarget instead?
            // fix the problem of CollectionViewOnDragEnter not firing when leaving a collection to the outside one -
            //var parentCollection = (sender as DependencyObject).GetFirstAncestorOfType<CollectionView>();
            //parentCollection?.ViewModel?.CollectionViewOnDragEnter(parentCollection.CurrentView, e);

            var element = sender as UserControl;
            if (element != null)
            {
                var color = ((SolidColorBrush)App.Instance.Resources["DragHighlight"]).Color;
                this.ChangeIndicationColor(element, color);
            }
            this.RemoveDragDropIndication(sender as UserControl);
            e.Handled = true;
        }

        /// <summary>
        /// Highlight a collection when drag enters it to indicate which collection would the document move to if the user were to drop it now
        /// </summary>
        private void HighlightPotentialDropTarget(UserControl element)
        {
            // change background of collection to indicate which collection is the potential drop target, determined by the drag entered event
            if (element != null)
            {
                // only one collection should be highlighted at a time
                if (_previousDragEntered != null)
                {
                    this.ChangeIndicationColor(_previousDragEntered, Colors.Transparent);
                }
                var color = ((SolidColorBrush)App.Instance.Resources["DragHighlight"]).Color;
                //element.HasDragLeft = false;
                _previousDragEntered = element;
                this.ChangeIndicationColor(element, color);
            }
        }

        /// <summary>
        /// Remove highlight from target drop collection and border from DocumentView being dragged
        /// </summary>
        /// <param name="element"></param>
        private void RemoveDragDropIndication(UserControl element)
        {
            // remove drop target indication when doc is dropped
            if (element != null)
            {
                this.ChangeIndicationColor(element, Colors.Transparent);
                _previousDragEntered = null;
            }
        }

        public void ChangeIndicationColor(UserControl element, Color fill)
        {
            (element as CollectionFreeformBase)?.SetDropIndicationFill(new SolidColorBrush(fill));
            (element as CollectionGridView)?.SetDropIndicationFill(new SolidColorBrush(fill));
        }


        #endregion

    }
}