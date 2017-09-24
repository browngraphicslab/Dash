using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    /// <summary>
    ///     The file types that we currently can parse into valid Dash Documents
    /// </summary>
    public enum FileType
    {
        Ppt,
        Json,
        Csv,
        Pdf
    }


    public static class FileDropHelper
    {
        public static async void HandleDropOnDocument(object sender, DragEventArgs e)
        {
            var docView = sender as DocumentView;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems) || docView == null) return;
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var doc = docView.ViewModel.DocumentController;
                var dropPoint = e.GetPosition(docView);
                foreach (var item in items)
                {
                    var layoutDoc = await AddFileAsField(doc, dropPoint, item);
                    if (layoutDoc != null)
                        dropPoint.Y += layoutDoc.GetHeightField().Data;
                }
            }
        }

        private static async Task<DocumentController> AddFileAsField(DocumentController doc, Point dropPoint,
            IStorageItem item)
        {
            var storageFile = item as StorageFile;
            var key = new KeyController(Guid.NewGuid().ToString(), storageFile.DisplayName);
            var layout = doc.GetActiveLayout();
            var activeLayout =
                layout.Data.GetDereferencedField(KeyStore.DataKey, null) as DocumentCollectionFieldModelController;
            if (storageFile.IsOfType(StorageItemTypes.Folder))
            {
                //Add collection of new documents?
            }
            else if (storageFile.IsOfType(StorageItemTypes.File))
            {
                object data = null;
                TypeInfo t;
                switch (storageFile.FileType.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                        t = TypeInfo.Image;
                        //Todo: needs to be fixed bc the images wont display if you use the system uri (i.e. storageFile.Path)
                        var localFolder = ApplicationData.Current.LocalFolder;
                        var file = await localFolder.CreateFileAsync("filename.jpg",
                            CreationCollisionOption.ReplaceExisting);
                        await storageFile.CopyAndReplaceAsync(file);
                        data = new Uri(file.Path);
                        break;
                    case ".txt":
                        t = TypeInfo.Text;
                        data = await FileIO.ReadTextAsync(storageFile);
                        break;
                    case ".json":
                        t = TypeInfo.Document;
                        data = await FileIO.ReadTextAsync(storageFile);
                        break;
                    case ".rtf":
                        t = TypeInfo.RichText;
                        data = await FileIO.ReadTextAsync(storageFile);
                        break;
                    default:
                        t = TypeInfo.Text;
                        try
                        {
                            data = await FileIO.ReadTextAsync(storageFile);
                            //TODO maybe parse drag from browser url?
                            //var str = data.ToString().ToLower();
                            //if (str.Contains("https://") && (str.Contains(".jpg") || str.Contains(".png")))
                            //{
                            //    var imageUrl = str.Remove(0, 24).Replace("\r", string.Empty).Replace("\n", string.Empty);
                            //    data = new Uri(imageUrl, UriKind.Absolute);
                            //    t = TypeInfo.Image;
                            //}
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("could not import field from file " + storageFile.Name +
                                            " due to exception: " + ex);
                        }
                        break;
                }
                if (data != null)
                {
                    var layoutDoc = AddFieldFromData(data, doc, key, dropPoint, t, activeLayout);
                    return layoutDoc;
                }
            }
            return null;
        }

        public static DocumentController CreateFieldLayoutDocumentFromReference(ReferenceFieldModelController reference,
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
                case TypeInfo.Collection:
                    return new CollectionBox(reference, x, y, w, h).Document;
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
            Point position, TypeInfo type, DocumentCollectionFieldModelController activeLayout)
        {
            var dto = new FieldModelDTO(type, data);
            var fmc = TypeInfoHelper.CreateFieldModelControllerHelper(dto);
            document.SetField(key, fmc, true);
            var layoutDoc =
                CreateFieldLayoutDocumentFromReference(new ReferenceFieldModelController(document.GetId(), key),
                    position.X, position.Y);
            activeLayout?.AddDocument(layoutDoc);
            return layoutDoc;
        }

        /// <summary>
        ///     Handles the situation where a file is dropped on a collection. The DragEventArgs are assumed
        ///     to have StorageItems, and the DragEventArgs should have been handled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="collectionViewModel"></param>
        public static async Task HandleDropOnCollectionAsync(object sender, DragEventArgs e,
            ICollectionViewModel collectionViewModel)
        {
            var files = (await e.DataView.GetStorageItemsAsync()).OfType<IStorageFile>();
            if (files.Any())
            {
                // the point where the items will be dropped
                var where = sender is CollectionFreeformView
                    ? Util.GetCollectionFreeFormPoint((CollectionFreeformView) sender, e.GetPosition(MainPage.Instance))
                    : new Point();

                // for each file, get it's type, parse it, and add it to the collection in the proper position
                foreach (var file in files)
                {
                    var fileType = GetSupportedFileType(file);
                    var documentController = ParseFile(fileType, file, where);
                }
            }
            else
            {
                throw new ArgumentException("The drag event did not contain any storage items");
            }
        }

        /// <summary>
        ///     Parses the file of the passed in type
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="storageItem"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        private static async Task<DocumentController> ParseFile(FileType fileType, IStorageFile storageItem, Point where)
        {
            switch (fileType)
            {
                case FileType.Ppt:
                    return await new PptToDashUtil().ParseAsync(storageItem, "TODO GET UNIQUE PATH");
                case FileType.Json:
                    return await new JsonToDashUtil().ParseAsync(storageItem, "TODO GET A UNIQUE PATH");
                    break;
                case FileType.Csv:
                    break;
                case FileType.Pdf:
                    return await new PdfToDashUtil().ParseAsync(storageItem, "TODO GET A UNIQUE PATH");
                    break;
            }
            throw new NotImplementedException("We need to implement the proper parser!");
        }

        /// <summary>
        ///     Given a storageItem returns the filetype enum for that file
        ///     or throws an error to imply that we do not support that kind of file
        /// </summary>
        /// <param name="storageItem"></param>
        /// <returns></returns>
        private static FileType GetSupportedFileType(IStorageFile storageItem)
        {
            var storagePath = storageItem.Path.ToLower();
            if (storagePath.EndsWith(".pdf"))
                return FileType.Pdf;
            if (storagePath.EndsWith(".json"))
                return FileType.Json;
            if (storagePath.EndsWith(".csv"))
                return FileType.Csv;
            if (storagePath.EndsWith(".pptx"))
                return FileType.Ppt;
            throw new ArgumentException($"We do not support the file type for the passed in file: {storageItem.Path}");
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
                    var fields = new Dictionary<KeyController, FieldModelController>
                    {
                        [KeyStore.SystemUriKey] = new TextFieldModelController(storageFile.Path + storageFile.Name)
                    };
                    var doc = new DocumentController(fields, DashConstants.DocumentTypeStore.FileLinkDocument);
                    var tb = new TextingBox(new ReferenceFieldModelController(doc.GetId(), KeyStore.SystemUriKey))
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

        // TODO remove this method
        public static async void HandleDropOnCollection(IEnumerable<StorageFile> files, ICollectionView collection,
            Point dropPoint)
        {
            foreach (var storageFile in files)
            {
                var fields = new Dictionary<KeyController, FieldModelController>
                {
                    [KeyStore.SystemUriKey] = new TextFieldModelController(storageFile.Path + storageFile.Name)
                };
                var doc = new DocumentController(fields, DashConstants.DocumentTypeStore.FileLinkDocument);
                var tb = new TextingBox(new ReferenceFieldModelController(doc.GetId(), KeyStore.SystemUriKey)).Document;
                doc.SetActiveLayout(new FreeFormDocument(new List<DocumentController> {tb}, dropPoint).Document, false,
                    true);
                await AddFileAsField(doc, new Point(0, tb.GetHeightField().Data), storageFile);
                collection.ViewModel.AddDocument(doc, null);
                dropPoint.X += 20;
                dropPoint.Y += 20;
            }
        }
    }
}

}