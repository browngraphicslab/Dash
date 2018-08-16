using System;
using Windows.ApplicationModel.DataTransfer;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [Flags]
    public enum DragDataTypeInfo
    {
        None = 0x0,
        Image = 0x1,
        Video = 0x2,
        Document = 0x4,
        StorageFile = 0x8,
        Html = 0x16,
        Rtf = 0x32,

        Any = Image | Video | Document | StorageFile | Html | Rtf
    }
}

    public static class DataPackageExtensionMethods
    {
        public static bool HasDroppableDocuments(this DataPackageView)
        {

        }

        public static GetDataOfType) 
        ()

    }
}