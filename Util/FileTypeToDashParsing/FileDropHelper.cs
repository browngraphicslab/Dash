using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
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
		Video,
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
        /// <param name="where"></param>
        /// <param name="e"></param>
        /// <param name="collectionViewModel"></param>
        public static async void HandleDrop(Point where, DataPackageView dataView, CollectionViewModel collectionViewModel)
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
                var CollectionNoteOffset = 250;
                // create a containing collection to hold all the files
                var outputCollection = new List<DocumentController>();

                int xPos = 0, yPos = 0, count = 0;  
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
                            // place files in a grid 
                            if (count % 5 == 0)
                            {
                                yPos += CollectionNoteOffset;
                                xPos = 0;
                            }
                            documentController.GetPositionField().Data = new Point(xPos, yPos); 
                            xPos += CollectionNoteOffset;
                            count++;
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
				case FileType.Video:
					return await new VideoToDashUtil().ParseFileAsync(fileData);
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
        private static async Task<FileData> GetFileData(IStorageFile storageItem, DataPackageView dataView)
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
                           GetFileType(storageItem.FileType);

            if (fileType == null)
            {

                Debug.WriteLine(
                     $"We do not support the file type for the passed in file: {storageItem.Path}");
                return new FileData();
            }

            return new FileData()
            {
                File = storageItem,
                Filetype = (FileType)fileType,
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
			if (filepath.EndsWith(".mp4") ||
				filepath.EndsWith(".avi") ||
				filepath.EndsWith(".mov") ||
				filepath.EndsWith(".mp3"))
				return FileType.Video;
            if (filepath.EndsWith(".txt"))
                return FileType.Text;

            return null;

        }
    }
}
