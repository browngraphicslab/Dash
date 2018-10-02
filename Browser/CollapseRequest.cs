using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Dash.Browser;
using Dash.Controllers;
using DashShared;

namespace Dash 
{
    public class CollapseRequest : BrowserRequest
    {
        public bool expanded { get; set; }
        public string url { get; set; }

        public override async Task Handle(BrowserView browser)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    if (!expanded)
                    {
                        var uri = new Uri(url);

                        var note = new RichTextNote("PDF HEADER");
                        note.Document.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Center);
                        note.Document.SetHeight(45);
                        var layout = await new PdfToDashUtil().UriToDoc(uri);
                        layout.SetHeight(MainPage.Instance.ActualHeight - 45);
                        layout.SetWidth(double.NaN);
                        layout.SetField<BoolController>(KeyStore.AbstractInterfaceKey, true, true);
                        layout.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Stretch);
                        var c2 = new CollectionNote(new Windows.Foundation.Point(), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN, new DocumentController[] { layout });
                        var docs = new List<DocumentController>(new DocumentController[] {note.Document, layout });
                        var coll = new CollectionNote(new Windows.Foundation.Point(), CollectionView.CollectionViewType.Stacking, double.NaN, double.NaN, docs);
                        coll.Document.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Stretch);
                        coll.Document.SetVerticalAlignment(Windows.UI.Xaml.VerticalAlignment.Stretch);

                        SplitFrame.ActiveFrame.TrySplit(SplitFrame.SplitDirection.Right, coll.Document, true);
                    }
                    else
                    {
                        var thisPdfDoc = MainPage.Instance.MainSplitter.GetChildFrames().FirstOrDefault(fr =>
                            fr.DocumentController.GetField<BoolController>(KeyStore.AbstractInterfaceKey) != null);
                        thisPdfDoc?.Delete();
                    }
                });
        }
    }
}
