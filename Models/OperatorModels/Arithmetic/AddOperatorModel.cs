﻿namespace Dash
{
    /* 
    public class AddOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("15964FFF-9592-47A7-8DED-FC09B4144E63", "Input A");
        public static readonly Key BKey = new Key("D1792115-40EA-479A-8F96-6E7207031A60", "Input B");

        //Output keys
        public static readonly Key SumKey = new Key("E1AFCF61-5AAF-4CDD-9C06-17EBFB4EF82A", "Sum");

        public override List<Key> InputKeys { get; } = new List<Key> { AKey, BKey };

        public override List<Key> OutputKeys { get; } = new List<Key> { SumKey };

        public override List<FieldModel> GetNewInputFields()
        {
            return new List<FieldModel>
            {
                new NumberFieldModel(), new NumberFieldModel()
            };
        }

        public override List<FieldModel> GetNewOutputFields()
        {
            return new List<FieldModel>
            {
                new NumberFieldModel()
            };
        }

        public override void Execute(DocumentModel doc)
        {
            throw new NotImplementedException();

            //NumberFieldModel numberA = doc.Field(AKey) as NumberFieldModel;
            //Debug.Assert(numberA != null, "Input is not a number");

            //NumberFieldModel numberB = doc.Field(BKey) as NumberFieldModel;
            //Debug.Assert(numberB != null, "Input is not a number");

            //double a = numberA.Data;
            //double b = numberB.Data;

            //(doc.Field(SumKey) as NumberFieldModel).Data = a + b;
        }
    }
    */ 
}
