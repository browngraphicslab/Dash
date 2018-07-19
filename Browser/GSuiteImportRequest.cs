using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
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

        public string url { get; set; }

        public override async Task Handle(BrowserView browser)
        {
            byte[] bdata = Convert.FromBase64String(data);
            var storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await storageFolder.CreateFileAsync((name ?? UtilShared.GenerateNewId()) + ".pdf", CreationCollisionOption.GenerateUniqueName);
            await Windows.Storage.FileIO.WriteBytesAsync(file, bdata);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    var doc = new PdfToDashUtil().GetPDFDoc(file);
                    if (MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>().CurrentView is CollectionFreeformBase cfb)
                    {
                        var point = Util.GetCollectionFreeFormPoint(cfb, new Point(
                            (MainPage.Instance.MainDocView.ActualWidth - MainPage.Instance.xMainTreeView.ActualWidth) / 2,
                            MainPage.Instance.MainDocView.ActualHeight / 2));
                        doc.SetField(KeyStore.PositionFieldKey, new PointController(point), true);
                    }

                    doc.GetDataDocument().SetField(KeyStore.WebContextKey, new TextController(url), true);
                    MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>()?.ViewModel
                        .AddDocument(doc);
                });
        }

    }
}
