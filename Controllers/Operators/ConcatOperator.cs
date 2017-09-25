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
        public static readonly KeyControllerBase AKey = new KeyControllerBase("5B15A261-18BF-479C-8F11-BF167A11B5DC", "A");
        public static readonly KeyControllerBase BKey = new KeyControllerBase("460427C6-B81C-44F8-AF96-60058BAB4F01", "B");

        public static readonly KeyControllerBase OutputKey = new KeyControllerBase("nguid", "Output");

        public ConcatOperator() : base(new OperatorFieldModel("concat")) { }

        public ConcatOperator(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController<OperatorFieldModel> Copy()
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
        public override ObservableDictionary<KeyControllerBase, IOInfo> Inputs { get; } = new ObservableDictionary<KeyControllerBase, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Text, true),
            [BKey] = new IOInfo(TypeInfo.Text, true)
        };

        public override ObservableDictionary<KeyControllerBase, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyControllerBase, TypeInfo>
        {
            [OutputKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyControllerBase, FieldControllerBase> inputs, Dictionary<KeyControllerBase, FieldControllerBase> outputs)
        {
            var a = (inputs[AKey] as TextFieldModelController).Data;
            var b = (inputs[BKey] as TextFieldModelController).Data;
            outputs[OutputKey] = new TextFieldModelController(a + b);
        }
    }
}
