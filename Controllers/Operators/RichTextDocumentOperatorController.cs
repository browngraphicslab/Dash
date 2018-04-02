using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Windows.UI.Text;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class RichTextDocumentOperatorController : OperatorController
    {

        public RichTextDocumentOperatorController() : base(new OperatorModel(OperatorType.RichTextDocument))
        {
        }

        public RichTextDocumentOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        //Input key
        public static readonly KeyController RichTextKey = KeyStore.DocumentTextKey;

        //Output key
        public static readonly KeyController ReadableTextKey = new KeyController("AAAA064D-C4BC-4623-AAD3-402077433C46", "ReadableText");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [RichTextKey] = new IOInfo(TypeInfo.RichText, true)
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ReadableTextKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var richTextController = inputs[RichTextKey] as RichTextController;
            if (richTextController != null)
            {
                var richEditBox = new RichEditBox();
                richEditBox.Document.SetText(TextSetOptions.FormatRtf, richTextController.RichTextFieldModel.Data.RtfFormatString);
                richEditBox.Document.GetText(TextGetOptions.UseObjectText, out string readableText);
                outputs[ReadableTextKey] = new TextController(readableText ?? "");
            }
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new RichTextDocumentOperatorController();
        }

        public override object GetValue(Context context)
        {
            return this;
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
