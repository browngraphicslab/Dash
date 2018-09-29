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

                        var layout = await new PdfToDashUtil().UriToDoc(uri);
                        layout.SetField<BoolController>(KeyStore.AbstractInterfaceKey, true, true);

                        SplitFrame.ActiveFrame.TrySplit(SplitFrame.SplitDirection.Right, layout, true);
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
