using System;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using DashShared;
using static Dash.DataTransferTypeInfo;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable once CheckNamespace
namespace Dash
{
    [Flags]
    public enum DataTransferTypeInfo
    {
        None = 0x0,
        Image = 0x1,
        Internal = 0x2,
        FileSystem = 0x4,
        Html = 0x8,
        Rtf = 0x10,
        PlainText = 0x20,

        External = Image | FileSystem | Html | Rtf,
        Any = External | Internal
    }

    public static class DataPackageExtensionMethods
    {
        // DATA ANALYSIS

        public static bool HasDataOfType(this DataPackageView packageView, DataTransferTypeInfo transferType)
        {
            var retVal = false;

            if (transferType.HasFlag(Html)) {
                retVal |= packageView.Contains(StandardDataFormats.Html);
            }

            if (transferType.HasFlag(Rtf)) {
                retVal |= packageView.Contains(StandardDataFormats.Rtf);
            }

            if (transferType.HasFlag(PlainText))
            {
                retVal |= packageView.Contains(StandardDataFormats.Text);
            }

            if (transferType.HasFlag(Image))
            {
                retVal |= packageView.Contains(StandardDataFormats.Bitmap);
            }

            if (transferType.HasFlag(FileSystem))
            {
                retVal |= packageView.Contains(StandardDataFormats.StorageItems);
            }

            if (transferType.HasFlag(Internal))
            {
                retVal |= packageView.Properties.ContainsKey(nameof(DragModelBase));
            }

            return retVal;
        }

        // DATA ACCESS

        public static async Task<List<DocumentController>> GetDroppableDocumentsForDataOfType(this DataPackageView packageView, DataTransferTypeInfo transferType, FrameworkElement targetElement, Point? where = null)
        {
            var dropDocs = new List<DocumentController>();

            // If the enum is or falls within the requested enum data type and such data exists in the package, convert it to its corresponding document controller
            // and return the list of these collected documents

            // Storage Items

            if (transferType.HasFlag(FileSystem) && packageView.Contains(StandardDataFormats.StorageItems))
            {
                DocumentController documentController = await FileDropHelper.HandleDrop(packageView, where ?? new Point());
                if (documentController != null) dropDocs.Add(documentController);
                else if(transferType.HasFlag(Html) && packageView.Contains(StandardDataFormats.Html))
                {
                    dropDocs.Add(await HtmlToDashUtil.ConvertHtmlData(packageView, where ?? new Point()));
                }
            }

            // HTML

            else if (transferType.HasFlag(Html) && packageView.Contains(StandardDataFormats.Html))
            {
                dropDocs.Add(await HtmlToDashUtil.ConvertHtmlData(packageView, where ?? new Point()));
            } 

            // RTF

            else if (transferType.HasFlag(Rtf) && packageView.Contains(StandardDataFormats.Rtf))
            {
                dropDocs.Add(await ConvertRtfData(packageView, where ?? new Point()));
            }

            // Plain Text

            else if (transferType.HasFlag(PlainText) && packageView.Contains(StandardDataFormats.Text))
            {
                dropDocs.Add(await ConvertPlainTextData(packageView, where ?? new Point()));
            }

            else if ( packageView.Contains(StandardDataFormats.Text))
            {
                dropDocs.Add(await TableExtractionRequest.ProcessTableData(await packageView.GetTextAsync(), where ?? new Point()));
            }

            // Image (rarely hit, most images fall under Storage Items)

            else if (transferType.HasFlag(Image) && packageView.Contains(StandardDataFormats.Bitmap))
            {
                dropDocs.Add(await ConvertBitmapData(packageView, where ?? new Point()));
            }

            // Internal Dash Document or Field

            else if (transferType.HasFlag(Internal))
            {
                dropDocs.AddRange(await packageView.GetAllInternalDroppableDocuments(where, targetElement));
            }

            return dropDocs;
        }

        // HELPER METHODS

        public static async Task<List<DocumentController>> GetAllInternalDroppableDocuments(this DataPackageView packageView, Point? where, FrameworkElement sender)
        {
            var dragModel = packageView.GetDragModel();
            return (dragModel?.CanDrop(sender) ?? false) ? await dragModel.GetDropDocuments(where, sender) : new List<DocumentController>();
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

            return await ImageToDashUtil.CreateImageNoteFromLocalFile(localFile, "Image dropped from file system", where);
        }

        private static async Task<DocumentController> ConvertRtfData(DataPackageView packageView, Point where)
        {
            string text = await packageView.GetRtfAsync();
            return new RichTextNote(text, where, new Size(300, double.NaN)).Document;
        }

        private static async Task<DocumentController> ConvertPlainTextData(DataPackageView packageView, Point where)
        {
            string text = await packageView.GetTextAsync();
            return new RichTextNote(text, where, new Size(300, double.NaN)).Document;
        }


        public static bool HasDroppableDragModels(this DataPackageView packageView, FrameworkElement target)
        {
            var dragModel = packageView.GetDragModel();
            return dragModel != null && IsDroppable(dragModel, target);
        }

        private static bool IsDroppable(DragModelBase dragModel, FrameworkElement target)
        {
            return dragModel is DragFieldModel || dragModel is DragDocumentModel ddm && ddm.CanDrop(target);
        }

        public static bool HasDragModel(this DataPackageView packageView)
        {
            return packageView.Properties.ContainsKey(nameof(DragModelBase));
        }

        public static bool HasDragModel(this DataPackage package)
        {
            return package.Properties.ContainsKey(nameof(DragModelBase));
        }

        public static DragModelBase GetDragModel(this DataPackageView packageView)
        {
            return !packageView.Properties.ContainsKey(nameof(DragModelBase)) ? null : (DragModelBase)packageView.Properties[nameof(DragModelBase)];
        }

        public static JoinDragModel GetJoinDragModel(this DataPackageView packageView)
        {
            return !packageView.Properties.ContainsKey(nameof(JoinDragModel))
                ? null
                : (JoinDragModel)packageView.Properties[nameof(JoinDragModel)];
        }

        public static void SetDragModel(this DataPackage package, DragModelBase model)
        {
            Debug.Assert(!package.HasDragModel());
            package.Properties[nameof(DragModelBase)] = model;
        }

        public static void SetJoinModel(this DataPackage package, JoinDragModel model)
        {
            package.Properties[nameof(JoinDragModel)] = model;
        }

        public static bool TryGetLoneDocument(this DataPackageView packageView, out DocumentController doc)
        {
            var dragModel = packageView.GetDragModel();
            if (dragModel is DragDocumentModel ddm && ddm.DraggedDocuments.Count == 1)
            {
                doc = ddm.DraggedDocuments[0];
                return true;
            }

            doc = null;
            return false;
        }

        public static bool TryGetLoneDragDocAndView(this DataPackageView packageView, out DocumentController doc, out DocumentView linkView)
        {
            var dragModel = packageView.GetDragModel();
            if (dragModel is DragDocumentModel ddm && ddm.DraggedDocuments.Count == 1)
            {
                doc = ddm.DraggedDocuments[0];
                linkView = ddm.DraggedDocumentViews?.FirstOrDefault();
                return true;
            }

            doc = null;
            linkView = null;
            return false;
        }

        public static bool HasAnyPropertiesSet(this DataPackageView packageView) => packageView.Properties.ToList().Any(); 
    }
}
