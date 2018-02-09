using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Dash.Controllers;
using DashShared;
using static Dash.NoteDocuments;

namespace Dash
{
    /// <summary>
    ///     The file types that we currently can parse into valid Dash Documents
    /// </summary>
    public enum FileType
    {
        Ppt,
        Web,
        Image,
        Json,
        Csv,
        Pdf,
        Text
    }

    /// <summary>
    /// Contains all the necessary information to parse files regardless of if they are stored locally or on the web
    /// </summary>
    public struct FileData
    {

        /// <summary>
        /// the storage file used when the file is stored locally
        /// </summary>
        public IStorageFile File;

        /// <summary>
        /// The uri for the file, can be null if the file is stored locally but does not have a path (i.e. when it exists only in a drag event)
        /// Because of this check for if a file is local using File.filetype != ".url"
        /// </summary>
        public Uri FileUri;

        /// <summary>
        /// The built in filetype that we have parsed from the file
        /// </summary>
        public FileType Filetype;
    }


    public static class FileDropHelper
    {

        public static DocumentController CreateFieldLayoutDocumentFromReference(ReferenceController reference,
            double x = 0, double y = 0, double w = 200, double h = 200, TypeInfo listType = TypeInfo.None)
        {
            var type = reference.DereferenceToRoot(null).TypeInfo;
            switch (type)
            {
                case TypeInfo.Text:
                    return new TextingBox(reference, x, y, w, h).Document;
                case TypeInfo.Number:
                    return new TextingBox(reference, x, y, w, h).Document;
                case TypeInfo.Image:
                    return new ImageBox(reference, x, y, w, h).Document;
                case TypeInfo.List:
                    if (listType == TypeInfo.Document)
                    {
                        return new CollectionBox(reference, x, y, w, h).Document;
                    }
                    throw new NotImplementedException();
                case TypeInfo.Document:
                    return new DocumentBox(reference, x, y, w, h).Document;
                case TypeInfo.Point:
                    return new TextingBox(reference, x, y, w, h).Document;
                case TypeInfo.RichText:
                    return new RichTextBox(reference, x, y, w, h).Document;
                default:
                    return null;
            }
        }

        public static DocumentController AddFieldFromData(object data, DocumentController document, KeyController key,
            Point position, TypeInfo type, ListController<DocumentController> activeLayout)
        {
            var fmc = FieldControllerFactory.CreateFromModel(TypeInfoHelper.CreateFieldModelHelper(type, data));
            document.SetField(key, fmc, true);
            var layoutDoc =
                CreateFieldLayoutDocumentFromReference(new DocumentReferenceController(document.GetId(), key),
                    position.X, position.Y);
            activeLayout?.Add(layoutDoc);
            return layoutDoc;
        }

        public static async Task<DocumentController> GetDroppedFile(DragEventArgs e)
        {
            var files = (await e.DataView.GetStorageItemsAsync()).OfType<IStorageFile>().ToList();

            // TODO Luke should refactor this if else since the code is more or less copy pasted
            if (files.Count == 1)
            {
                var fileType = await GetFileData(files.First(), e.DataView);
                return await ParseFileAsync(fileType, new Point(), e.DataView).AsAsyncOperation();
            }
            return null;
        }

        /// <summary>
        ///     Handles the situation where a file is dropped on a collection. The DragEventArgs are assumed
        ///     to have StorageItems, and the DragEventArgs should have been handled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="collectionViewModel"></param>
        public static void HandleDropOnCollectionAsync(object sender, DragEventArgs e,
            ICollectionViewModel collectionViewModel)
        {
            // the point where the items will be dropped
            var where = sender is CollectionFreeformView
                ? Util.GetCollectionFreeFormPoint((CollectionFreeformView)sender, e.GetPosition(MainPage.Instance))
                : new Point();
            HandleDrop(e.DataView, where, collectionViewModel);
        }

        public static async void HandleDrop(DataPackageView dataView, Point where, ICollectionViewModel collectionViewModel)
        {
            // get all the files from the drag event
            var files = (await dataView.GetStorageItemsAsync()).OfType<IStorageFile>().ToList();

            // if there is only one file then we add it to the collection as a single document
            if (files.Count == 1)
            {
                // for each file, get it's type, parse it, and add it to the collection in the proper position
                var fileType = await GetFileData(files.First(), dataView);
                var documentController = await ParseFileAsync(fileType, where, dataView);
                if (documentController != null)
                {
                    documentController.GetPositionField().Data = where;
                    collectionViewModel.AddDocument(documentController, null);
                }
            }

            // if there is more than one file then we add it to the collection as a collection of documents
            else if (files.Any())
            {
                // create a containing collection to hold all the files
                var outputCollection = new List<DocumentController>();

                int offset = 0; 
                // for each file, get its type, parse it, and add it to the output collection
                foreach (var file in files)
                {
                    FileData fileType;
                    try
                    {
                        fileType = await GetFileData(file, dataView);
                        var documentController = await ParseFileAsync(fileType, where, dataView);
                        if (documentController != null)
                        {
                            outputCollection.Add(documentController);
                            documentController.SetField(KeyStore.PositionFieldKey, new PointController(new Point(offset, 0)), true);
                            offset += 200; 
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                var cnote = new CollectionNote(where, CollectionView.CollectionViewType.Schema, 200, 200, outputCollection);
                collectionViewModel.AddDocument(cnote.Document, null);

            }
            else
            {
                throw new ArgumentException("The drag event did not contain any storage items");
            }
        }

        // TODO comment this method - LM
        private static async Task<DocumentController> ParseFileAsync(FileData fileData, Point where,
            DataPackageView dataView)
        {
            switch (fileData.Filetype)
            {
                case FileType.Ppt:
                    return await new PptToDashUtil().ParseFileAsync(fileData);
                case FileType.Json:
                    return await new JsonToDashUtil().ParseFileAsync(fileData);
                case FileType.Csv:
                    return await new CsvToDashUtil().ParseFileAsync(fileData);
                case FileType.Image:
                    return await new ImageToDashUtil().ParseFileAsync(fileData);
                case FileType.Web:
                    var link = await dataView.GetWebLinkAsync();
                    return new HtmlNote(link.AbsoluteUri, where: where).Document;
                case FileType.Pdf:
                    return await new PdfToDashUtil().ParseFileAsync(fileData);
                case FileType.Text:
                    return await new TextToDashUtil().ParseFileAsync(fileData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(fileData.Filetype), fileData.Filetype, null);
            }
        }

        
        /// <summary>
        /// Gets all the file data for a storage item that is coming from a drag event args
        /// </summary>
        /// <param name="storageItem"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static async Task<FileData> GetFileData(IStorageFile storageItem,  DataPackageView dataView)
        {
            // if the file is a url then check the link filetype
            if (storageItem.FileType.EndsWith(".url"))
            {
                var link = await dataView.GetWebLinkAsync();
                // if the link does not have a filetype assume its a web link
                return new FileData()
                {
                    File = storageItem,
                    Filetype = GetFileType(link.AbsoluteUri) ?? FileType.Web,
                    FileUri = link
                };
            }

            // otherwise the file is a local file so check the storage file path and file type
            var fileType = GetFileType(storageItem.Path) ??
                           GetFileType(storageItem.FileType) ??
                           throw new ArgumentException(
                               $"We do not support the file type for the passed in file: {storageItem.Path}");

            return new FileData()
            {
                File = storageItem,
                Filetype = fileType,
                FileUri = string.IsNullOrWhiteSpace(storageItem.Path) ? null : new Uri(storageItem.Path)
            };
        }


        private static FileType? GetFileType(string filepath)
        {
            filepath = filepath.ToLower();
            if (filepath.EndsWith(".pdf"))
                return FileType.Pdf;
            if (filepath.EndsWith(".json"))
                return FileType.Json;
            if (filepath.EndsWith(".csv"))
                return FileType.Csv;
            if (filepath.EndsWith(".pptx"))
                return FileType.Ppt;
            if (filepath.EndsWith(".pptx"))
                return FileType.Ppt;
            if (filepath.EndsWith(".jpg") ||
                filepath.EndsWith(".jpeg") || 
                filepath.EndsWith(".png") || 
                filepath.EndsWith(".bmp") || 
                filepath.EndsWith(".gif")) // PRODUCTION READY! Is this all of them? who knows?
                return FileType.Image;
            if (filepath.EndsWith(".txt"))
                return FileType.Text;

            return null;

        }


        // TODO remove this method
        public static async void HandleDropOnCollection(object sender, DragEventArgs e,
            BaseCollectionViewModel collection)
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var elem = sender as UIElement;
                var dropPoint = e.GetPosition(elem);
                foreach (var item in items)
                {
                    var storageFile = item as StorageFile;
                    var fields = new Dictionary<KeyController, FieldControllerBase>
                    {
                        [KeyStore.SystemUriKey] = new TextController(storageFile.Path + storageFile.Name)
                    };
                    var doc = new DocumentController(fields, DashConstants.TypeStore.FileLinkDocument);
                    var tb = new TextingBox(new DocumentReferenceController(doc.GetId(), KeyStore.SystemUriKey))
                        .Document;
                    doc.SetActiveLayout(new FreeFormDocument(new List<DocumentController> {tb}, dropPoint).Document,
                        false, true);
                    collection.AddDocument(doc, null);
                    dropPoint.X += 20;
                    dropPoint.Y += 20;
                }
            }
            e.Handled = true;
        }
    }
}
