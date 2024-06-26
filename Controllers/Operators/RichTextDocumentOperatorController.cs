﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    [OperatorType(Op.Name.rich_document_text)]
    public class RichTextDocumentOperatorController : OperatorController
    {

        public RichTextDocumentOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public RichTextDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        //Input key   KeyStore.DataKey;

        //Output key
        public static readonly KeyController ReadableTextKey = KeyStore.DocumentTextKey;

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>>  Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(KeyStore.DataKey, new IOInfo(TypeInfo.RichText, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ReadableTextKey] = TypeInfo.Text
        };

        

        public override KeyController OperatorType { get; } = TypeKey;
        private static KeyController TypeKey = KeyController.Get("Doc Text");
        private static RichEditBox richEditBox = new RichEditBox();
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var richTextController = inputs[KeyStore.DataKey] as RichTextController;
            if (richTextController != null)
            {
                richEditBox.Document.SetText(TextSetOptions.FormatRtf, richTextController.RichTextFieldModel.Data.RtfFormatString);

                richEditBox.Document.GetText(TextGetOptions.NoHidden, out string readableText);
                readableText = readableText.Replace("\r", "\n").Replace("\u00A0", " ");
                outputs[ReadableTextKey] = new TextController(readableText ?? "");
            }
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new RichTextDocumentOperatorController();
        }
    }
}
