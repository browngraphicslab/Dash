using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using Dash.Controllers;
using Dash.Converters;
using Dash.Models.DragModels;
using DashShared;
using Flurl.Util;
using Microsoft.Office.Interop.Word;
using Syncfusion.Pdf.Graphics;
using Border = Windows.UI.Xaml.Controls.Border;
using Point = Windows.Foundation.Point;
using Task = System.Threading.Tasks.Task;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateEditorView : UserControl
    {
        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument { get; set; }

        public ObservableCollection<DocumentController> DocumentControllers { get; set; }

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

        public Collection<DocumentView> DocumentViews { get; set; }

        private KeyValueTemplatePane _keyValuePane;
        private DocumentView _selectedDocument;
        private Point _pasteWhereHack;
        private double _thickness;
        private Windows.UI.Color _color;
        DataPackage dataPackage = new DataPackage();

       

        public TemplateEditorView()
	    {
		    this.InitializeComponent();
	        //this.GetFirstAncestorOfType<DocumentView>().ViewModel.DecorationState = false;
	        //this.GetFirstAncestorOfType<DocumentView>().hideControls();

            DocumentControllers = new ObservableCollection<DocumentController>();
	        DocumentViewModels = new ObservableCollection<DocumentViewModel>();
	        DocumentViews = new Collection<DocumentView>();

	      
        }

        private void DocumentControllers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddDocs(e.NewItems.Cast<DocumentController>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void AddDocs(IEnumerable<DocumentController> newDocs)
        {
            foreach (var doc in newDocs)
            {
                // create new viewmodel with a copy of document, set editor to this
                var dvm =
                    new DocumentViewModel(doc.GetViewCopy(new Point(0, 0)), new Context(doc));
                DocumentViewModels.Add(dvm);
                // check that the layout doc doesn't already exist in data document's list of layout docs
                // if it already exists, don't add it again to the data document
                DataDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey).Add(dvm.LayoutDocument);
            }

            xItemsControl.ItemsSource = DocumentViewModels;

            //var dvms = xItemsControl.Items.Cast<DocumentViewModel>();
            //foreach (var dvm in dvms)
            //{
            //    // idk what to do from here, we have the viewmodel but we need the view to change manipulations
            //}
        }

        public void Load()
        {
	        this.UpdatePanes();
            var rect = new Rect(0, 0, 300, 400);
            var rectGeo = new RectangleGeometry {Rect = rect};
            xWorkspace.Clip = rectGeo;
        }

	    public void UpdatePanes()
	    {
			//make key value pane
		    if (xDataPanel.Children.Count == 0)
		    {
				_keyValuePane = new KeyValueTemplatePane(this);
			    xDataPanel.Children.Add(_keyValuePane);
			}

            //make central collection/canvas
	        DocumentControllers =
	            new ObservableCollection<DocumentController>(DataDocument
	                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData);
           
        }

        private void XWorkspace_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ViewModel.DisableDecorations = true;
            this.GetFirstAncestorOfType<DocumentView>().hideControls();
            DocumentViewModels.Clear();
            foreach (var layoutDoc in DataDocument
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData)
            {
                DocumentViewModels.Add(new DocumentViewModel(layoutDoc));
            }
            xItemsControl.ItemsSource = DocumentViewModels;
            DocumentControllers.CollectionChanged += DocumentControllers_CollectionChanged;
        }

        private void XWorkspace_OnUnloaded(object sender, RoutedEventArgs e)
        {
            DocumentControllers.CollectionChanged -= DocumentControllers_CollectionChanged;
        }

        private void TextButton_OnClick(object sender, RoutedEventArgs e)
        {
            DocumentControllers.Add(new RichTextNote("New text box").Document);
        }

        private async void ImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            //opens file picker and limits search by listed image extensions
            var imagePicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            imagePicker.FileTypeFilter.Add(".jpg");
            imagePicker.FileTypeFilter.Add(".jpeg");
            imagePicker.FileTypeFilter.Add(".bmp");
            imagePicker.FileTypeFilter.Add(".png");
            imagePicker.FileTypeFilter.Add(".svg");

            //adds each image selected to Dash
            var imagesToAdd = await imagePicker.PickMultipleFilesAsync();
            
            if (imagesToAdd != null)
            {
                foreach (var thisImage in imagesToAdd)
                {
                    var parser = new ImageToDashUtil();
                    var docController = await parser.ParseFileAsync(thisImage);
                    if (docController != null)
                    {
                        if (docController.GetWidthField().Data >= xWorkspace.Width)
                            docController.SetWidth(xWorkspace.Width - 20);
                        // TODO: Check for if height is too large (may be difficult bcs height = nan? -sy
                        DocumentControllers.Add(docController);
                    }
                }
            }
        }

        private async void VideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's video library
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".wmv");

            //awaits user upload of video 
            var files = await picker.PickMultipleFilesAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the video, set position, and add to canvas
                    var docController = await new VideoToDashUtil().ParseFileAsync(file);
                    if (docController != null)
                    {
                        if (docController.GetWidthField().Data >= xWorkspace.Width)
                            docController.SetWidth(xWorkspace.Width - 20);
                        // TODO: Check for if height is too large (may be difficult bcs height = nan? -sy
                        DocumentControllers.Add(docController);
                    }
                }

                //add error message for null file?
            }
        }

        private async void AudioButton_OnClick(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's audio library
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };

            picker.FileTypeFilter.Add(".mp3");


            //awaits user upload of audio 
            var files = await picker.PickMultipleFilesAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the audio, set position, and add to canvas
                    var docController = await new AudioToDashUtil().ParseFileAsync(file);
                    if (docController != null)
                    {
                        if (docController.GetWidthField().Data >= xWorkspace.Width)
                            docController.SetWidth(xWorkspace.Width - 20);
                        DocumentControllers.Add(docController);
                    }
                }
                //add error message for null file?
            }
        }

        private void AlignmentButton_OnChecked(object sender, RoutedEventArgs e)
        {
            var button = sender as AppBarButton;

            //for each document, align according to what button was pressed
            foreach (var dvm in DocumentViewModels)
            {
                var point = dvm.LayoutDocument.GetField<PointController>(KeyStore.PositionFieldKey);
                switch (button.Name)
                {
                    case "xAlignLeftButton":
                        point = new PointController(0, point.Data.Y);
                        break;

                    case "xAlignCenterButton":
                        var centerX = (xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X) / 2;
                        point = new PointController(centerX, point.Data.Y);
                        break;

                    case "xAlignRightButton":
                        var rightX = xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X;
                        point = new PointController(rightX, point.Data.Y);
                        break;
                }
                dvm.LayoutDocument.SetField(KeyStore.PositionFieldKey, point, true);
            }
        }

        

        private void BorderOption_OnChanged(object sender, RoutedEventArgs e)
        {
            double left = 0;
            if (xLeftBorderChecker.IsChecked.GetValueOrDefault(false))
            {
                left = _thickness;
            }

            double top = 0;
            if (xTopBorderChecker.IsChecked.GetValueOrDefault(false))
            {
                top = _thickness;
            }

            double right = 0;
            if (xRightBorderChecker.IsChecked.GetValueOrDefault(false))
            {
                right = _thickness;
            }

            double bottom = 0;
            if (xBottomBorderChecker.IsChecked.GetValueOrDefault(false))
            {
                bottom = _thickness;
            }

            if (_selectedDocument != null)
            {
                _selectedDocument.TemplateBorder.BorderBrush = new SolidColorBrush(_color);
                _selectedDocument.TemplateBorder.BorderThickness = new Thickness(left, top, right, bottom);
            }
        }

        private void ApplyChanges_OnClicked(object sender, RoutedEventArgs e)
        {
            var workingDoc = LayoutDocument.GetField<DocumentController>(KeyStore.DataKey);
            DataDocument.SetField(KeyStore.DocumentContextKey, workingDoc, true);
            foreach (var doc in DocumentControllers)
            {
                if (doc.GetDataDocument().Equals(workingDoc.GetDataDocument()) || doc.GetDataDocument().Equals(workingDoc))
                {
                    doc.SetField(KeyStore.DocumentContextKey, new DocumentReferenceController(DataDocument.Id, KeyStore.DocumentContextKey),
                        true);
                    var keyValuePairs = DataDocument.GetField<DocumentController>(KeyStore.DocumentContextKey).GetDataDocument().EnumFields();
                    KeyController specificKey = null;
                    specificKey = keyValuePairs.FirstOrDefault(kvp => kvp.Key.ToString().Equals(doc.Title)).Key;

                    if (specificKey != null)
                    {
                        doc.SetField(KeyStore.DataKey,
                            new PointerReferenceController(
                                doc.GetField<DocumentReferenceController>(KeyStore.DocumentContextKey), specificKey), true);
                    }
                    else if (doc.Equals(DataDocument))
                    {
                        doc.SetField(KeyStore.DataKey,
                            new DocumentReferenceController(
                                DataDocument.Id, KeyStore.DataKey), true);
                    }
                   
                }
                else
                {
                }
            }

            var layoutCopy = DataDocument.MakeCopy();
            layoutCopy.SetField(KeyStore.DocumentContextKey, workingDoc.GetDataDocument(), true);
            layoutCopy.SetField(KeyStore.PositionFieldKey,
                workingDoc.GetField<PointController>(KeyStore.PositionFieldKey), true);
            workingDoc.SetField(KeyStore.ActiveLayoutKey, layoutCopy, true);
        }

        private void DocumentView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var docView = sender as DocumentView;
            if (!DocumentViews.Contains(docView))
            {
                DocumentViews.Add(docView);
                docView.hideEllipses();
            }
            
            var bounds = new Rect(0, 0, xWorkspace.Clip.Rect.Width - docView.ActualWidth,
                xWorkspace.Clip.Rect.Height - docView.ActualHeight);
            docView.Bounds = new RectangleGeometry { Rect = bounds };
            docView.DocumentSelected += DocView_DocumentSelected;
        }

        private void DocView_DocumentSelected(DocumentView sender, DocumentView.DocumentViewSelectedEventArgs args)
        {
            _selectedDocument = sender;
        }

        private void TemplateEditorView_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //var focused = this.Focus(FocusState.Programmatic);
        }

        private async void XWorkspace_OnDrop(object sender, DragEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                e.Handled = true;
                // accept move, then copy, and finally accept whatever they requested (for now)
                if (e.AllowedOperations.HasFlag(DataPackageOperation.Move))
                    e.AcceptedOperation = DataPackageOperation.Move;
                else if (e.AllowedOperations.HasFlag(DataPackageOperation.Copy))
                    e.AcceptedOperation = DataPackageOperation.Copy;
                else e.AcceptedOperation = e.DataView.RequestedOperation;

                //RemoveDragDropIndication(sender as UserControl);

                var senderView = (sender as CollectionView)?.CurrentView as ICollectionView;
                var where = new Point();
                if (senderView is CollectionFreeformView)
                    where = Util.GetCollectionFreeFormPoint(senderView as CollectionFreeformView,
                        e.GetPosition(MainPage.Instance));
                else if (DocumentViewModels.Count > 0)
                {
                    var lastPos = DocumentViewModels.Last().Position;
                    where = e.GetPosition(xWorkspace);
                }

                // if we drag from the file system
                if (e.DataView?.Contains(StandardDataFormats.StorageItems) == true)
                {
                    try
                    {
                        var droppedDoc = await FileDropHelper.HandleDrop(where, e.DataView, null);
                        if (droppedDoc != null)
                            DocumentControllers.Add(droppedDoc);
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
                        DocumentControllers.Add(imgNote.Document);
                        return;
                    }

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
                    var htmlNote = new RichTextNote(richtext, _pasteWhereHack, new Size(300, 300)).Document;


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
                                htmlNote.GetDataDocument().SetField<TextController>(new KeyController(pair[0], pair[0]),
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
                                        .SetField<TextController>(new KeyController(pair[0], pair[0]), pair[1].Trim(),
                                            true);
                                }
                            }
                        }
                    }

                    DocumentControllers.Add(htmlNote);
                }
                else if (e.DataView?.Contains(StandardDataFormats.Rtf) == true)
                {
                    var text = await e.DataView.GetRtfAsync();

                    var t = new RichTextNote(text, where, new Size(300, double.NaN));
                    DocumentControllers.Add(t.Document);
                }
                else if (e.DataView?.Contains(StandardDataFormats.Text) == true)
                {
                    var text = await e.DataView.GetTextAsync();
                    var t = new RichTextNote(text, where, new Size(300, double.NaN));
                    var matches = new Regex(".*:.*").Matches(text);
                    foreach (var match in matches)
                    {
                        var pair = new Regex(":").Split(match.ToString());
                        t.Document.GetDataDocument()
                            .SetField<TextController>(KeyController.LookupKeyByName(pair[0], true), pair[1].Trim('\r'),
                                true);
                    }

                    DocumentControllers.Add(t.Document);
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
                    DocumentControllers.Add(img);
                    var t = new ImageNote(new Uri(localFile.FolderRelativeId));
                    // var t = new AnnotatedImage(null, Convert.ToBase64String(buffer), "", "");
                    DocumentControllers.Add(t.Document);
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
                        DocumentControllers.Add(cnote.Document);
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
                            if (p.GetActiveLayout() == null && p.GetDereferencedField(KeyStore.DocumentContextKey, null) == null)
                                p.SetActiveLayout(new DefaultLayout().Document, true, true);
                            var newDoc = e.AcceptedOperation == DataPackageOperation.Move ? p.GetSameCopy(where) :
                                         e.AcceptedOperation == DataPackageOperation.Link ? p.GetKeyValueAlias(where) : p.GetCopy(where);
                            if (double.IsNaN(newDoc.GetWidthField().Data))
                                newDoc.SetWidth(dragData.Width ?? double.NaN);
                            if (double.IsNaN(newDoc.GetHeightField().Data))
                                newDoc.SetHeight(dragData.Height ?? double.NaN);
                            return newDoc;
                        });
                        DocumentControllers.Add(new CollectionNote(where, dragData.ViewType, 500, 300, payloadLayoutDelegates.ToList()).Document);
                    }
                }
                // if the user drags a data document
                else if (e.DataView?.Properties.ContainsKey(nameof(List<DragDocumentModel>)) == true)
                {
                    var dragModel = (List<DragDocumentModel>)e.DataView.Properties[nameof(List<DragDocumentModel>)];
                    foreach (var d in dragModel.Where((dm) => dm.CanDrop(sender as FrameworkElement)))
                    {
                        var start = dragModel.First().DraggedDocument.GetPositionField().Data;
                        foreach (var doc in (dragModel.Where((dm) => dm.CanDrop(sender as FrameworkElement)).Select(
                            (dm) => dm.GetDropDocument(new Point(
                                dm.DraggedDocument.GetPositionField().Data.X - start.X + where.X,
                                dm.DraggedDocument.GetPositionField().Data.Y - start.Y + where.Y), true)).ToList()))
                        {
                            DocumentControllers.Add(doc);
                        }
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
                                DocumentControllers.Add(cnote.Document);
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
                                DocumentControllers.Add(cnote.Document);
                            }
                        }
                        else // if no modifiers are pressed, we want to create a new annotation document and link it to the source document (region)
                        {
                            var dragDoc = dragModel.DraggedDocument;
                            if (dragModel.LinkSourceView != null &&
                                KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                                dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](dragModel.LinkSourceView);
                            var note = new RichTextNote("<annotation>", where).Document;
                            dragDoc.Link(note);
                            DocumentControllers.Add(note);
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
                        DocumentControllers.Add(dragModel.GetDropDocument(where));
                    }
                }
            }
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

        private void XThicknessSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider slider)
            {
                _thickness = slider.Value;
            }
        }

        private void XColorPicker_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
             //_color = xColorPicker.SelectedColor;    
        }

        private void XResetButton_OnClick_(object sender, RoutedEventArgs e)
        {
            //TODO: reset to original state of template (clear if new, or revert to other if editing)
        }

        private void XUndoButton_OnClick_(object sender, RoutedEventArgs e)
        {
            //TODO: implement undo
        }

        private void XRedoButton_OnClick_(object sender, RoutedEventArgs e)
        {
            //TODO: implement redo
        }

        private void XClearButton_OnClick_(object sender, RoutedEventArgs e)
        {
            //TODO: implement clear
        }

        private void XUploadTemplate_OnClick(object sender, RoutedEventArgs e)
        {
            //TODO: implement 
        }
    }
}
