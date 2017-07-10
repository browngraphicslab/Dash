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

        public override Dictionary<Key, TypeInfo> Inputs { get; } = new Dictionary<Key, TypeInfo>
        {
            [AKey] = TypeInfo.Number,
            [BKey] = TypeInfo.Number
        };
        public override Dictionary<Key, TypeInfo> Outputs { get; } = new Dictionary<Key, TypeInfo>
        {
            [QuotientKey] = TypeInfo.Number,
            [RemainderKey] = TypeInfo.Number
        };

        public override void Execute(DocumentController doc, IEnumerable<DocumentController> docContextList)
        {
            var numberA = doc.GetDereferencedField(AKey, docContextList) as NumberFieldModelController;

            var numberB = doc.GetDereferencedField(BKey, docContextList) as NumberFieldModelController;

            if (numberA == null || numberB == null)//One or more of the inputs isn't set yet
            {
                return;
            }

            double a = numberA.Data;
            double b = numberB.Data;
            (doc.GetDereferencedField(QuotientKey, docContextList) as NumberFieldModelController).Data = a / b;
            (doc.GetDereferencedField(RemainderKey, docContextList) as NumberFieldModelController).Data = a % b;
        }
    }
}