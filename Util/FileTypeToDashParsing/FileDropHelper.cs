using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using MyToolkit.Multimedia;

namespace Dash
{
    /// <summary>
    ///     The file types that we currently can parse into valid Dash Documents
    /// </summary>
    public enum FileType
    {
        None,
        Text,
        Ppt,
        Web,
        Image,
		Video,
        Json,
        Csv,
        Pdf,
        Audio
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
        /// <param name="dataView"></param>
        public static async Task<DocumentController> HandleDrop(DataPackageView dataView, Point where)
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
                    documentController.GetDataDocument().SetTitle(files[0].Name);
                    documentController.GetPositionField().Data = where;
                    var uri = fileType.FileUri?.AbsoluteUri ?? (dataView.AvailableFormats.Contains("UniformResourceLocator") ? (await dataView.GetWebLinkAsync())?.AbsoluteUri : null);
                    documentController.GetDataDocument()?
                        .SetField<TextController>(KeyStore.SourceUriKey, uri, true);
                    documentController.GetDataDocument()?
                        .SetField<TextController>(KeyStore.WebContextKey, uri, true);
                    return documentController;
                }
            }

            // if there is more than one file then we add it to the collection as a collection of documents
            else if (files.Any())
            {
                var CollectionNoteOffset = 260;
                // create a containing collection to hold all the files
                var outputCollection = new List<DocumentController>();

                double xPos = 0, yPos = 0, count = 0;  
                // for each file, get its type, parse it, and add it to the output collection
                foreach (var file in files)
                {
                    try
                    {
                        FileData fileType = await GetFileData(file, dataView);
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
                            var docWidth = documentController.GetWidthField()?.Data + 10 ?? CollectionNoteOffset;
                            xPos += double.IsNaN(docWidth) ? CollectionNoteOffset : docWidth;
                            count++;
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                var cnote = new CollectionNote(where, CollectionViewType.Schema, 200, 200, outputCollection);
                return cnote.Document;
            }
            else
            {
                throw new ArgumentException("The drag event did not contain any storage items");
            }
            return null;
        }

        // TODO comment this method - LM
        private static async Task<DocumentController> ParseFileAsync(FileData fileData, Point where,
            DataPackageView dataView)
        {
            switch (fileData.Filetype)
            {
                case FileType.None:
                    return null;
                case FileType.Ppt:
                    return await new PptToDashUtil().ParseFileAsync(fileData, dataView);
                case FileType.Json:
                    return await new JsonToDashUtil().ParseFileAsync(fileData, dataView);
                case FileType.Csv:
                    return await new CsvToDashUtil().ParseFileAsync(fileData, dataView);
                case FileType.Image:
                    return await new ImageToDashUtil().ParseFileAsync(fileData, dataView);
				case FileType.Video:
					return await new VideoToDashUtil().ParseFileAsync(fileData, dataView);
                case FileType.Audio:
                    return await new AudioToDashUtil().ParseFileAsync(fileData, dataView);
                case FileType.Web:
                    var link = await dataView.GetWebLinkAsync();

					// if this is a YouTube link, drop the video instead
	                if (link.Host == "www.youtube.com")
	                {
		                var query = HttpUtility.ParseQueryString(link.Query);
		                var videoId = string.Empty;
						// the video ID depends on if it's youtube or youtu.be
		                videoId = query.AllKeys.Contains("v") ? query["v"] : link.Segments.Last();

		                try
		                {
							// make the video box with the Uri set as the video's, and return it
			                var url = await YouTube.GetVideoUriAsync(videoId, YouTubeQuality.Quality1080P);
			                var uri = url.Uri;
			                return VideoToDashUtil.CreateVideoBoxFromUri(uri);
		                }
		                // if that returns an error somehow, just return the page instead
		                catch (Exception)
		                {
			                return new HtmlNote(link.AbsoluteUri, where: where).Document;
						}
	                }
	                else
					{
						return new HtmlNote(link.AbsoluteUri, where: where, size: new Size(double.NaN, double.NaN)).Document;
					}

				case FileType.Pdf:
                    return await new PdfToDashUtil().ParseFileAsync(fileData, dataView);
                case FileType.Text:
                    return await new TextToDashUtil().ParseFileAsync(fileData, dataView);
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
            if (storageItem.FileType.EndsWith(".url") && dataView.Contains(StandardDataFormats.WebLink))
            {
                var link = await dataView.GetWebLinkAsync();
                // if the link does not have a filetype assume its a web link
                var ftypeUri = GetFileType(link.AbsoluteUri);
                return new FileData()
                {
                    File = storageItem,
                    Filetype = ftypeUri == FileType.None ? FileType.Web : ftypeUri,
                    FileUri = link
                };
            }

            // otherwise the file is a local file so check the storage file path and file type 
            var ftypePath = GetFileType(storageItem.Path);
            var fileType = ftypePath == FileType.None ? GetFileType(storageItem.FileType) : ftypePath;

            if (fileType == FileType.None)
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


        public static FileType  GetFileType(string filepath)
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
				filepath.EndsWith(".mov"))
                return FileType.Video;
            if (filepath.EndsWith(".mp3") ||
                filepath.EndsWith(".wav"))
                return FileType.Audio;
            if (filepath.EndsWith(".txt"))
                return FileType.Text;

            return FileType.None;

        }
    }
}
