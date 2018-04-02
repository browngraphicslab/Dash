using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Windows.UI.Text;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Documents;
using System.Windows.Documents.TextPointer;

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
        public static readonly KeyController ReadableTextKey = new KeyController("INSERT GUID", "ReadableText"); //"     "

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
            //Missing reference to System.Windows.Documents
            var richTextController = inputs[RichTextKey] as RichTextController;
            RichTextBox rtb = new RichTextBox(richTextController, 0, 0, 0, 0);
            TextRange textRange = new TextRange(
                rtb.Document.ContentStart, 
                rtb.Document.ContentEnd
            );
            outputs[ReadableTextKey] = textRange.Text;
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
