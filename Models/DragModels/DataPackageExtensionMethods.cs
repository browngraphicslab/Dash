using System;
using Windows.ApplicationModel.DataTransfer;
using Dash;
using Dash.Models.DragModels;

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

        Any = Image | Document | StorageFile | Html | Rtf
    }

    public static class DataPackageExtensionMethods
    {
        public static bool HasDataOfType(this DataPackageView packageView, DragDataTypeInfo type)
        {
            var retVal = false;

            if (type.HasFlag(DragDataTypeInfo.Image))
            {
                retVal |= packageView.Contains(StandardDataFormats.Bitmap);
            }

            if (type.HasFlag(DragDataTypeInfo.Document))
            {
                retVal |= packageView.Properties.ContainsKey(nameof(DragDocumentModel));
            }

            if (type.HasFlag(DragDataTypeInfo.Field))
            {
                retVal |= packageView.Properties.ContainsKey(nameof(DragFieldModel));
            }

            if (type.HasFlag(DragDataTypeInfo.StorageFile)) {
                retVal |= packageView.Contains(StandardDataFormats.StorageItems);
            }

            if (type.HasFlag(DragDataTypeInfo.Html)) {
                retVal |= packageView.Contains(StandardDataFormats.Html);
            }

            if (type.HasFlag(DragDataTypeInfo.Rtf)) {
                retVal |= packageView.Contains(StandardDataFormats.Rtf);
            }

            return retVal;
        }
    }
}
}