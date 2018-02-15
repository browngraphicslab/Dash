﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class ExecuteHtmlOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("63DCAFB4-EED1-4EA7-868F-FAA2479651B0", "ExecuteHtmlOperator Box");

        public ExecuteHtmlOperatorBox(ReferenceController refToOp, double width=350, double height=100)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(width,height), refToOp);
            Document = new DocumentController(fields, DocumentType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var data = docController.GetField(KeyStore.DataKey);
            var opfmc = (data as ReferenceController);
            OperatorView opView = new OperatorView { DataContext = opfmc.GetFieldReference() };
            var opDoc = opfmc.GetDocumentController(null);
            var stack = new Windows.UI.Xaml.Controls.StackPanel();
            stack.Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical;
           
            var scriptBox = new Windows.UI.Xaml.Controls.ComboBox();
            scriptBox.ItemsSource = new string[] { "Tables", "Images", "References" };
            scriptBox.Height = 50;
            scriptBox.VerticalAlignment = VerticalAlignment.Top;
            scriptBox.SelectionChanged += ((sender, e) =>
            {
                //DBTest.ResetCycleDetection();
                if (opDoc != null && (sender as Windows.UI.Xaml.Controls.ComboBox).SelectedItem == "Tables")
                    opDoc.SetField(ExecuteHtmlJavaScriptController.ScriptKey,
                        new TextController("function tableToJson(table) { var data = []; var headers = []; for (var i = 0; i < table.rows[0].cells.length; i++) {headers[i] = table.rows[0].cells[i].textContent.toLowerCase().replace(' ', ''); } for (var i = 1; i < table.rows.length; i++) { var tableRow = table.rows[i]; var rowData = { }; for (var j = 0; j < tableRow.cells.length; j++) { rowData[headers[j]] = tableRow.cells[j].textContent; } data.push(rowData); } return data; } window.external.notify( JSON.stringify( tableToJson( document.getElementsByTagName('table')[0]) ))"), false);
            });

            stack.Children.Add(scriptBox);
            stack.HorizontalAlignment = HorizontalAlignment.Stretch;
            stack.VerticalAlignment = VerticalAlignment.Top;
            opView.OperatorContent = stack;
            opView.xOpContentPresenter.Width = 100;
            
            return opView;
        }
    }
}
