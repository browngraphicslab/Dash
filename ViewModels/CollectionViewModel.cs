using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models.DragModels;
using DashShared;
using Dash;
using Microsoft.Toolkit.Uwp.UI;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    {
        static ICollectionView _previousDragEntered;
        bool    _canDragItems = true;
        bool    _isLoaded;
        DocumentController _lastContainerDocument; // if the ContainerDocument changes, this stores the previous value which is used to cleanup listener references
        private SettingsView.WebpageLayoutMode         WebpageLayoutMode => SettingsView.Instance.WebpageLayout;
        public ListController<DocumentController>      CollectionController => ContainerDocument.GetDereferencedField<ListController<DocumentController>>(CollectionKey, null);
        public InkController                           InkController        => ContainerDocument.GetDereferencedField<InkController>(KeyStore.InkDataKey, null);

        public TransformGroupData                      TransformGroup
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
        public DocumentController                      ContainerDocument { get; set; }
        public KeyController                           CollectionKey { get; set; }
        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<DocumentViewModel> ThumbDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public AdvancedCollectionView                  BindableDocumentViewModels { get; set; }
        public CollectionView.CollectionViewType       ViewType
        {
            get => Enum.Parse<CollectionView.CollectionViewType>(ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionViewTypeKey, null)?.Data ?? CollectionView.CollectionViewType.Grid.ToString());
            set => ContainerDocument.SetField<TextController>(KeyStore.CollectionViewTypeKey, value.ToString(), true);
        }
        public bool                                    CanDragItems
        {
            get { return _canDragItems; }
            set { SetProperty(ref _canDragItems, value); }
        }

        public CollectionViewModel(DocumentController containerDocument, KeyController fieldKey, Context context = null)
        {
            BindableDocumentViewModels = new AdvancedCollectionView(DocumentViewModels, true) { Filter = o => true };

            SetCollectionRef(containerDocument, fieldKey);

        }

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
            _lastContainerDocument = ContainerDocument;
        }
        /// <summary>
        /// pan/zooms the document so that all of its contents are visible.  
        /// This only applies of the CollectionViewType is Freeform/Standard, and the CollectionFitToParent field is true
        /// </summary>
        public void FitContents(CollectionView cview)
        {
            if (ContainerDocument.GetFitToParent() && (ViewType == CollectionView.CollectionViewType.Freeform || ViewType == CollectionView.CollectionViewType.Standard))
            {
                 var realPar = cview?.CurrentView.UserControl;
                 var parSize = realPar != null ? new Point(realPar.ActualWidth, realPar.ActualHeight): ContainerDocument.GetActualSize() ?? new Point();
                
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

        public void Loaded(bool isLoaded)
        {
            _isLoaded = isLoaded;
            if (isLoaded)
            {
                ContainerDocument.RemoveFieldUpdatedListener(CollectionKey, collectionFieldChanged);
                ContainerDocument.RemoveFieldUpdatedListener(KeyStore.PanPositionKey, PanZoomFieldChanged);
                ContainerDocument.RemoveFieldUpdatedListener(KeyStore.PanZoomKey, PanZoomFieldChanged);
                ContainerDocument.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, ActualSizeFieldChanged);
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

                _lastContainerDocument = ContainerDocument;
            }
            else
            {
                _lastContainerDocument?.RemoveFieldUpdatedListener(KeyStore.PanPositionKey, PanZoomFieldChanged);
                _lastContainerDocument?.RemoveFieldUpdatedListener(KeyStore.PanZoomKey, PanZoomFieldChanged);
                _lastContainerDocument?.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, ActualSizeFieldChanged);
                _lastContainerDocument?.RemoveFieldUpdatedListener(CollectionKey, collectionFieldChanged);
                _lastContainerDocument = null;
            }
        }
        void PanZoomFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            OnPropertyChanged(nameof(TransformGroup));
        }
        void ActualSizeFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            if (!MainPage.Instance.IsShiftPressed())
                FitContents(DocumentViewModels.FirstOrDefault()?.Content.GetFirstAncestorOfType<CollectionView>());   // pan/zoom collection so all of its contents are visible
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
        
        private Storyboard _lateralAdjustment = new Storyboard();
        private Storyboard _verticalAdjustment = new Storyboard();

        void updateViewModels(ListController<DocumentController>.ListFieldUpdatedEventArgs args)
        {
            switch (args.ListAction)
            {
                case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Content:
                    // we only care about changes to the Hidden field of the contained documents.
                    foreach (var d in args.NewItems)
                    {
                        //var visible = !d.GetHidden();
                        //var shown = DocumentViewModels.Any(dvm => dvm.DocumentController.Equals(d));
                        //if (visible && !shown)
                        //    addViewModels(new List<DocumentController>(new DocumentController[] { d }));
                        //if (!visible && shown)
                        //    removeViewModels(new List<DocumentController>(new DocumentController[] { d }));
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

        public bool CreatesCycle(DocumentController newDoc)
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
                        if (CreatesCycle(subDoc))
                            return true;
                    }
                }
            }
            return false;
        }

        public bool CreatesCycle(List<DocumentController> docs) => docs.Where(CreatesCycle).Any();

        /// <summary>
        /// Adds a document to the given collectionview.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="context"></param>
        public void AddDocument(DocumentController doc)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (!CreatesCycle(doc))
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

                if (document.IsMovingCollections)
                {
                    document.IsMovingCollections = false;
                    return;
                }

                PresentationView pres = MainPage.Instance.xPresentationView;
                if (pres.ViewModel != null && pres.ViewModel.PinnedNodes.Contains(document)) pres.FullPinDelete(document);
            }
        }

        #endregion

        #region StandardView
        public enum StandardViewLevel
        {
            None = 0,
            Overview = 1,
            Region = 2,
            Detail = 3
        }

        private StandardViewLevel _viewLevel = StandardViewLevel.None;
        public StandardViewLevel ViewLevel
        {
            get => _viewLevel;
            set
            {
                SetProperty(ref _viewLevel, value);
                UpdateViewLevel();
            }
        }
        private double _prevScale = 1;
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

        #region DragAndDrop
        List<DocumentController> pivot(List<DocumentController> docs, KeyController pivotKey)
        {
            var dictionary = new Dictionary<object, Dictionary<KeyController, List<object>>>();
            var pivotDictionary = new Dictionary<object, DocumentController>();

            foreach (var d in docs.Select(dd => dd.GetDataDocument()))
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
                    pivotDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
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

        public async Task<DocumentController> Paste(DataPackageView dvp, Point where)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (dvp.Contains(StandardDataFormats.StorageItems))
                {
                    var droppedDoc = await FileDropHelper.HandleDrop(dvp, where);
                    AddDocument(droppedDoc);
                    return droppedDoc;
                }

                if (dvp.Contains(StandardDataFormats.Bitmap))
                {
                    return await PasteBitmap(dvp, where);
                }

                if (dvp.Contains(StandardDataFormats.Rtf))
                {
                    var text = await dvp.GetRtfAsync();
                    if (text != "")
                    {
                        if (SettingsView.Instance.MarkdownEditOn)
                        {
                            var postitNote = new MarkdownNote(text: text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                            return postitNote;
                        }
                        else
                        {
                            var postitNote = new RichTextNote(text: text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                            return postitNote;
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
                            return postitNote;
                        }
                        else
                        {
                            string urlSource = null;
                            if (Clipboard.GetContent().Contains(StandardDataFormats.Html))
                            {
                                var html = await Clipboard.GetContent().GetHtmlFormatAsync();
                                foreach (var str in html.Split(new[] {'\r'}))
                                {
                                    var matches = new Regex("^SourceURL:.*").Matches(str.Trim());
                                    if (matches.Count != 0)
                                    {
                                        urlSource = matches[0].Value.Replace("SourceURL:", "");
                                        break;
                                    }
                                }
                            }


                            DocumentController postitNote;
                            if (Clipboard.GetContent().Properties[nameof(DocumentController)] is DocumentController sourceDoc)
                            {
                                var region = new RichTextNote("Rich text region").Document;

                                //add link to region of sourceDoc
                                var postitView = new RichTextNote(text: text, size: new Size(300, double.NaN), urlSource: region.Id);
                                postitNote = postitView.Document;
                                postitNote.GetDataDocument().SetField<TextController>(KeyStore.SourceTitleKey,
                                    sourceDoc.Title, true);
                                postitNote.GetDataDocument().AddToRegions(new List<DocumentController>{region});

                                region.SetRegionDefinition(postitNote);
                                region.SetAnnotationType(AnnotationType.Selection);

                                region.Link(sourceDoc, LinkTargetPlacement.Default);

                            }
                            else
                            {
                                postitNote = new RichTextNote(text: text, size: new Size(300, double.NaN), urlSource: urlSource).Document;
                            }

                            
                            Actions.DisplayDocument(this, postitNote, where);
                            return postitNote;
                            

                        }
                    }
                }
            }
            return null;
        }

        private async Task<DocumentController> PasteBitmap(DataPackageView dvp, Point where)
        {
            using (UndoManager.GetBatchHandle())
            {
                var streamRef = await dvp.GetBitmapAsync();
                WriteableBitmap writeableBitmap = new WriteableBitmap(400, 400);
                await writeableBitmap.SetSourceAsync(await streamRef.OpenReadAsync());

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile savefile = await storageFolder.CreateFileAsync("paste.jpg",
                    CreationCollisionOption.ReplaceExisting);
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
                var droppedDoc = await FileDropHelper.HandleDrop(dp.GetView(), where);
                AddDocument(droppedDoc);
                return droppedDoc;
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
                e.AcceptedOperation = e.AllowedOperations.HasFlag(DataPackageOperation.Move) ? DataPackageOperation.Move : e.DataView.RequestedOperation;

                RemoveDragDropIndication(sender as ICollectionView);

                var senderView = (sender as CollectionView)?.CurrentView;
                var where = new Point();
                if (senderView is CollectionFreeformBase freeformBase)
                    where = Util.GetCollectionFreeFormPoint(freeformBase, e.GetPosition(MainPage.Instance.xCanvas));
                else if (DocumentViewModels.Count > 0)
                {
                    Point lastPos = DocumentViewModels.Last().Position;
                    where = new Point(lastPos.X + DocumentViewModels.Last().ActualSize.X, lastPos.Y);
                }

				//adds all docs in the group, if applicable
	            var docView = (sender as UserControl).GetFirstAncestorOfType<DocumentView>();
				var adornmentGroups = SelectionManager.GetSelectedSiblings(docView).Where(dv => dv.ViewModel.IsAdornmentGroup).ToList();
	            adornmentGroups.ForEach(dv =>
	            {
		           AddDocument(dv.ViewModel.DataDocument);
	            });

                // EXTERNAL
                DataPackageView packageView = e.DataView;

                AddDocuments(await packageView.GetDropDocumentsOfType(DragDataTypeInfo.External, where));

                // FROM WITHIN DASH

                if (e.DataView?.Properties.ContainsKey(nameof(DragCollectionFieldModel)) == true)
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
                                subDocs = dragData.DraggedItems.Select(d =>
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
                            .Select(cv => cv.ParentDocument?.ViewModel?.DataDocument);
                        var filteredDocs = dragData.DraggedItems.Where(d =>
                            !parentDocs.Contains(d.GetDataDocument()) &&
                            d?.DocumentType?.Equals(DashConstants.TypeStore.MainDocumentType) == false).ToList();
                        
                        var payloadLayoutDelegates = filteredDocs.Select(p =>
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
                        }).ToList();
                        AddDocument(new CollectionNote(where, dragData.ViewType, 500, 300, payloadLayoutDelegates.ToList()).Document);
                    }
                }
                
                // if the user drags a data document
                else if (e.DataView?.Properties.ContainsKey(nameof(List<DragDocumentModel>)) == true)
                {
                    var dragModel = (List<DragDocumentModel>)e.DataView.Properties[nameof(List<DragDocumentModel>)];
                    foreach (var d in dragModel.Where(dm => dm.CanDrop(sender as FrameworkElement)))
                    {
                        var start = dragModel.First().DraggedDocument.GetPositionField().Data;
                        AddDocuments(dragModel.Where(dm => dm.CanDrop(sender as FrameworkElement)).Select(
                            dm => dm.GetDropDocument(
                                new Point(dm.DraggedDocument.GetPositionField().Data.X - start.X + where.X,
                                    dm.DraggedDocument.GetPositionField().Data.Y - start.Y + where.Y), true)).ToList());
                    }
                }
                
                // if the user drags a data document
                else if (e.DataView?.Properties.ContainsKey(nameof(DragDocumentModel)) == true)
                {
                    var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
                    if (dragModel.LinkSourceView != null) // The LinkSourceView is non-null when we're dragging the green 'link' dot from a document
                    {
                        // bcz:   Needs to support LinksFrom as well as LinksTo...
                        if (MainPage.Instance.IsShiftPressed() && MainPage.Instance.IsAltPressed()) // if shift is pressed during this drag, we want to see all the linked documents to this document as a collection
                        {
                            var regions = dragModel.DraggedDocument.GetDataDocument()
                                .GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)
                                ?.TypedData;
                            if (regions != null)
                            {
                                var links = regions.SelectMany(r =>
                                    r.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData);
                                var targets = links.SelectMany(l =>
                                    l.GetDataDocument().GetLinks(KeyStore.LinkToKey).TypedData);
                                var aliases = targets.Select(t =>
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
                        else if (MainPage.Instance.IsCtrlPressed() && MainPage.Instance.IsShiftPressed()) // if control is pressed during this drag, we want to see a collection of the actual link documents
                        {
                            var regions = dragModel.DraggedDocument.GetDataDocument()
                                .GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)
                                ?.TypedData;
                            var directlyLinkedTo = dragModel.DraggedDocument.GetDataDocument()
                                .GetLinks(KeyStore.LinkToKey)?.TypedData;
                            var regionLinkedTo = regions?.SelectMany(r =>
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
                        else if (MainPage.Instance.IsShiftPressed() || MainPage.Instance.IsAltPressed() || MainPage.Instance.IsCtrlPressed())
                        {
                            AddDocument(dragModel.GetDropDocument(where));
                        }
                        else// if no modifiers are pressed, we want to create a new annotation document and link it to the source document (region)
                        {
                            var dragDoc = dragModel.DraggedDocument;
	                        if (dragModel.LinkSourceView != null && KeyStore.RegionCreator[dragDoc.DocumentType] != null)
	                        {
		                        // if RegionCreator exists, then dragDoc becomes the region document
								dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](dragModel.LinkSourceView);
	                        }

                            var freebase2 = senderView as CollectionFreeformBase;
                            if (dragModel.LinkType != null)
                            {
                                var noteLocation = e.GetPosition(freebase2?.GetCanvas());
                                if (freebase2 != null)
                                    freebase2.RenderPreviewTextbox(noteLocation, dragDoc, dragModel.LinkType, "");
                                else e.Handled = false;
                                return;
                            }
							// note is the new annotation textbox that is created
							//DocumentController note = new RichTextNote("<annotation>", where).Document;

                            //ActionTextBox inputBox = MainPage.Instance.xMainTreeView.xLinkInputBox;
                            //Storyboard fadeIn = MainPage.Instance.xMainTreeView.xLinkInputIn;
                            //Storyboard fadeOut = MainPage.Instance.xMainTreeView.xLinkInputOut;
							/*
                            ActionTextBox inputBox = MainPage.Instance.xLinkInputBox;
                            Storyboard fadeIn = MainPage.Instance.xLinkInputIn;
                            Storyboard fadeOut = MainPage.Instance.xLinkInputOut;

                            where = e.GetPosition(MainPage.Instance.xCanvas);

                            var moveTransform = new TranslateTransform {X = where.X, Y = where.Y};
                            inputBox.RenderTransform = moveTransform;

                            inputBox.AddKeyHandler(VirtualKey.Enter, args =>
                            {
                                string entry = inputBox.Text.Trim();
                                if (string.IsNullOrEmpty(entry)) return;

                                inputBox.ClearHandlers(VirtualKey.Enter);

                                fadeOut.Completed += FadeOutOnCompleted;
                                fadeOut.Begin();

                                args.Handled = true;

                                void FadeOutOnCompleted(object sender2, object o1)
                                {
                                    fadeOut.Completed -= FadeOutOnCompleted;
									*/
                                    if (freebase2 != null)
                                    {
                                        var noteLocation = e.GetPosition(freebase2?.GetCanvas());
                                        freebase2.RenderPreviewTextbox(noteLocation, dragDoc, null, "");
                                    }
                                    else
                                    {
                                        //dragModel.LinkType = entry;
                                        (senderView as FrameworkElement).GetFirstAncestorOfType<DocumentView>().This_Drop(sender, e);
                                        //var note = new RichTextNote("<annotation>", where).Document;
                                        //note.SetField<BoolController>(KeyStore.AnnotationVisibilityKey, true, true);
                                        //dragDoc.Link(note, LinkContexts.None, entry);
                                        //AddDocument(note);
                                    }
							/*
                                    inputBox.Visibility = Visibility.Collapsed;
                                }
                            });

                            inputBox.Visibility = Visibility.Visible;
                            fadeIn.Begin();
                            inputBox.Focus(FocusState.Programmatic);
							
                            var adjustLat = false;
                            var adjustVert = false;

                            double overExtensionX = where.X + inputBox.ActualWidth - MainPage.Instance.xCanvas.ActualWidth;
                            if (overExtensionX > 0) adjustLat = true;

                            var adjustX = new DoubleAnimation
                            {
                                By = -1 * overExtensionX - 10,
                                EnableDependentAnimation = true,
                                Duration = new Duration(TimeSpan.FromMilliseconds(500))
                            };

                            _lateralAdjustment = new Storyboard();
                            _lateralAdjustment.Children.Add(adjustX);

                            Storyboard.SetTarget(adjustX, moveTransform);
                            Storyboard.SetTargetProperty(adjustX, "X");

                            ResourceDictionary resources = MainPage.Instance.xOuterGrid.Resources;
                            if (!resources.ContainsKey("lateralAdjustment")) resources.Add("lateralAdjustment", _lateralAdjustment);

                            double overExtensionY = where.Y + inputBox.ActualHeight - MainPage.Instance.xCanvas.ActualHeight;
                            if (overExtensionY > 0) adjustVert = true;

                            var adjustY = new DoubleAnimation
                            {
                                By = -1 * overExtensionY - 10,
                                EnableDependentAnimation = true,
                                Duration = new Duration(TimeSpan.FromMilliseconds(500))
                            };

                            _verticalAdjustment = new Storyboard();
                            _verticalAdjustment.Children.Add(adjustY);

                            Storyboard.SetTarget(adjustY, moveTransform);
                            Storyboard.SetTargetProperty(adjustY, "Y");

                            if (!resources.ContainsKey("verticalAdjustment")) resources.Add("verticalAdjustment", _verticalAdjustment);

                            if (adjustLat) _lateralAdjustment.Begin();
                            if (adjustVert) _verticalAdjustment.Begin();
							*/
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
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            HighlightPotentialDropTarget(sender as ICollectionView);

            e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;

            e.DragUIOverride.IsContentVisible = true;

            e.Handled = true;
        }
        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragOver(object sender, DragEventArgs e)
        {
            MainPage.Instance.DockManager.HighlightDock(e.GetPosition(MainPage.Instance.xMainDocView));
            HighlightPotentialDropTarget(sender as ICollectionView);

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
            RemoveDragDropIndication(sender as ICollectionView);
            e.Handled = true;
        }

        /// <summary>
        /// Highlight a collection when drag enters it to indicate which collection would the document move to if the user were to drop it now
        /// </summary>
        private void HighlightPotentialDropTarget(ICollectionView element)
        {
            // only one collection should be highlighted at a time
            _previousDragEntered?.SetDropIndicationFill(new SolidColorBrush(Colors.Transparent));
            element?.SetDropIndicationFill((SolidColorBrush)App.Instance.Resources["DragHighlight"]);
            _previousDragEntered = element;
        }

        /// <summary>
        /// Remove highlight from target drop collection and border from DocumentView being dragged
        /// </summary>
        /// <param name="element"></param>
        private void RemoveDragDropIndication(ICollectionView element)
        {
            element?.SetDropIndicationFill(new SolidColorBrush(Colors.Transparent));
            _previousDragEntered = null;
        }
        #endregion

    }
}