using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            SaveOnServer();
        }

        public RichTextDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        //Input key   KeyStore.DataKey;

        //Output key
        public static readonly KeyController ReadableTextKey = new KeyController("ReadableText");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>>  Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(KeyStore.DataKey, new IOInfo(TypeInfo.RichText, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ReadableTextKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static KeyController TypeKey = new KeyController("Doc Text", "A0BB0580-31E8-441E-907A-8A9C74224964");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var richTextController = inputs[KeyStore.DataKey] as RichTextController;
            if (richTextController != null)
            {
                var richEditBox = new RichEditBox();
                richEditBox.Document.SetText(TextSetOptions.FormatRtf, richTextController.RichTextFieldModel.Data.RtfFormatString);
                richEditBox.Document.GetText(TextGetOptions.UseObjectText, out string readableText);
                readableText = readableText.Replace("\r", "\n");
                outputs[ReadableTextKey] = new TextController(readableText ?? "");
            }
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new RichTextDocumentOperatorController();
        }
    }
}
