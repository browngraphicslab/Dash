using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Microsoft.Extensions.DependencyInjection;



namespace Dash
{
    public class DivideOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("AAC1631C-9DC3-48FC-984A-EE0D80C9A397", "A");
        public static readonly Key BKey = new Key("A757D709-8D83-44C9-B047-D5DB6420F51F", "B");

        //Output keys
        public static readonly Key QuotientKey = new Key("DA705E3D-4773-4C7D-B770-536BA321D0FA", "Quotient");
        public static readonly Key RemainderKey = new Key("32208EDB-B673-4957-A0AB-3704A15A1686", "Remainder");

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};
        public override List<Key> OutputKeys { get; } = new List<Key> {QuotientKey, RemainderKey};

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
                new NumberFieldModel(), new NumberFieldModel()
            };
        }

        public DivideOperatorModel()
        {
        }

        public override void Execute(IDictionary<Key, FieldModel> fields)
        {
            NumberFieldModel numberA = fields[AKey] as NumberFieldModel;
            Debug.Assert(numberA != null, "Input is not a number");

            NumberFieldModel numberB = fields[BKey] as NumberFieldModel;
            Debug.Assert(numberB != null, "Input is not a number");

            double a = numberA.Data;
            double b = numberB.Data;
            (fields[QuotientKey] as NumberFieldModel).Data = a / b;
            (fields[RemainderKey] as NumberFieldModel).Data = a % b;
        }
    }
}