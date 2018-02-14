﻿using Dash.Controllers.Operators;
using DashShared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.UI;
using static Dash.NoteDocuments;
using Windows.System;
using Windows.UI.Core;
using Dash.Models.DragModels;

namespace Dash
{
    public abstract class BaseCollectionViewModel : ViewModelBase, ICollectionViewModel
    {
        private bool _canDragItems;
        private double _cellSize;
        private ListViewSelectionMode _itemSelectionMode;
        private static SelectionElement _previousDragEntered;

        public virtual KeyController CollectionKey => KeyStore.CollectionKey;
        public KeyController OutputKey { get; set;}

        protected BaseCollectionViewModel() : base()
        {
            SelectionGroup = new List<DocumentViewModel>();
            //BindableDocumentViewModels = new AdvancedCollectionView(DocumentViewModels, true);
            BindableDocumentViewModels = new AdvancedCollectionView(DocumentViewModels, true) {Filter = o => true};
            //BindableDocumentViewModels = new AdvancedCollectionView(new List<DocumentViewModel>());
        }
        

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();
        public ObservableCollection<DocumentViewModel> ThumbDocumentViewModels { get; set; } = new ObservableCollection<DocumentViewModel>();

        public AdvancedCollectionView BindableDocumentViewModels { get; set; }

        // used to keep track of groups of the currently selected items in a collection
        public List<DocumentViewModel> SelectionGroup { get; set; }

        public abstract void AddDocuments(List<DocumentController> documents, Context context);
        public abstract void AddDocument(DocumentController document, Context context);
        public abstract void RemoveDocuments(List<DocumentController> documents);
        public abstract void RemoveDocument(DocumentController document);

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

        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }

        #endregion


        #region DragAndDrop
        

        List<DocumentController> pivot(List<DocumentController> docs, KeyController pivotKey)
        {
            var dictionary      = new Dictionary<object, Dictionary<KeyController, List<object>>>();
            var pivotDictionary = new Dictionary<object, DocumentController>();

            foreach (var d in docs.Select((dd) => dd.GetDataDocument(null)))
            {
                var fieldDict = setupPivotDoc(pivotKey, dictionary, pivotDictionary, d);
                if (fieldDict == null)
                    continue;
                foreach (var f in d.EnumFields())
                    if (!f.Key.Equals(pivotKey) && !f.Key.IsUnrenderedKey())
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
                    if (doc.GetField(f.Key) == null) {
                        var items = new List<FieldControllerBase>();
                        foreach (var i in f.Value)
                        {
                            if (i is string)
                                items.Add(new TextController(i as string));
                            else if (i is double)
                                items.Add(new NumberController((double) i));
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
                            else if (items.First() is RichTextController  )
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
            var obj = d.GetDataDocument(null).GetDereferencedField(pivotKey, null)?.GetValue(null);
            DocumentController pivotDoc = null;
            if (obj != null && !dictionary.ContainsKey(obj))
            {
                var pivotField = d.GetDataDocument(null).GetField(pivotKey);
                pivotDoc = (pivotField as ReferenceController)?.GetDocumentController(null);
                if (pivotDoc == null || pivotDoc.DocumentType.Equals(DashConstants.TypeStore.OperatorType))
                {
                    pivotDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>() {
                        [KeyStore.PrimaryKeyKey] = new ListController<KeyController>(pivotKey)
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
                    DBTest.DBDoc.AddChild(pivotDoc);
                    d.SetField(pivotKey, new DocumentReferenceController(pivotDoc.GetId(), pivotKey), true);
                }
                pivotDictionary.Add(obj, pivotDoc);
                dictionary.Add(obj, new Dictionary<KeyController, List<object>>());
            }

            if (obj != null)
            {
                d.SetField(pivotKey, new DocumentReferenceController(pivotDictionary[obj].GetId(), pivotKey), true);
                return dictionary[obj];
            }
            return null;
        }
        
        KeyController expandCollection(KeyController fieldKey, FieldControllerBase getDocs, List<DocumentController> subDocs, KeyController showField)
        {
            foreach (var d in (getDocs as ListController<DocumentController>).TypedData)
            {
                var fieldData = d.GetDataDocument(null).GetDereferencedField(fieldKey, null);
                if (fieldData is ListController<DocumentController>)
                    foreach (var dd in (fieldData as ListController<DocumentController>).TypedData)
                    {
                        var dataDoc = dd.GetDataDocument(null);
                        
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(null), true);
                        expandedDoc.SetField(showField, dataDoc, true);
                        subDocs.Add(expandedDoc);
                    }
                else if (fieldData is ListController<TextController>)
                    foreach (var dd in (fieldData as ListController<TextController>).Data)
                    {
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(null), true);
                        expandedDoc.SetField(showField, new TextController((dd as TextController).Data), true);
                        subDocs.Add(expandedDoc);
                    }
                else if (fieldData is ListController<NumberController>)
                    foreach (var dd in (fieldData as ListController<NumberController>).Data)
                    {
                        var expandedDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), DocumentType.DefaultType);
                        expandedDoc.SetField(KeyStore.HeaderKey, d.GetDataDocument(null), true);
                        expandedDoc.SetField(showField, new NumberController((dd as NumberController).Data), true);
                        subDocs.Add(expandedDoc);
                    }
            }

            return showField;
        }

        public async void Paste(DataPackageView dvp, Point where)
        {
            if (dvp.Contains(StandardDataFormats.StorageItems))
            {
                FileDropHelper.HandleDrop(dvp, where, this);
            }
            else if (dvp.Contains(StandardDataFormats.Bitmap))
            {
                PasteBitmap(dvp, where);
            }
            else if (dvp.Contains(StandardDataFormats.Text))
            {
                var text = await dvp.GetTextAsync();
                if (text != "")
                {
                    var postitNote = new RichTextNote(PostitNote.DocumentType, text: text, size: new Size(400, 40)).Document;
                    Actions.DisplayDocument(this, postitNote, where);
                }
            }
        }

        private async void PasteBitmap(DataPackageView dvp, Point where)
        {
            var streamRef = await dvp.GetBitmapAsync();
            WriteableBitmap writeableBitmap = new WriteableBitmap(400, 400);
            await writeableBitmap.SetSourceAsync(await streamRef.OpenReadAsync());

            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile savefile = await storageFolder.CreateFileAsync("paste.jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            // Save the image file with jpg extension 
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)writeableBitmap.PixelWidth, (uint)writeableBitmap.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();
            var dp = new DataPackage();
            dp.SetStorageItems(new IStorageItem[] { savefile });
            FileDropHelper.HandleDrop(dp.GetView(), where, this);
        }

        /// <summary>
        /// Fired by a collection when an item is dropped on it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            
            // accept move, then copy, and finally accept whatever they requested (for now)
            if (e.AllowedOperations.HasFlag(DataPackageOperation.Move))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
            else if (e.AllowedOperations.HasFlag(DataPackageOperation.Copy))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else
            {
                e.AcceptedOperation = e.DataView.RequestedOperation;
            }

            var where = sender is CollectionFreeformView ?
                Util.GetCollectionFreeFormPoint((sender as CollectionFreeformView), e.GetPosition(MainPage.Instance)) :
                new Point();

            if (e.DataView != null && e.DataView.Properties.ContainsKey(nameof(DragCollectionFieldModel)))
            {
                var dragData  = (DragCollectionFieldModel)e.DataView.Properties[nameof(DragCollectionFieldModel)];
                var getDocs   = dragData.CollectionReference.DereferenceToRoot(null);
                var subDocs   = new List<DocumentController>();
                var showField = dragData.FieldKey;
                var cnote     = new CollectionNote(where, dragData.ViewType);

                if ((getDocs as ListController<DocumentController>).Data.Any())
                {
                    var firstDocValue = (getDocs as ListController<DocumentController>).TypedData.First().GetDataDocument(null).GetDereferencedField(showField, null);
                    if (firstDocValue is ListController<DocumentController> || firstDocValue?.GetValue(null) is List<FieldControllerBase>)
                        showField = expandCollection(dragData.FieldKey, getDocs, subDocs, showField);
                    else if (firstDocValue is DocumentController)
                        subDocs = (getDocs as ListController<DocumentController>).TypedData.Select((d) => d.GetDataDocument(null).GetDereferencedField<DocumentController>(showField, null)).ToList();
                    else subDocs = pivot((getDocs as ListController<DocumentController>).TypedData, showField);
                }
                if (subDocs != null)
                    cnote.Document.GetDataDocument(null).SetField(KeyStore.CollectionKey, new ListController<DocumentController>(subDocs), true);
                else cnote.Document.GetDataDocument(null).SetField(KeyStore.CollectionKey, dragData.CollectionReference, true);
                cnote.Document.SetField(CollectionDBView.FilterFieldKey, showField, true);

                AddDocument(cnote.Document, null);
                return;
            }

            // first check for things we don't want to allow dropped onto the collection
            //restore previous conditions 
            if (DocumentView.DragDocumentView != null)
                DocumentView.DragDocumentView.IsHitTestVisible = true;
            this.RemoveDragDropIndication(sender as SelectionElement);

            // true if dragged from key value pane in interfacebuilder
            var isDraggedFromKeyValuePane = e.DataView?.Properties.ContainsKey(KeyValuePane.DragPropertyKey) ?? false;
            
            if (isDraggedFromKeyValuePane) return; // in both these cases we don't want the collection to intercept the event

            //return if it's an operator dragged from compoundoperatoreditor listview 
            if (e.Data?.Properties[CompoundOperatorController.OperationBarDragKey] != null) return;

            // from now on we are handling this event!
            e.Handled = true;

            // if we are dragging and dropping from the radial menu
            // if we drag from radial menu
            var sourceIsRadialMenu = e.DataView?.Properties.ContainsKey(RadialMenuView.RadialMenuDropKey) ?? false;
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
                    FileDropHelper.HandleDropOnCollectionAsync(sender, e, this);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            }
            else if (e.DataView.Contains(StandardDataFormats.Html))
            {
                var html = await e.DataView.GetHtmlFormatAsync();

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
                var imgs = splits.Where((s) => new Regex("img.*src=\"[^>\"]*").Match(s).Length >0);
                var text = e.DataView.Contains(StandardDataFormats.Text) ? (await e.DataView.GetTextAsync()).Trim() : "";
                var strings = text.Split(new char[] { '\r' });
                var htmlNote = new HtmlNote(html, BrowserView.Current?.Title ?? "", where: where).Document;
                foreach (var str in html.Split(new char[] { '\r' }))
                {
                    var matches = new Regex("^SourceURL:.*").Matches(str.Trim());
                    if (matches.Count != 0)
                    {
                        htmlNote.GetDataDocument(null).SetField(KeyStore.SourecUriKey, new TextController(matches[0].Value.Replace("SourceURL:", "")), true);
                        break;
                    }
                }

                if (imgs.Count() == 0)
                {
                    var matches = new Regex(".{1,100}:.*").Matches(text.Trim());
                    var title = (matches.Count == 1 && matches[0].Value == text) ? new Regex(":").Split(matches[0].Value)[0] : "";
                    htmlNote.GetDataDocument(null).SetField(KeyStore.DocumentTextKey, new TextController(text), true);
                    if (title == "")
                        foreach (var match in matches)
                        {
                            var pair = new Regex(":").Split(match.ToString());
                            htmlNote.GetDataDocument(null).SetField(new KeyController(pair[0], pair[0]), new TextController(pair[1].Trim()), true);
                        }
                    else
                        htmlNote.SetField(KeyStore.TitleKey, new TextController(title), true);
                } else {
                    var related = new List<DocumentController>();
                    foreach (var img in imgs)
                    {
                        var srcMatch = new Regex("[^-]src=\"[^{>?}\"]*").Match(img.ToString()).Value;
                        var src = srcMatch.Substring(6, srcMatch.Length - 6);
                        var i = new AnnotatedImage(new Uri(src), "", 100, double.NaN, where.X, where.Y);
                        related.Add(i.Document);
                    }
                    var cnote = new CollectionNote(new Point(), CollectionView.CollectionViewType.Page, collectedDocuments: related).Document;
                    htmlNote.GetDataDocument(null).SetField(new KeyController("Html Images", "Html Images"), cnote, true);//
                    //htmlNote.GetDataDocument(null).SetField(new KeyController("Html Images", "Html Images"), new ListController<DocumentController>(related), true);
                    htmlNote.GetDataDocument(null).SetField(KeyStore.DocumentTextKey, new TextController(text), true);
                    foreach (var str in strings)
                    {
                        var matches = new Regex("^.{1,100}:.*").Matches(str.Trim());
                        if (matches.Count != 0)
                        {
                            foreach (var match in matches)
                            {
                                var pair = new Regex(":").Split(match.ToString());
                                htmlNote.GetDataDocument(null).SetField(new KeyController(pair[0], pair[0]), new TextController(pair[1].Trim()), true);
                            }
                        }
                    }
                }
                AddDocument(htmlNote, null);
            }
            else if (e.DataView.Contains(StandardDataFormats.Rtf))
            {
                var text = await e.DataView.GetRtfAsync();

                var t = new RichTextNote(PostitNote.DocumentType);
                t.Document.GetDataDocument(null).SetField(RichTextNote.RTFieldKey, new RichTextController(new RichTextModel.RTD(text, text)), true);
                AddDocument(t.Document, null);
            }
            else if (e.DataView.Contains(StandardDataFormats.Text))
            {
                var text = await e.DataView.GetTextAsync();
                var t = new RichTextNote(PostitNote.DocumentType);
                t.Document.GetDataDocument(null).SetField(RichTextNote.RTFieldKey, new RichTextController(new RichTextModel.RTD(text)), true);
                var matches = new Regex(".*:.*").Matches(text);
                foreach (var match in matches)
                {
                    var pair = new Regex(":").Split(match.ToString());
                    t.Document.GetDataDocument(null).SetField(new KeyController(pair[0], pair[0]), new TextController(pair[1].Trim('\r')), true);
                }
                AddDocument(t.Document, null);
            }
            else if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                var bmp = await e.DataView.GetBitmapAsync();
                IRandomAccessStreamWithContentType streamWithContent = await bmp.OpenReadAsync();
                byte[] buffer = new byte[streamWithContent.Size];
                using (DataReader reader = new DataReader(streamWithContent))
                {
                    await reader.LoadAsync((uint)streamWithContent.Size);
                    reader.ReadBytes(buffer);
                }
                var t = new AnnotatedImage(null, Convert.ToBase64String(buffer), "", "");
                AddDocument(t.Document, null);
            }

            // TODO remove this and all references to TreeMenuNode
            if (e.DataView != null && e.DataView.Properties.ContainsKey(TreeMenuNode.TreeNodeDragKey))
            {
                var draggedLayout = e.DataView.Properties[TreeMenuNode.TreeNodeDragKey] as DocumentController;
                AddDocument(draggedLayout.GetViewCopy(where), null);
            }

            if (e.DataView != null && e.DataView.Properties.ContainsKey("DocumentControllerList"))
            {
                var collectionViewModel = e.DataView.Properties.ContainsKey(nameof(BaseCollectionViewModel)) == true ?
                          e.DataView.Properties[nameof(BaseCollectionViewModel)] as BaseCollectionViewModel : null;

                var items = e.DataView.Properties.ContainsKey("DocumentControllerList") == true ?
                          e.DataView.Properties["DocumentControllerList"] as List<DocumentController> : null;

                var width = e.DataView.Properties.ContainsKey("Width") == true ? (double)e.DataView.Properties["Width"] : double.NaN;
                var height = e.DataView.Properties.ContainsKey("Height") == true ? (double)e.DataView.Properties["Height"] : double.NaN;

                var payloadLayoutDelegates = items.Select((p) =>
                {
                    if (p.GetActiveLayout() == null && p.GetDereferencedField(KeyStore.DocumentContextKey, null) == null)
                        p.SetActiveLayout(new DefaultLayout().Document, true, true);
                    var newDoc = e.DataView.Properties.ContainsKey("View") ? p.GetViewCopy(where) :
                                                                     e.AcceptedOperation == DataPackageOperation.Move ? p.GetSameCopy(where) :
                                                                     e.AcceptedOperation == DataPackageOperation.Link ? p.GetKeyValueAlias(where) : p.GetCopy(where);
                    if (double.IsNaN(newDoc.GetWidthField().Data))
                        newDoc.SetField(KeyStore.WidthFieldKey, new NumberController(width), true);
                    if (double.IsNaN(newDoc.GetHeightField().Data))
                        newDoc.SetField(KeyStore.HeightFieldKey, new NumberController(height), true);
                    if (e.DataView.Properties.ContainsKey("SelectedText"))
                    {
                        var col = newDoc.GetDataDocument(null)?.GetDereferencedField<ListController<DocumentController>>(KeyStore.CollectionKey, null)?.Data;

                    }
                    return newDoc;
                });
                AddDocuments(payloadLayoutDelegates.ToList(), null);
                if (collectionViewModel == this && e.AcceptedOperation == DataPackageOperation.Move)
                {
                   e.AcceptedOperation = DataPackageOperation.Link; // if the item stayed in the same container, treat it as link, not a move (a move will remove the source object in DragCompleted)
                }
            }


            // if the user drags the entire collection of documents from the search bar
            if (e.DataView != null && e.DataView.Properties.ContainsKey(MainSearchBox.SearchCollectionDragKey))
            {
                // the drag contains an IEnumberable of view documents, we add it as a collection note displayed as a grid
                var models = (e.DataView.Properties[MainSearchBox.SearchCollectionDragKey] as IEnumerable<SearchResultViewModel>);
                var parentDocs = (sender as FrameworkElement)?.GetAncestorsOfType<CollectionView>().Select((cv) => cv.ParentDocument?.ViewModel?.DataDocument);
                var docs = models.Select((srvm) => srvm.ViewDocument).Where((d) => !parentDocs.Contains(d.GetDataDocument(null)) && d?.DocumentType?.Equals(DashConstants.TypeStore.MainDocumentType) == false);
                var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Grid, collectedDocuments: docs.Select(doc => doc.GetViewCopy()).ToList());
                AddDocument(cnote.Document, null);
            }

            // if the user drags a single document from the search bar
            if (e.DataView != null && e.DataView.Properties.ContainsKey(MainSearchBox.SearchResultDragKey))
            {
                // the drag contains the view document which we just display an alias of
                var doc = e.DataView.Properties[MainSearchBox.SearchResultDragKey] as DocumentController;
                var docAlias = doc.GetViewCopy(where);
                AddDocument(docAlias, null);
            }

            // if the user drags a data document
            if (e.DataView != null && e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                var dragModel = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];

                if (dragModel.CanDrop(sender as FrameworkElement))
                {
                    var draggedDocument = dragModel.GetDraggedDocument();
                    if (draggedDocument.DocumentType.Equals(DashConstants.TypeStore.CollectionBoxType) &&
                        (sender as DependencyObject).GetFirstAncestorOfType<DocumentView>()?.ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) == false &&
                        this.DocumentViewModels.Where((dvm) => dvm.DocumentController.Equals(draggedDocument)).Count() == 0)
                    {
                        HandleTemplateLayoutDrop(dragModel);
                        e.Handled = true;
                        return;
                    }
                    else
                        AddDocument(dragModel.GetDropDocument(where), null);
                }
            }
            
            e.Handled = true;
        }

        /// <summary>
        /// If you drop a collection, the collection serves as a layout template for all the documents in the collection and changes the way they are displayed.
        /// TODO: This needs an explicit GUI drop target for setting a template...
        /// </summary>
        /// <param name="dragModel"></param>
        private void HandleTemplateLayoutDrop(DragDocumentModel dragModel)
        {
            var template = dragModel.GetDraggedDocument();
            var templateFields = template.GetDataDocument(null).GetDereferencedField<ListController<DocumentController>>(KeyStore.CollectionKey, null)?.TypedData;
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
                dvm.OnActiveLayoutChanged(new Context(dvm.LayoutDocument));
            }
        }

        /// <summary>
        /// Fired by a collection when an item is dragged over it
        /// </summary>
        public void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CollectionViewOnDragEnter Base");
            this.HighlightPotentialDropTarget(sender as SelectionElement);

            // accept move, then copy, and finally accept whatever they requested (for now)
            if (e.AllowedOperations.HasFlag(DataPackageOperation.Move) || e.DataView.RequestedOperation.HasFlag(DataPackageOperation.Move))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
            else if (e.AllowedOperations.HasFlag(DataPackageOperation.Copy) || e.DataView.RequestedOperation.HasFlag(DataPackageOperation.Copy))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }  else 
            {
                e.AcceptedOperation = e.DataView.RequestedOperation;
            }

            // special case for schema view... should be removed
            //if (e.DataView.Properties.ContainsKey("CollectionReference"))
            //    e.AcceptedOperation = DataPackageOperation.Copy;
            
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
            var parentCollection = (sender as DependencyObject).GetFirstAncestorOfType<CollectionView>();
            parentCollection?.ViewModel?.CollectionViewOnDragEnter(parentCollection.CurrentView, e);

            var element = sender as SelectionElement;
            if (element != null)
            {
                var color = ((SolidColorBrush)App.Instance.Resources["DragHighlight"]).Color;
                this.ChangeIndicationColor(element, color);
                element.HasDragLeft = true;
                var parent = element.ParentSelectionElement;
                // if the current collection fires a dragleave event and its parent hasn't
                if (parent != null && !parent.HasDragLeft)
                {
                    this.ChangeIndicationColor(parent, color);
                }
            }
            this.RemoveDragDropIndication(sender as SelectionElement);
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
                var color = ((SolidColorBrush)App.Instance.Resources["DragHighlight"]).Color;
                element.HasDragLeft = false;
                _previousDragEntered = element;
                this.ChangeIndicationColor(element, color);
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
        

        #endregion

    }
}