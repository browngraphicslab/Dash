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
                            var note = new RichTextNote("PDF HEADER");
                            note.Document.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Center);
                            note.Document.SetHeight(45);
                            var pdfLayout = await new PdfToDashUtil().UriToDoc(uri);
                            pdfLayout.SetHeight(MainPage.Instance.ActualHeight - 110);
                            pdfLayout.SetWidth(double.NaN);
                            pdfLayout.SetHorizontalAlignment(Windows.UI.Xaml.HorizontalAlignment.Stretch);
                            pdfLayout.SetVerticalAlignment(Windows.UI.Xaml.VerticalAlignment.Top);
                            var docs = new List<DocumentController>(new DocumentController[] { note.Document, pdfLayout });
                            dockedPdfView = new CollectionNote(new Windows.Foundation.Point(), CollectionViewType.Stacking, double.NaN, double.NaN, docs).Document;
                            Recent.Add(uri, dockedPdfView);
                        }

                        if (LastFrame == null || !MainPage.Instance.GetDescendants().Contains(LastFrame))
                            SplitFrame.ActiveFrame.Split(SplitDirection.Left, dockedPdfView, true);
                        else LastFrame.OpenDocument(dockedPdfView);
                        LastFrame = MainPage.Instance.MainSplitter.GetFrameWithDoc(Recent[uri], false);
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
