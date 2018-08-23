using System;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.Generic;
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
                retVal |= packageView.Properties.ContainsKey(nameof(List<DragModelBase>));
            }

            return retVal;
        }

        // DATA ACCESS

        public static async Task<List<DocumentController>> GetDroppableDocumentsForDataOfType(this DataPackageView packageView, DataTransferTypeInfo transferType, FrameworkElement targetElement, Point @where = new Point())
        {
            var dropDocs = new List<DocumentController>();

            // If the enum is or falls within the requested enum data type and such data exists in the package, convert it to its corresponding document controller
            // and return the list of these collected documents

            // Storage Items

            if (transferType.HasFlag(FileSystem) && packageView.Contains(StandardDataFormats.StorageItems))
            {
                DocumentController documentController = await FileDropHelper.HandleDrop(packageView, where);
                if (documentController != null) dropDocs.Add(documentController);
            }

            // HTML

            else if (transferType.HasFlag(Html) && packageView.Contains(StandardDataFormats.Html))
            {
                dropDocs.Add(await HtmlToDashUtil.ConvertHtmlData(packageView, where));
            } 

            // RTF

            else if (transferType.HasFlag(Rtf) && packageView.Contains(StandardDataFormats.Rtf))
            {
                dropDocs.Add(await ConvertRtfData(packageView, where));
            }

            // Plain Text

            else if (transferType.HasFlag(PlainText) && packageView.Contains(StandardDataFormats.Text))
            {
                dropDocs.Add(await ConvertPlainTextData(packageView, where));
            }

            // Image (rarely hit, most images fall under Storage Items)

            else if (transferType.HasFlag(Image) && packageView.Contains(StandardDataFormats.Bitmap))
            {
                dropDocs.Add(await ConvertBitmapData(packageView, where));
            }

            // Internal Dash Document or Field

            else if (transferType.HasFlag(Internal))
            {
                dropDocs.AddRange(packageView.GetAllInternalDroppableDocuments(where, targetElement));
            }

            return dropDocs;
        }

        // HELPER METHODS

        public static List<DocumentController> GetAllInternalDroppableDocuments(this DataPackageView packageView, Point where, FrameworkElement sender)
        {
            var dragModels = packageView.GetDragModels();
            var dropSafe = dragModels.Where(dmb => dmb is DragFieldModel || dmb is DragDocumentModel ddm && ddm.CanDrop(sender)).ToList();
            return dropSafe.SelectMany(dm => dm.GetDropDocuments(where)).ToList();
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

        public static bool HasDragModels(this DataPackageView packageView) => packageView.Properties.ContainsKey(nameof(List<DragModelBase>));

        public static bool HasDroppableDragModels(this DataPackageView packageView, FrameworkElement target) => packageView.GetDragModels().Any(d => IsDroppable(d, target));

        private static bool IsDroppable(DragModelBase dragModel, FrameworkElement target) => dragModel is DragFieldModel || dragModel is DragDocumentModel ddm && ddm.CanDrop(target);

        public static bool HasDragModels(this DataPackage package) => package.Properties.ContainsKey(nameof(List<DragModelBase>));

        public static List<DragModelBase> GetDragModels(this DataPackageView packageView)
        {
            if (!packageView.Properties.ContainsKey(nameof(List<DragModelBase>))) return new List<DragModelBase>();
            return (List<DragModelBase>) packageView.Properties[nameof(List<DragModelBase>)];
        }

        public static List<DragModelBase> AddDragModel(this DataPackage package, DragModelBase model)
        {
            if (!package.HasDragModels()) package.Properties[nameof(List<DragModelBase>)] = new List<DragModelBase>() { model };
            else ((List<DragModelBase>) package.Properties[nameof(List<DragModelBase>)]).Add(model);

            return (List<DragModelBase>) package.Properties[nameof(List<DragModelBase>)];
        }

        public static List<DragModelBase> AddDragModels(this DataPackage package, List<DragModelBase> models)
        {
            if (!package.HasDragModels()) package.Properties[nameof(List<DragModelBase>)] = models;
            else ((List<DragModelBase>)package.Properties[nameof(List<DragModelBase>)]).AddRange(models);

            return (List<DragModelBase>)package.Properties[nameof(List<DragModelBase>)];
        }

        public static bool TryGetLoneDocument(this DataPackageView packageView, out DocumentController doc)
        {
            var dragModels = packageView.GetDragModels();
            if (dragModels.Count == 1 && dragModels.First() is DragDocumentModel ddm && ddm.DraggedDocuments.Count == 1)
            {
                doc = ddm.DraggedDocuments.First();
                return true;
            }

            doc = null;
            return false;
        }

        public static bool TryGetLoneDragDocAndView(this DataPackageView packageView, out DocumentController doc, out DocumentView linkView)
        {
            var dragModels = packageView.GetDragModels();
            if (dragModels.Count == 1 && dragModels.First() is DragDocumentModel ddm && ddm.DraggedDocuments.Count == 1)
            {
                doc = ddm.DraggedDocuments.First();
                linkView = ddm.LinkSourceViews.FirstOrDefault();
                return true;
            }

            doc = null;
            linkView = null;
            return false;
        }

        public static bool TryGetLoneDragModel(this DataPackageView packageView, out DragModelBase dragModel)
        {
            if (!packageView.HasDragModels())
            {
                dragModel = null;
                return false;
            }

            var dragModels = packageView.GetDragModels();
            if (dragModels.Count == 1)
            {
                dragModel = dragModels.First();
                return true;
            }

            dragModel = null;
            return false;
        }

        public static bool HasAnyPropertiesSet(this DataPackageView packageView) => packageView.Properties.ToList().Any(); 
    }
}