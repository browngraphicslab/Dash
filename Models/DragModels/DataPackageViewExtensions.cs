using System;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using DashShared;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable once CheckNamespace
namespace Dash
{
    [Flags]
    public enum DragDataTypeInfo
    {
        None = 0x0,
        Image = 0x1,
        Document = 0x4,
        StorageFile = 0x8,
        Html = 0x16,
        Rtf = 0x32,
        Field = 0x64,

        External = Image | StorageFile | Html | Rtf,
        Dash = Document | Field,
        Any = External | Dash
    }

    public static class DataPackageExtensionMethods
    {
        public static bool HasDataOfType(this DataPackageView packageView, DragDataTypeInfo type)
        {
            var retVal = false;

            if (type.HasFlag(DragDataTypeInfo.Html)) {
                retVal |= packageView.Contains(StandardDataFormats.Html);
            }

            if (type.HasFlag(DragDataTypeInfo.Rtf)) {
                retVal |= packageView.Contains(StandardDataFormats.Rtf);
            }

            if (type.HasFlag(DragDataTypeInfo.Image))
            {
                retVal |= packageView.Contains(StandardDataFormats.Bitmap);
            }

            if (type.HasFlag(DragDataTypeInfo.StorageFile))
            {
                retVal |= packageView.Contains(StandardDataFormats.StorageItems);
            }

            if (type.HasFlag(DragDataTypeInfo.Document))
            {
                retVal |= packageView.Properties.ContainsKey(nameof(DragDocumentModel));
            }

            if (type.HasFlag(DragDataTypeInfo.Field))
            {
                retVal |= packageView.Properties.ContainsKey(nameof(DragFieldModel));
            }

            return retVal;
        }

        public static async Task<List<DocumentController>> GetDropDocumentsOfType(this DataPackageView packageView, DragDataTypeInfo type, Point where)
        {
            var dropDocs = new List<DocumentController>();

            if (type.HasFlag(DragDataTypeInfo.Html) && packageView.Contains(StandardDataFormats.Html))
            {
                dropDocs.Add(await HtmlToDashUtil.ConvertHtmlData(packageView, where));
            }

            if (type.HasFlag(DragDataTypeInfo.Rtf) && packageView.Contains(StandardDataFormats.Rtf))
            {
                dropDocs.Add(await ConvertRtfData(packageView, where));
            }

            if (type.HasFlag(DragDataTypeInfo.Image) && packageView.Contains(StandardDataFormats.Bitmap))
            {
                dropDocs.Add(await ConvertBitmapData(packageView, where));
            }

            if (type.HasFlag(DragDataTypeInfo.StorageFile) && packageView.Contains(StandardDataFormats.StorageItems))
            {
                dropDocs.Add(await FileDropHelper.HandleDrop(packageView, where));
            }

            if (type.HasFlag(DragDataTypeInfo.Document) && packageView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                var dragModelDocs = ((DragDocumentModel)packageView.Properties[nameof(DragDocumentModel)]).GetDropDocuments(where);
                dropDocs.AddRange(dragModelDocs);
            }

            if (type.HasFlag(DragDataTypeInfo.Field) && packageView.Properties.ContainsKey(nameof(DragFieldModel)))
            {
                var dragModelDocs = ((DragFieldModel)packageView.Properties[nameof(DragFieldModel)]).GetDropDocuments(where);
                dropDocs.AddRange(dragModelDocs);
            }

            return dropDocs;
        }

        private static async Task<DocumentController> ConvertBitmapData(DataPackageView packageView, Point where)
        {
            RandomAccessStreamReference bmp = await packageView.GetBitmapAsync();
            IRandomAccessStreamWithContentType streamWithContent = await bmp.OpenReadAsync();
            var buffer = new byte[streamWithContent.Size];

            using (var reader = new DataReader(streamWithContent))
            {
                await reader.LoadAsync((uint)streamWithContent.Size);
                reader.ReadBytes(buffer);
            }

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string uniqueFilePath = UtilShared.GenerateNewId() + ".jpg"; // somehow this works for all images... who knew

            StorageFile localFile = await localFolder.CreateFileAsync(uniqueFilePath, CreationCollisionOption.ReplaceExisting);
            localFile.OpenStreamForWriteAsync().Result.Write(buffer, 0, buffer.Count());

            return await ImageToDashUtil.CreateImageNoteFromLocalFile(localFile, where, "Image dropped from file system");
        }

        private static async Task<DocumentController> ConvertRtfData(DataPackageView packageView, Point where)
        {
            string text = await packageView.GetRtfAsync();
            return new RichTextNote(text, where, new Size(300, double.NaN)).Document;
        }
    }
}