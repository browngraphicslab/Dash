using System.Collections.Generic;
using System.Diagnostics;
using DashShared;


namespace Dash
{
    public class DivideOperatorFieldModelController : OperatorFieldModelController
    {
        public DivideOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }
        //Input keys
        public static readonly Key AKey = new Key("AAC1631C-9DC3-48FC-984A-EE0D80C9A397", "A");
        public static readonly Key BKey = new Key("A757D709-8D83-44C9-B047-D5DB6420F51F", "B");

        //Output keys
        public static readonly Key QuotientKey = new Key("DA705E3D-4773-4C7D-B770-536BA321D0FA", "Quotient");
        public static readonly Key RemainderKey = new Key("32208EDB-B673-4957-A0AB-3704A15A1686", "Remainder");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [AKey] = TypeInfo.Number,
            [BKey] = TypeInfo.Number
        };
        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [QuotientKey] = TypeInfo.Number,
            [RemainderKey] = TypeInfo.Number
        };

        private int nextChar = 'C';

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            var numberA = inputs[AKey] as NumberFieldModelController;

            var numberB = inputs[BKey] as NumberFieldModelController;

            string s = new string((char)nextChar++, 1);
            Inputs.Add(new Key(s, s), TypeInfo.Number);
            
            double a = numberA.Data;
            double b = numberB.Data;
            outputs[QuotientKey] = new NumberFieldModelController(a / b);
            outputs[RemainderKey] = new NumberFieldModelController(a % b);
        }
    }
}