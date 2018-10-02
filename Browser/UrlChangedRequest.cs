using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Browser;
using Dash.Controllers;
using DashShared;

namespace Dash 
{
    public class UrlChangedRequest : BrowserRequest
    {
        public string url { get; set; }

        public override async Task Handle(BrowserView browser)
        {
            //var uri = new Uri(url);

            //// create a backing document for the pdf
            //var fields = new Dictionary<KeyController, FieldControllerBase>
            //{
            //    [KeyStore.DataKey] = new PdfController(uri),
            //    [KeyStore.TitleKey] = new TextController("Chrome_PDF"),
            //    [KeyStore.DateCreatedKey] = new DateTimeController(),
            //    [KeyStore.AuthorKey] = new TextController(MainPage.Instance.GetSettingsView.UserName)
            //};
            //var dataDoc = new DocumentController(fields, DocumentType.DefaultType);
            
            //// return a new pdf box
            //DocumentController layout = new PdfBox(new DocumentReferenceController(dataDoc, KeyStore.DataKey)).Document;
            //layout.SetField(KeyStore.DocumentContextKey, dataDoc, true);

            //SplitFrame.ActiveFrame.TrySplit(SplitFrame.SplitDirection.Left, layout, true);
        }
    }
}
