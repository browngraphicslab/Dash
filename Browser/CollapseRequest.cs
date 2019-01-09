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


        static public SplitFrame LastFrame = null;

        static Dictionary<Uri,DocumentController> Recent = new Dictionary<Uri,DocumentController>();

        public override async Task Handle(BrowserView browser)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    if (!expanded)
                    {

                        var uri    = new Uri(url);
                        var dockedPdfView = Recent.ContainsKey(uri) ? Recent[uri] : null;
                        if (dockedPdfView == null)
                        {
                            dockedPdfView = await new PdfToDashUtil().UriToDoc(uri);
                            Recent.Add(uri, dockedPdfView);
                            MainPage.Instance.MiscellaneousFolder.GetDataDocument().AddToListField(KeyStore.DataKey, dockedPdfView);
                        }
                        dockedPdfView.SetHeight(double.NaN);// MainPage.Instance.ActualHeight - 110);
                        dockedPdfView.SetWidth(double.NaN);
                        dockedPdfView.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Stretch);
                        dockedPdfView.SetVerticalAlignment(Windows.UI.Xaml.VerticalAlignment.Stretch);
                        //SplitFrame.ActiveFrame.OpenDocument(dockedPdfView);
                        SplitFrame.ActiveFrame.Split(SplitDirection.Left, dockedPdfView, true);
                        //if (LastFrame == null || !MainPage.Instance.GetDescendants().Contains(LastFrame))
                        //    SplitFrame.ActiveFrame.Split(SplitDirection.Left, dockedPdfView, true);
                        //else 
                        //LastFrame = MainPage.Instance.MainSplitter.GetFrameWithDoc(Recent[uri], false);
                    }
                    else
                    {
                        LastFrame?.Delete();
                        LastFrame = null;
                    }
                });
        }
    }
}
