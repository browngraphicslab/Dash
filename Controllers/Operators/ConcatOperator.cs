using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ConcatOperator : OperatorFieldModelController
    {
        public static readonly KeyController AKey = new KeyController("5B15A261-18BF-479C-8F11-BF167A11B5DC", "A");
        public static readonly KeyController BKey = new KeyController("460427C6-B81C-44F8-AF96-60058BAB4F01", "B");

        public static readonly KeyController OutputKey = new KeyController("nguid", "Output");

        public ConcatOperator() : base(new OperatorFieldModel("concat")) { }

        public ConcatOperator(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController Copy()
        {
            return new ConcatOperator();
        }

        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
        public override ObservableDictionary<KeyController, TypeInfo> Inputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [AKey] = TypeInfo.Text,
            [BKey] = TypeInfo.Text
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var a = (inputs[AKey] as TextFieldModelController).Data;
            var b = (inputs[BKey] as TextFieldModelController).Data;
            outputs[OutputKey] = new TextFieldModelController(a + b);
        }
    }
}
