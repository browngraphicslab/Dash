using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Dash.Browser;
using DashShared;

namespace Dash
{
    class GSuiteImportRequest : BrowserRequest
    {
        public string name { get; set; }

        public string data { get; set; }

        public override async Task Handle(BrowserView browser)
        {
            byte[] bdata = Convert.FromBase64String(data);
            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await storageFolder.CreateFileAsync((name ?? UtilShared.GenerateNewId()) + ".pdf", CreationCollisionOption.GenerateUniqueName);
            await Windows.Storage.FileIO.WriteBytesAsync(file, bdata);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>()?.ViewModel
                        .AddDocument(new PdfToDashUtil().GetPDFDoc(file));
                });
        }

    }
}
