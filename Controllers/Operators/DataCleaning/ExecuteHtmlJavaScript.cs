using System;
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
    public class ExecuteHtmlJavaScript : OperatorController
    {
        //Input Keys

        /// <summary>
        /// This key contains a ListController<DocumentController> which is the input
        /// to the melt operator
        /// </summary>
        public static readonly KeyController HtmlINput =
            new KeyController("4B55E588-D6C9-4C0D-B5A8-AB61BD4B2E9B", "Html Input");

        public static readonly KeyController Script =
            new KeyController("44ACDEBC-D6E5-4491-9755-B4E462202BA7", "Script");

        // Output Keys
        public static readonly KeyController OutputDocument =
            new KeyController("34B77899-D18D-4AD4-8C5E-FC617548C392", "Output Document");


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } =
            new ObservableDictionary<KeyController, IOInfo>()
            {
                [HtmlINput] = new IOInfo(TypeInfo.Text, true),
                [Script]    = new IOInfo(TypeInfo.Text, true),

            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputDocument] = TypeInfo.Document
            };

        WebView _web = null;
        public ExecuteHtmlJavaScript() : base(new OperatorModel(OperatorType.ExecuteHtmlJavaScript))
        {
            _web = MainPage.Instance.JavaScriptHack;
            _web.ScriptNotify += _web_ScriptNotify;
        }

        private void _web_ScriptNotify(object sender, NotifyEventArgs e)
        {
            scripoutput = e.Value;
            var jsonlist = new JsonToDashUtil().ParseJsonString(scripoutput, "");
            var cnote = (CollectionNote)_web.Tag;
            var children = cnote.DataDocument.GetDereferencedField(CollectionNote.CollectedDocsKey, null) as ListController<DocumentController>;
            Debug.Assert(children != null);
            if (!children.GetElements().Contains(jsonlist))
                children.Add(jsonlist);
        }

        string scripoutput = "";
        public ExecuteHtmlJavaScript(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            _web.ScriptNotify += _web_ScriptNotify;
        }

        public override async void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var html    = (inputs[HtmlINput] as TextController).Data;
            var script  = (inputs[Script] as RichTextController).Data.ReadableString;
            var modHtml = html.Substring(html.ToLower().IndexOf("<html"), html.Length - html.ToLower().IndexOf("<html"));
            var correctedHtml = modHtml.Replace("<html>", "<html><head><style>img {height: auto !important;}</style></head>");

            var doc = new CollectionNote(new Windows.Foundation.Point(), CollectionView.CollectionViewType.Freeform);
            await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, new Windows.UI.Core.DispatchedHandler(
                async () =>
                {
                    _web.NavigateToString(correctedHtml);
                    _web.LoadCompleted += (s, e) =>
                    {
                        _web.Tag = doc;
                         _web.InvokeScriptAsync("eval", new[] { script });
                    };

                }));

            
            outputs[OutputDocument] = doc.Document;
        }
        

        public override bool SetValue(object value)
        {
            return false;
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new MeltOperatorController();
        }
    }
}