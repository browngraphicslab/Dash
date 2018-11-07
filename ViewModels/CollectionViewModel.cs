using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using Microsoft.Toolkit.Uwp.UI;
using static Dash.DataTransferTypeInfo;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    {
        private static ICollectionView _previousDragEntered;
        private bool _canDragItems = true;
        private double _cellFontSize = 9;
        public bool IsLoaded => _refCount > 0;
        private DocumentController _lastContainerDocument; // if the ContainerDocument changes, this stores the previous value which is used to cleanup listener references
        private SettingsView.WebpageLayoutMode WebpageLayoutMode => SettingsView.Instance.WebpageLayout;
        public ListController<DocumentController> CollectionController => ContainerDocument.GetDereferencedField<ListController<DocumentController>>(CollectionKey, null) ?? ContainerDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(CollectionKey, null);
        public InkController InkController => ContainerDocument.GetDataDocument().GetDereferencedField<InkController>(KeyStore.InkDataKey, null);

        public double CellFontSize
        {
            get => _cellFontSize;
            set
            {
                this.SetProperty<double>(ref _cellFontSize, value);
            }
        }
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
                return new TransformGroupData(trans, IsLoaded ? scale : new Point(1, 1));
            }
            set
            {
                if (ContainerDocument.GetField<PointController>(KeyStore.PanPositionKey)?.Data != value.Translate ||
                    ContainerDocument.GetField<PointController>(KeyStore.PanZoomKey)?.Data != value.ScaleAmount)
                {
                    ContainerDocument.SetField<PointController>(KeyStore.PanPositionKey, value.Translate, true);
                    ContainerDocument.SetField<PointController>(KeyStore.PanZoomKey, value.ScaleAmount, true);
                    if (MainPage.Instance.XDocumentDecorations.SelectedDocs.Any((sd) => DocumentViewModels.Contains(sd.ViewModel)))
                    {
                        MainPage.Instance.XDocumentDecorations.SetPositionAndSize(); // bcz: hack ... The Decorations should update automatically when the view zooms -- need a mechanism to bind/listen to view changing globally?
                    }
                }
            }
        }
        public DocumentController ContainerDocument { get; private set; }
        public KeyController CollectionKey { get; set; }
        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public AdvancedCollectionView BindableDocumentViewModels { get; set; }
        public CollectionViewType ViewType
        {
            get => Enum.Parse<CollectionViewType>(ContainerDocument.GetDereferencedField<TextController>(KeyStore.CollectionViewTypeKey, null)?.Data ?? CollectionViewType.Freeform.ToString());
            set => ContainerDocument.SetField<TextController>(KeyStore.CollectionViewTypeKey, value.ToString(), true);
        }
        public bool CanDragItems
        {
            get { return _canDragItems; }
            set { SetProperty(ref _canDragItems, value); }
        }

        public CollectionViewModel(DocumentController containerDocument, KeyController fieldKey, Context context = null)
        {
            BindableDocumentViewModels = new AdvancedCollectionView(DocumentViewModels, true) { Filter = o => true };

            SetCollectionRef(containerDocument, fieldKey);

            DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }

        ~CollectionViewModel()
        {
            //Debug.WriteLine("FINALIZING CollectionViewModel");
        }

        /// <summary>
        /// Sets the reference to the field that contains the documents to display.
        /// </summary>
        /// <param name="refToCollection"></param>
        /// <param name="context"></param>
        public void SetCollectionRef(DocumentController containerDocument, KeyController fieldKey)
        {
            var wasLoaded = IsLoaded;
            if (IsLoaded)
            {
                Loaded(false);
            }

            ContainerDocument = containerDocument;
            CollectionKey = fieldKey;
            if (wasLoaded)
            {
                Loaded(true);
            }
            if (!IsLoaded && !wasLoaded)
            {
                Loaded(true);
            }
            _lastContainerDocument = ContainerDocument;
        }
        /// <summary>
        /// pan/zooms the document so that all of its contents are visible.  
        /// This only applies of the CollectionViewType is Freeform/Standard, and the CollectionFitToParent field is true
        /// </summary>
        public void FitContents()
        {
            if (!LocalSqliteEndpoint.SuspendTimer &&
                ContainerDocument.GetFitToParent() && ViewType == CollectionViewType.Freeform)
            {
                var parSize = ContainerDocument.GetActualSize() ?? new Point();
                var r = DocumentViewModels.Aggregate(Rect.Empty, (rect, dvm) => { rect.Union(dvm.Bounds); return rect; });
                if (!r.IsEmpty && r.Width != 0 && r.Height != 0)
                {
                    var rect = new Rect(new Point(), new Point(parSize.X, parSize.Y));
                    var scaleWidth = r.Width / r.Height > rect.Width / rect.Height;
                    var scaleAmt = scaleWidth ? rect.Width / r.Width : rect.Height / r.Height;
                    var trans = new Point(-r.Left * scaleAmt, -r.Top * scaleAmt);
                    if (scaleAmt > 0)
                    {
                        TransformGroup = new TransformGroupData(trans, new Point(scaleAmt, scaleAmt));
                    }
                }
            }
        }

        private int _refCount = 0;
        public void Loaded(bool isLoaded)
        {
            bool wasLoaded = IsLoaded;
            _refCount += isLoaded ? 1 : -1;
            if (IsLoaded && !wasLoaded)
            {
                var theDoc = ContainerDocument;
                var collectionField = theDoc.GetField<ListController<DocumentController>>(CollectionKey);
                if (collectionField == null)
                    theDoc = ContainerDocument.GetDataDocument();
                theDoc.AddFieldUpdatedListener(CollectionKey, collectionFieldChanged);
                ContainerDocument.AddFieldUpdatedListener(KeyStore.PanPositionKey, PanZoomFieldChanged);
                ContainerDocument.AddFieldUpdatedListener(KeyStore.PanZoomKey, PanZoomFieldChanged);
                ContainerDocument.AddFieldUpdatedListener(KeyStore.ActualSizeKey, ActualSizeFieldChanged);
                // force the view to refresh now that everything is loaded.  These changed handlers will cause the
                // TransformGroup to be re-read by thew View and will force FitToContents if necessary.
                PanZoomFieldChanged(null, null); // bcz: setting the TransformGroup scale before this view is loaded causes a hard crash at times.
                //Stuff may have changed in the collection while we weren't listening, so remake the list
                if (CollectionController != null)
                {
                    var curDocs = DocumentViewModels.Select((dvm, i) => new {Doc = dvm.DocumentController, Index = i}).ToList();
                    var colDocs = CollectionController.Select((doc, i) => new {Doc = doc, Index = i}).ToList();

                    var newDocs = colDocs.Where(dc => !curDocs.Any(doc => doc.Doc.Equals(dc.Doc))).ToList();
                    var deletedDocs = curDocs.Where(dc => !colDocs.Any(doc => doc.Doc.Equals(dc.Doc))).ToList();
                    foreach (var deletedDoc in deletedDocs)
                    {
                        RemoveViewModels(new List<DocumentController>{deletedDoc.Doc}, deletedDoc.Index);
                    }
                    foreach (var newDoc in newDocs)
                    {
                        AddViewModels(new List<DocumentController>{newDoc.Doc}, newDoc.Index);
                    }
                }
                ActualSizeFieldChanged(null, null);

                _lastContainerDocument = ContainerDocument;
            }
            else if(!IsLoaded && wasLoaded)
            {
                _lastContainerDocument.RemoveFieldUpdatedListener(KeyStore.PanPositionKey, PanZoomFieldChanged);
                _lastContainerDocument.RemoveFieldUpdatedListener(KeyStore.PanZoomKey, PanZoomFieldChanged);
                _lastContainerDocument.RemoveFieldUpdatedListener(KeyStore.ActualSizeKey, ActualSizeFieldChanged);
                _lastContainerDocument.RemoveFieldUpdatedListener(CollectionKey, collectionFieldChanged);
            }
        }

        private void PanZoomFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            OnPropertyChanged(nameof(TransformGroup));
        }
        private void ActualSizeFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (!MainPage.Instance.IsShiftPressed())
                FitContents();   // pan/zoom collection so all of its contents are visible
        }
        private void collectionFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
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

        private void updateViewModels(ListController<DocumentController>.ListFieldUpdatedEventArgs args)
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
                AddViewModels(args.NewItems, args.StartingChangeIndex);
                break;
            case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                DocumentViewModels.Clear();
                break;
            case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                RemoveViewModels(args.OldItems, args.StartingChangeIndex);
                break;
            case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
                DocumentViewModels.Clear();
                AddViewModels(args.NewItems, 0);
                break;
            }
        }

        private void AddViewModels(List<DocumentController> documents, int startIndex)
        {
            using (BindableDocumentViewModels.DeferRefresh())
            {
                foreach (var documentController in documents)
                {
                    if (startIndex >= DocumentViewModels.Count)
                        DocumentViewModels.Add(new DocumentViewModel(documentController));
                    else DocumentViewModels.Insert(startIndex, new DocumentViewModel(documentController));
                    startIndex++;
                }
            }
        }

        private void RemoveViewModels(List<DocumentController> documents, int startIndex)
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
            if (documents == null) return;

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
            var curLayout = ContainerDocument;
            var newLayout = newDoc;
            if (newLayout.DocumentType.Equals(CollectionBox.DocumentType) && curLayout.GetDataDocument().Equals(newLayout.GetDataDocument()))
                return true;
            if (newLayout.DocumentType.Equals(CollectionBox.DocumentType))
            {
                var newDocList = newLayout.GetDereferencedField(KeyStore.DataKey, null) as ListController<DocumentController>;
                foreach (var subDoc in newDocList.TypedData)
                {
                    var subLayout = subDoc;
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

        public bool CreatesCycle(List<DocumentController> docs)
        {
            return docs.Where(CreatesCycle).Any();
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
                if (!CreatesCycle(doc))
                {
                    doc.CaptureNeighboringContext();

                    var theDoc = ContainerDocument;
                    var collectionField = theDoc.GetField<ListController<DocumentController>>(CollectionKey);
                    if (collectionField == null)
                        theDoc = ContainerDocument.GetDataDocument();
                    theDoc.AddToListField(CollectionKey, doc);
                }
            }
        }

        public void InsertDocument(DocumentController doc, int i)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (!CreatesCycle(doc))
                {
                    doc.CaptureNeighboringContext();

                    var theDoc = ContainerDocument;
                    var collectionField = theDoc.GetField<ListController<DocumentController>>(CollectionKey);
                    if (collectionField == null)
                        theDoc = ContainerDocument.GetDataDocument();
                    theDoc.AddToListField(CollectionKey, doc, i);
                }
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
                var theDoc = ContainerDocument;
                var collectionField = theDoc.GetField<ListController<DocumentController>>(CollectionKey);
                if (collectionField == null)
                    theDoc = ContainerDocument.GetDataDocument();
                
                theDoc.RemoveFromListField(CollectionKey, document);
                if (document.IsMovingCollections)
                {
                    document.IsMovingCollections = false;
                }
                else
                {
                    var pres = MainPage.Instance.xPresentationView;
                    if (pres.ViewModel != null && pres.ViewModel.PinnedNodes.Contains(document))
                        pres.FullPinDelete(document);
                }
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
                switch (fieldData)
                {
                case ListController<DocumentController> docListField:
                    foreach (var dd in docListField)
                    {
                        var dataDoc = dd.GetDataDocument();

                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
                        expandedDoc.SetField(showField, dataDoc, true);
                        subDocs.Add(expandedDoc);
                    }

                    break;
                //TODO tfs: why do these next to cases copy the Text/Number Controller?
                case ListController<TextController> textListField:
                    foreach (var dd in textListField)
                    {
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
                        expandedDoc.SetField(showField, new TextController(dd.Data), true);
                        subDocs.Add(expandedDoc);
                    }

                    break;
                case ListController<NumberController> numListField:
                    foreach (var dd in numListField)
                    {
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(), true);
                        expandedDoc.SetField(showField, new NumberController(dd.Data), true);
                        subDocs.Add(expandedDoc);
                    }

                    break;
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
                            var postitNote = new RichTextNote(text, size: new Size(300, double.NaN)).Document;
                            Actions.DisplayDocument(this, postitNote, where);
                            return postitNote;
                        }
                    }
                }
                else if (dvp.Contains(StandardDataFormats.Html) )
                {
                    var text = await dvp.GetHtmlFormatAsync();
                    var layoutMode = await MainPage.Instance.GetLayoutType();

                    if ((layoutMode == SettingsView.WebpageLayoutMode.HTML && !MainPage.Instance.IsCtrlPressed()) ||
                        (layoutMode == SettingsView.WebpageLayoutMode.RTF && MainPage.Instance.IsCtrlPressed()))
                    {
                        var htmlNote = new HtmlNote(text, "<unknown html>", where).Document;
                        Actions.DisplayDocument(this, htmlNote, where);
                        return htmlNote;
                    } 
                    else
                    {
                        var htmlNote = await HtmlToDashUtil.CreateRtfNote(where, "<unknown html>", text);
                        Actions.DisplayDocument(this, htmlNote, where);
                        return htmlNote;
                    }
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
                                foreach (var str in html.Split(new[] { '\r' }))
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
                                postitNote.GetDataDocument().AddToRegions(new List<DocumentController> { region });

                                region.SetRegionDefinition(postitNote);
                                region.SetAnnotationType(AnnotationType.Selection);

                                region.Link(sourceDoc, LinkBehavior.Annotate);

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
                var fromFileSystem = e.DataView.Contains(StandardDataFormats.StorageItems);

                var dragModel        = e.DataView.GetDragModel();
                var dragDocModel     = dragModel as DragDocumentModel;
                var joinDragModel    = e.DataView.GetJoinDragModel();
                var internalMove     = !MainPage.Instance.IsShiftPressed() && !MainPage.Instance.IsAltPressed() && !MainPage.Instance.IsCtrlPressed() && !fromFileSystem;
                var isLinking        = e.AllowedOperations.HasFlag(DataPackageOperation.Link) && internalMove && dragDocModel?.DraggingLinkButton == true;
                var isMoving         = e.AllowedOperations.HasFlag(DataPackageOperation.Move) && internalMove && dragDocModel?.DraggingLinkButton != true;
                var isCopying        = e.AllowedOperations.HasFlag(DataPackageOperation.Copy) && (fromFileSystem || MainPage.Instance.IsShiftPressed());
                var isSettingContext = MainPage.Instance.IsAltPressed() && !fromFileSystem;

                e.AcceptedOperation = isSettingContext ? DataPackageOperation.None : 
                                      isLinking        ? DataPackageOperation.Link :  
                                      isMoving         ? DataPackageOperation.Move :
                                      isCopying        ? DataPackageOperation.Copy : 
                                      DataPackageOperation.None;

                RemoveDragDropIndication(sender as CollectionView);
                
                var where = new Point();
                if ((sender as CollectionView)?.CurrentView is CollectionFreeformBase freeformBase)
                {
                    where = Util.GetCollectionFreeFormPoint(freeformBase, e.GetPosition(MainPage.Instance.xOuterGrid));
                }
                else if (DocumentViewModels.LastOrDefault() is DocumentViewModel last)
                {
                    where = new Point(last.Position.X + DocumentViewModels.Last().ActualSize.X, last.Position.Y);
                }
                
                if (isSettingContext && dragDocModel != null && dragDocModel.DraggedDocCollectionViews?.FirstOrDefault() != this &&
                    (sender as FrameworkElement).GetFirstAncestorOfType<CollectionView>() != null) // bcz: hack -- dropping a KeyValuepane will set the datacontext of the collection
                {
                    ContainerDocument.SetField(KeyStore.DocumentContextKey, dragDocModel.DraggedDocuments.First().GetDataDocument().GetDataDocument(), true);
                } 
                else
                {
                    if (joinDragModel != null)
                    {
                        var newDoc = joinDragModel.CollectionDocument.GetViewCopy(where);
                        newDoc.SetField(KeyController.Get("DBChartField"), joinDragModel.DraggedKey, true);
                        newDoc.SetField<TextController>(KeyStore.CollectionViewTypeKey, CollectionViewType.DB.ToString(), true);
                        AddDocument(newDoc);
                    }
                    var docsToAdd = await e.DataView.GetDroppableDocumentsForDataOfType(Any, sender as FrameworkElement, where);
                    AddDocuments(await AddDroppedDocuments(sender, docsToAdd, dragModel, isMoving, this));
                }
                e.DataView.ReportOperationCompleted(e.AcceptedOperation);
            }
        }
        
        public static async Task<List<DocumentController>> AddDroppedDocuments(object sender, List<DocumentController> docsToAdd, DragModelBase dragModel, bool isMoving, CollectionViewModel collectionViewModel)
        {
            if (isMoving && dragModel is DragDocumentModel dragDocModel)
            {
                for (var i = 0; i < dragDocModel.DraggedDocCollectionViews?.Count; i++)
                {
                    if (dragDocModel.DraggedDocCollectionViews[i] == collectionViewModel)

                    {
                        docsToAdd.Remove(dragDocModel.DraggedDocuments[i]);
                        if (dragDocModel.DraggedDocumentViews?[i] != null)
                        {
                            dragDocModel.DraggedDocumentViews[i].Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (dragDocModel.DraggedDocumentViews != null)
                        {
                            MainPage.Instance.ClearFloaty(dragDocModel.DraggedDocumentViews[i]);
                        }

                        if (dragDocModel.DraggedDocCollectionViews[i] == null)
                        {
                            var overlay = dragDocModel.DraggedDocumentViews[i].GetFirstAncestorOfType<AnnotationOverlay>();
                            overlay?.EmbeddedDocsList.Remove(dragDocModel.DraggedDocuments[i]);
                        }
                        else 
                        {
                            dragDocModel.DraggedDocCollectionViews[i].RemoveDocument(dragDocModel.DraggedDocuments[i]);
                        }
                    }
                }
            }
            for (int i = 0; i < docsToAdd.Count; i++)
            {
                if (collectionViewModel?.ViewType == CollectionViewType.Freeform && !docsToAdd[i].DocumentType.Equals(RichTextBox.DocumentType))
                {
                    if (docsToAdd[i].GetHorizontalAlignment() == HorizontalAlignment.Stretch)
                        docsToAdd[i].SetHorizontalAlignment(HorizontalAlignment.Left);
                    if (docsToAdd[i].GetVerticalAlignment() == VerticalAlignment.Stretch)
                        docsToAdd[i].SetVerticalAlignment(VerticalAlignment.Top);
                    if (double.IsNaN(docsToAdd[i].GetWidth()) && !docsToAdd[i].DocumentType.Equals(WebBox.DocumentType))
                        docsToAdd[i].SetWidth(300);
                    if (docsToAdd[i].DocumentType.Equals(CollectionBox.DocumentType))
                        docsToAdd[i].SetFitToParent(true);
                }
            }

            return docsToAdd;
        }

        public static void ConvertToTemplate(DocumentController templateRoot, DocumentController collectionToConvert)
        {
            var children = collectionToConvert.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey,null).TypedData;
            var nestedCollections = children.Where((c) => c.DocumentType.Equals(CollectionBox.DocumentType));
            foreach (var nc in nestedCollections)
            {
                ConvertToTemplate(templateRoot, nc);
            }
            RouteDataBoxReferencesThroughCollection(templateRoot, children);
        }

        public static void RouteDataBoxReferencesThroughCollection(DocumentController cpar, List<DocumentController> docsToAdd)
        {
            var databoxes = docsToAdd.Where((ad) => ad.DocumentType.Equals(DataBox.DocumentType)).ToList();
            if (databoxes.Count > 0)
            {
                cpar.SetField(KeyStore.DataKey, cpar.GetDereferencedField(KeyStore.DataKey, null), true); // move the layout data to the collection's layout document.
            }
            foreach (var dataBox in databoxes)
            {
                var dataBoxSourceDoc     = dataBox.GetDataDocument();
                var dataBoxDataReference = dataBox.GetField<ReferenceController>(KeyStore.DataKey);
                if (dataBoxSourceDoc != null && dataBoxDataReference != null)
                {
                    var fieldKey = dataBoxDataReference.FieldKey;
                    cpar.SetField(KeyStore.DocumentContextKey, dataBoxSourceDoc, true);
                    dataBox.SetField(KeyStore.DataKey, new PointerReferenceController(new DocumentReferenceController(cpar, KeyStore.DocumentContextKey), fieldKey), true);
                }
            }
        }

        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            HighlightPotentialDropTarget(sender as CollectionView);

            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.IsGlyphVisible = false;
                e.DragUIOverride.IsCaptionVisible = false;

                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None
                    ? DataPackageOperation.Copy
                    : e.DataView.RequestedOperation;

                e.DragUIOverride.IsContentVisible = true;
            }

            e.Handled = true;
        }
        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragOver(object sender, DragEventArgs e)
        {
            HighlightPotentialDropTarget(sender as CollectionView);

            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.IsGlyphVisible = false;
                e.DragUIOverride.IsCaptionVisible = false;
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None
                    ? DataPackageOperation.Copy
                    : e.DataView.RequestedOperation;

                if (e.DataView.HasDataOfType(Internal) &&
                    !e.DataView.HasDroppableDragModels(sender as FrameworkElement))
                    e.AcceptedOperation = DataPackageOperation.None;

                e.DragUIOverride.IsContentVisible = true;
            }
        }

        /// <summary>
        /// Fired by a collection when the item being dragged is no longer over it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            RemoveDragDropIndication(sender as CollectionView);
            e.Handled = true;
        }

        /// <summary>
        /// Highlight a collection when drag enters it to indicate which collection would the document move to if the user were to drop it now
        /// </summary>
        private void HighlightPotentialDropTarget(CollectionView element)
        {
            // only one collection should be highlighted at a time
            _previousDragEntered?.SetDropIndicationFill(new SolidColorBrush(Colors.Transparent));
            element.CurrentView?.SetDropIndicationFill((SolidColorBrush)App.Instance.Resources["DragHighlight"]);
            _previousDragEntered = element.CurrentView;
        }

        /// <summary>
        /// Remove highlight from target drop collection and border from DocumentView being dragged
        /// </summary>
        /// <param name="element"></param>
        private void RemoveDragDropIndication(CollectionView element)
        {
            element.CurrentView?.SetDropIndicationFill(new SolidColorBrush(Colors.Transparent));
            _previousDragEntered = null;
        }
        #endregion
    }
}
