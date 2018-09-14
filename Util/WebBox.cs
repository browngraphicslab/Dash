using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Windows.System;

namespace Dash
{    /// <summary>
     /// A generic document type containing a single text element.
     /// </summary>
    public class WebBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("1C17B38F-C9DC-465D-AC3E-43EA105D18C6", "Web Box");
        private static readonly string PrototypeId = "9190B041-CC40-4B32-B99B-E7A1CDE3C1C9";
        public WebBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            SetupDocument(DocumentType, PrototypeId, "WebBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var webView = new WebBoxView(docController);
            
            SetupBindings(webView, docController, context);
            
            return webView;
        }

        // document.getElementsByTagName('table')
        //tableToJson = function(table)
        //{
        //    var data = [];

        //    // first row needs to be headers
        //    var headers = [];
        //    for (var i = 0; i < table.rows[0].cells.length; i++)
        //    {
        //        headers[i] = table.rows[0].cells[i].textContent.toLowerCase().replace(/ / gi, '');
        //    }

        //    // go through cells
        //    for (var i = 1; i < table.rows.length; i++)
        //    {

        //        var tableRow = table.rows[i];
        //        var rowData = { };

        //        for (var j = 0; j < tableRow.cells.length; j++)
        //        {

        //            rowData[headers[j]] = tableRow.cells[j].textContent;

        //        }

        //        data.push(rowData);
        //    }

        //    return data;
        //}

    }
}
