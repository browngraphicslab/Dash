﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dash.Models;
using Dash.StaticClasses;
using DashShared;
using Windows.UI.Xaml.Controls;
using static Dash.NoteDocuments;

namespace Dash
{
    public class ExecuteHtmlJavaScriptController : OperatorController
    {
        //Input Keys

        /// <summary>
        /// This key contains a ListController<DocumentController> which is the input
        /// to the melt operator
        /// </summary>
        public static readonly KeyController HtmlInputKey =
            new KeyController("4B55E588-D6C9-4C0D-B5A8-AB61BD4B2E9B", "Html Input");

        public static readonly KeyController ScriptKey =
            new KeyController("44ACDEBC-D6E5-4491-9755-B4E462202BA7", "Script");

        // Output Keys
        public static readonly KeyController OutputDocumentKey =
            new KeyController("34B77899-D18D-4AD4-8C5E-FC617548C392", "Output Document");

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = rfmc => new ExecuteHtmlOperatorBox(rfmc);


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } =
            new ObservableDictionary<KeyController, IOInfo>()
            {
                [HtmlInputKey] = new IOInfo(TypeInfo.Text, true),
                [ScriptKey]    = new IOInfo(TypeInfo.Text, true),

            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputDocumentKey] = TypeInfo.Document
            };


        public static DocumentController CreateController(DocumentReferenceController htmlRef)
        {
            var fieldController = new ExecuteHtmlJavaScriptController();
            var execOp = OperatorDocumentFactory.CreateOperatorDocument(fieldController);
            execOp.SetField(HtmlInputKey, htmlRef, true);
            execOp.SetField(ScriptKey,    new TextController(""), true);
            execOp.SetField(OutputDocumentKey, new TextController(""), true);

            var layoutDoc = new ExecuteHtmlOperatorBox(new DocumentReferenceController(execOp.GetId(), KeyStore.OperatorKey)).Document;
            execOp.SetActiveLayout(layoutDoc, true, true);
            return execOp;
        }
        

        public ExecuteHtmlJavaScriptController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }
        

        public ExecuteHtmlJavaScriptController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("D0286E73-D9F6-4341-B901-5ECC27AC76BC", "Execute html javascript");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var html    = (inputs[HtmlInputKey] as TextController).Data;
            var script  = (inputs[ScriptKey] as TextController).Data;
            if (html.Contains("<html>"))
            {
                var modHtml = html.Substring(html.ToLower().IndexOf("<html"), html.Length - html.ToLower().IndexOf("<html"));
                var correctedHtml = modHtml.Replace("<html>", "<html><head><style>img {height: auto !important;}</style></head>");

                var doc = new CollectionNote(new Windows.Foundation.Point(), CollectionView.CollectionViewType.Schema);

                MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(
                    async () => new execClass(correctedHtml, script, doc)));

                outputs[OutputDocumentKey] = doc.Document;
            }
        }

        class execClass
        {
            WebView _web = MainPage.Instance.JavaScriptHack;
            CollectionNote Cnote;
            static int id = 10000;
            static string prefix = "window.external.notify(";
            int Id = 0;
            public execClass(string correctedHtml, string script, CollectionNote doc)
            {
                Cnote = doc;
                _web.ScriptNotify += _web_ScriptNotify1;
                _web.NavigateToString(correctedHtml);
                Id = id++;
                _web.LoadCompleted += (s, e) =>
                {
                    if (Id == id-1)
                    {
                        _web.InvokeScriptAsync("eval", new[] { script.Replace(prefix, prefix +"\'" + Id + "\'+") });
                    }
                };
                if (id > 99999)
                    id = 10000; 
            }

            private void _web_ScriptNotify1(object sender, NotifyEventArgs e)
            {
                var id = int.Parse(e.Value.Substring(0, 5));
                var res = e.Value.Substring(5, e.Value.Length - 5);
                if (id == Id)
                {
                    var jsonlist = new JsonToDashUtil().ParseJsonString(res, "HtmlExec");
                    var children = Cnote.Document.GetDataDocument().GetDereferencedField(KeyStore.DataKey, null) as ListController<DocumentController>;
                    foreach (var f in jsonlist.EnumFields(true))
                        if (f.Value is ListController<DocumentController>)
                            foreach (var d in (f.Value as ListController<DocumentController>).TypedData)
                                if (!children.GetElements().Contains(d))
                                {
                                    foreach (var field in d.EnumDisplayableFields().ToArray())
                                    {
                                        if (field.Value is TextController)
                                        {
                                            double num;
                                            if (double.TryParse((field.Value as TextController).Data, out num))
                                            {
                                                d.SetField(field.Key, new NumberController(num), true);
                                            }
                                        }
                                    }
                                    children.Add(d);
                                }
                }
            }
        };
       

        public override FieldControllerBase GetDefaultController()
        {
            return new ExecuteHtmlJavaScriptController();
        }
    }
}