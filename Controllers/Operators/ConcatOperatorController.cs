using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ConcatOperatorController : OperatorController
    {
        public static readonly KeyController AKey = new KeyController("5B15A261-18BF-479C-8F11-BF167A11B5DC", "A");
        public static readonly KeyController BKey = new KeyController("460427C6-B81C-44F8-AF96-60058BAB4F01", "B");

        public static readonly KeyController OutputKey = new KeyController("nguid", "Output");

        public ConcatOperatorController() : base(new OperatorModel(OperatorType.Concat)) { }

        public ConcatOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new ConcatOperatorController();
        }

        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [AKey] = new IOInfo(TypeInfo.Text, true),
            [BKey] = new IOInfo(TypeInfo.Text, true)
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var a = (inputs[AKey] as TextController).Data;
            var b = (inputs[BKey] as TextController).Data;
            outputs[OutputKey] = new TextController(a + b);
        }
    }
}
