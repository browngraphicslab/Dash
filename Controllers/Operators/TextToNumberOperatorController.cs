using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Toolkit.Extensions;

namespace Dash
{
    [OperatorType("textToNumber")]
    public class TextToNumberOperatorController : OperatorController
    {
        public TextToNumberOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public TextToNumberOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        //Input keys
        public static readonly KeyController TextKey = new KeyController("DC0B046B-5226-4F4B-B3FD-3425CF362D29", "Text");

        //Output keys
        public static readonly KeyController NumberKey = new KeyController("D8884AD6-B67C-4C75-9813-23775D3FA22A", "Number");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
            {
                new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true))
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [NumberKey] = TypeInfo.Number
            };
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey =
            new KeyController("48AA3F01-569D-445B-BB4A-3158D9968EDF", "Text To Number");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var textController = inputs[TextKey] as TextController;

            if (double.TryParse(textController.Data, out var num))
            {
                outputs[NumberKey] = new NumberController(num);
            }
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new TextToNumberOperatorController();
        }
    }
}
