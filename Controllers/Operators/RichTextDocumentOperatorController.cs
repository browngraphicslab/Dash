using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Windows.UI.Text;

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
            //INCOMPLETE - efficiently exctact readable text from RTF string - make a new Rich Textbox? Strip the code, as done in RichTextTitleOperator seen below?
            var value = inputs[RichTextKey] as RichTextController;
            if (value is RichTextController rtc)
            {
                computedTitle = rtc.Data.Split(
                    new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.None
                ).FirstOrDefault();
                var regex = new Regex("HYPERLINK \"[^\"].*\"");
                computedTitle = regex.Replace(computedTitle, "");
            }
            value.Data.RtfFormatString
            outputs.
            outputs[ReadableTextKey] = 
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
