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

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};
        public override List<Key> OutputKeys { get; } = new List<Key> {QuotientKey, RemainderKey};

        public override List<FieldModelController> GetNewInputFields()
        {
            return new List<FieldModelController>
            {
                new NumberFieldModelController(), new NumberFieldModelController()
            };
        }

        public override List<FieldModelController> GetNewOutputFields()
        {
            return new List<FieldModelController>
            {
                new NumberFieldModelController(), new NumberFieldModelController()
            };
        }

        public override void Execute(DocumentController doc, IEnumerable<DocumentController> docContextList)
        {
            var numberA = doc.GetDereferencedField(AKey, docContextList) as NumberFieldModelController;
            Debug.Assert(numberA != null, "Input is not a number");

            var numberB = doc.GetDereferencedField(BKey, docContextList) as NumberFieldModelController;
            Debug.Assert(numberB != null, "Input is not a number");
            
            double a = numberA.Data;
            double b = numberB.Data;
            (doc.GetDereferencedField(QuotientKey, docContextList) as NumberFieldModelController).Data = a / b;
            (doc.GetDereferencedField(RemainderKey, docContextList) as NumberFieldModelController).Data = a % b;
        }
    }
}