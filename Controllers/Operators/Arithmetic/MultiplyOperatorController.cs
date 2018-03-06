using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation.Metadata;
using DashShared;

namespace Dash
{
    [OperatorType("mult")]
    public class MultiplyOperatorController : OperatorController
    {

        public MultiplyOperatorController() : base(new OperatorModel(OperatorType.Multiply))
        {

        }

        public MultiplyOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        //Input keys
        public static readonly KeyController AKey = new KeyController("D0FF0175-F158-43CC-B2A3-CE7266BBA062", "A");
        public static readonly KeyController BKey = new KeyController("3D1BB49B-6F11-4044-AC81-2ECA3EECEB7B", "B");

        //Output keys
        public static readonly KeyController ProductKey = new KeyController("B618238C-16A0-4F6F-9DEE-C4657C087991", "Product");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ProductKey] = TypeInfo.Number,
        };

        public static int numExecutions = 0;

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var numberA = (NumberController)inputs[AKey];
            var numberB = (NumberController)inputs[BKey];
            Debug.WriteLine("NumExecutions " + ++numExecutions + " " + numberA);

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[ProductKey] = new NumberController(a * b);
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new MultiplyOperatorController();
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
    }
}